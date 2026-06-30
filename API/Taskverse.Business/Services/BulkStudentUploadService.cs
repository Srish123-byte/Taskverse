using System.Net;
using System.Data.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.Business.Services;

public class BulkStudentUploadService : IBulkStudentUploadService
{
    private const string StudentRole = "Student";
    private const int EnrollmentNumberMaxLength = 50;

    private readonly IDbContextFactory<TaskverseContext> _dbContextFactory;
    private readonly IEmailService _emailService;
    private readonly ILogger<BulkStudentUploadService> _logger;

    public BulkStudentUploadService(
        IDbContextFactory<TaskverseContext> dbContextFactory,
        IEmailService emailService,
        ILogger<BulkStudentUploadService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<BulkStudentUploadResultDto> UploadAsync(
        BulkStudentUploadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.UploadedByUserId == Guid.Empty)
        {
            throw new InvalidOperationException("The uploading user could not be resolved.");
        }

        if (request.Rows.Count == 0)
        {
            throw new InvalidOperationException("The upload file does not contain any student rows.");
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

        try
        {
            transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var result = new BulkStudentUploadResultDto();
            var normalizedRows = request.Rows
                .Select((row, index) => new UploadRowContext(index + 2, row))
                .ToList();

            var duplicateEmailSet = normalizedRows
                .GroupBy(row => NormalizeEmail(row.Row.Email))
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                .SelectMany(group => group)
                .ToList();

            foreach (var duplicateRow in duplicateEmailSet)
            {
                result.DuplicateRows.Add(new BulkStudentUploadRowIssueDto
                {
                    RowNumber = duplicateRow.RowNumber,
                    Email = duplicateRow.Row.Email,
                    Message = "Duplicate email found in the uploaded file."
                });
            }

            var fileDuplicateRowNumbers = duplicateEmailSet.Select(item => item.RowNumber).ToHashSet();
            var candidateRows = normalizedRows
                .Where(row => !fileDuplicateRowNumbers.Contains(row.RowNumber))
                .ToList();

            candidateRows = await PrepareRowsAsync(
                candidateRows,
                request.RestrictedCollegeId,
                context,
                result.InvalidRows,
                cancellationToken);

            var collegeIds = candidateRows
                .Select(row => ParseGuid(row.Row.CollegeId))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .Distinct()
                .ToList();

            var classIds = candidateRows
                .Select(row => ParseGuid(row.Row.ClassId))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .Distinct()
                .ToList();

            var batchIds = candidateRows
                .Select(row => ParseGuid(row.Row.BatchId))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .Distinct()
                .ToList();

            var colleges = await context.Colleges
                .AsNoTracking()
                .Where(item => collegeIds.Contains(item.CollegeId))
                .ToDictionaryAsync(item => item.CollegeId, cancellationToken);

            var classes = await context.Classes
                .AsNoTracking()
                .Where(item => classIds.Contains(item.ClassId))
                .ToDictionaryAsync(item => item.ClassId, cancellationToken);

            foreach (var trackedClass in context.Classes.Local)
            {
                classes[trackedClass.ClassId] = trackedClass;
            }

            var batches = await context.Batches
                .AsNoTracking()
                .Where(item => batchIds.Contains(item.BatchId))
                .ToDictionaryAsync(item => item.BatchId, cancellationToken);

            var candidateEmails = candidateRows
                .Select(item => NormalizeEmail(item.Row.Email))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingEmails = await context.Users
                .AsNoTracking()
                .Where(user => candidateEmails.Contains(user.Email))
                .Select(user => user.Email)
                .ToListAsync(cancellationToken);

            var existingEmailSet = existingEmails
                .Select(NormalizeEmail)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var passwordHasher = new PasswordHasher<User>();
            var createdUsers = new List<CreatedStudentCredential>();

            foreach (var row in candidateRows)
            {
                var normalizedEmail = NormalizeEmail(row.Row.Email);
                if (existingEmailSet.Contains(normalizedEmail))
                {
                    result.DuplicateRows.Add(new BulkStudentUploadRowIssueDto
                    {
                        RowNumber = row.RowNumber,
                        Email = row.Row.Email,
                        Message = "Email already exists."
                    });
                    continue;
                }

                if (!TryValidateRow(row, request.RestrictedCollegeId, colleges, classes, batches, out var validationMessage, out var collegeId, out var classId, out var batchId))
                {
                    result.InvalidRows.Add(new BulkStudentUploadRowIssueDto
                    {
                        RowNumber = row.RowNumber,
                        Email = row.Row.Email,
                        Message = validationMessage
                    });
                    continue;
                }

                var tempPassword = TemporaryPasswordGenerator.Generate();
                var now = DateTime.UtcNow;
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = row.Row.FullName.Trim(),
                    Email = normalizedEmail,
                    Phone = row.Row.Phone.Trim(),
                    EnrollmentNumber = string.IsNullOrWhiteSpace(row.Row.EnrollmentNumber) ? null : row.Row.EnrollmentNumber.Trim(),
                    CollegeId = collegeId,
                    CollegeName = colleges[collegeId].CollegeName?.Trim(),
                    Role = StudentRole,
                    Status = UserStatus.APPROVED,
                    BatchId = batchId,
                    ClassId = classId,
                    CreatedAt = now,
                    ModifiedAt = now,
                    TemporaryPassword = tempPassword,
                    UploadedBy = request.UploadedByUserId,
                    IsBulkUploaded = true,
                    MustChangePassword = true,
                    TempPasswordIssuedAt = now
                };
                user.PasswordHash = passwordHasher.HashPassword(user, tempPassword);

                context.Users.Add(user);
                existingEmailSet.Add(normalizedEmail);
                createdUsers.Add(new CreatedStudentCredential(row.Row.FullName.Trim(), normalizedEmail, tempPassword));
                result.CreatedUsers.Add(new BulkStudentUploadCreatedUserDto
                {
                    FullName = row.Row.FullName.Trim(),
                    Email = normalizedEmail
                });
            }

            result.CreatedCount = result.CreatedUsers.Count;
            result.DuplicateCount = result.DuplicateRows.Count;
            result.InvalidCount = result.InvalidRows.Count;

            if (result.CreatedCount == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return result;
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            try
            {
                await _emailService.SendEmailAsync(
                    new EmailMessage
                    {
                        ToAddresses =
                        [
                            new EmailRecipient
                            {
                                Address = request.UploadedByEmail,
                                Name = request.UploadedByDisplayName
                            }
                        ],
                        Subject = $"Taskverse bulk upload summary ({result.CreatedCount} students created)",
                        HtmlBody = BuildSummaryEmailBody(request.UploadedByDisplayName, createdUsers)
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.SummaryEmailSent = false;
                result.SummaryEmailWarning = "Students were created, but we could not send the temporary-password summary email. Please reset passwords for these students before sharing access.";
                _logger.LogWarning(
                    ex,
                    "Bulk student upload created {CreatedCount} students for college {CollegeId}, but the summary email could not be sent to {UploadedByEmail}.",
                    result.CreatedCount,
                    request.RestrictedCollegeId,
                    request.UploadedByEmail);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            await RollbackTransactionAsync(transaction, cancellationToken);
            _logger.LogError(ex, "Bulk student upload failed while saving students to the database for college {CollegeId}.", request.RestrictedCollegeId);
            throw new InvalidOperationException("We could not save the uploaded students because the database operation failed. Please try again.", ex);
        }
        catch (DbException ex)
        {
            await RollbackTransactionAsync(transaction, cancellationToken);
            _logger.LogError(ex, "Bulk student upload failed while reading or writing student data for college {CollegeId}.", request.RestrictedCollegeId);
            throw new InvalidOperationException("We could not process the upload because the database call failed. Please try again.", ex);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private static async Task RollbackTransactionAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is null)
        {
            return;
        }

        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch
        {
            // The original database exception is the most important failure to preserve.
        }
    }

    private async Task<List<UploadRowContext>> PrepareRowsAsync(
        List<UploadRowContext> rows,
        Guid? restrictedCollegeId,
        TaskverseContext context,
        List<BulkStudentUploadRowIssueDto> invalidRows,
        CancellationToken cancellationToken)
    {
        var preparedRows = new List<UploadRowContext>();
        var classLookupByCollege = new Dictionary<Guid, Dictionary<string, List<Class>>>();
        var collegesById = new Dictionary<Guid, College>();
        var collegesByNormalizedName = new Dictionary<string, List<College>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var resolvedCollege = await ResolveCollegeAsync(
                row,
                restrictedCollegeId,
                context,
                collegesById,
                collegesByNormalizedName,
                invalidRows,
                cancellationToken);

            if (resolvedCollege is null)
            {
                continue;
            }

            row.Row.CollegeId = resolvedCollege.CollegeId.ToString();
            row.Row.CollegeName = resolvedCollege.CollegeName?.Trim() ?? row.Row.CollegeName?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(row.Row.ClassId) || string.IsNullOrWhiteSpace(row.Row.ClassName))
            {
                preparedRows.Add(row);
                continue;
            }

            var classesByNormalizedName = await GetClassesByCollegeAsync(
                resolvedCollege.CollegeId,
                context,
                classLookupByCollege,
                cancellationToken);

            var normalizedClassName = NormalizeClassName(row.Row.ClassName);
            if (normalizedClassName.Length == 0)
            {
                invalidRows.Add(new BulkStudentUploadRowIssueDto
                {
                    RowNumber = row.RowNumber,
                    Email = row.Row.Email,
                    Message = "Class is required."
                });
                continue;
            }

            if (!classesByNormalizedName.TryGetValue(normalizedClassName, out var matchingClasses))
            {
                var createdClass = new Class
                {
                    ClassId = Guid.NewGuid(),
                    CollegeId = resolvedCollege.CollegeId,
                    Name = row.Row.ClassName.Trim(),
                    AcademicYear = null,
                    Description = null,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                };

                context.Classes.Add(createdClass);
                classesByNormalizedName[normalizedClassName] = [createdClass];
                row.Row.ClassId = createdClass.ClassId.ToString();
                preparedRows.Add(row);
                continue;
            }

            if (matchingClasses.Count > 1)
            {
                invalidRows.Add(new BulkStudentUploadRowIssueDto
                {
                    RowNumber = row.RowNumber,
                    Email = row.Row.Email,
                    Message = $"Multiple classes named '{row.Row.ClassName.Trim()}' already exist for this college. Please create or map the class manually."
                });
                continue;
            }

            row.Row.ClassId = matchingClasses[0].ClassId.ToString();
            preparedRows.Add(row);
        }

        return preparedRows;
    }

    private async Task<College?> ResolveCollegeAsync(
        UploadRowContext row,
        Guid? restrictedCollegeId,
        TaskverseContext context,
        Dictionary<Guid, College> collegesById,
        Dictionary<string, List<College>> collegesByNormalizedName,
        List<BulkStudentUploadRowIssueDto> invalidRows,
        CancellationToken cancellationToken)
    {
        if (restrictedCollegeId.HasValue)
        {
            var collegeId = restrictedCollegeId.Value;
            if (collegesById.TryGetValue(collegeId, out var cachedRestrictedCollege))
            {
                return cachedRestrictedCollege;
            }

            var restrictedCollege = await context.Colleges
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.CollegeId == collegeId, cancellationToken);

            if (restrictedCollege is null)
            {
                invalidRows.Add(new BulkStudentUploadRowIssueDto
                {
                    RowNumber = row.RowNumber,
                    Email = row.Row.Email,
                    Message = "The current college could not be resolved for this upload."
                });
                return null;
            }

            collegesById[collegeId] = restrictedCollege;
            return restrictedCollege;
        }

        var rawCollegeId = row.Row.CollegeId?.Trim() ?? string.Empty;
        if (Guid.TryParse(rawCollegeId, out var parsedCollegeId))
        {
            if (collegesById.TryGetValue(parsedCollegeId, out var cachedCollegeById))
            {
                return cachedCollegeById;
            }

            var collegeById = await context.Colleges
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.CollegeId == parsedCollegeId, cancellationToken);

            if (collegeById is null)
            {
                invalidRows.Add(new BulkStudentUploadRowIssueDto
                {
                    RowNumber = row.RowNumber,
                    Email = row.Row.Email,
                    Message = "CollegeId was not found."
                });
                return null;
            }

            collegesById[parsedCollegeId] = collegeById;
            return collegeById;
        }

        var normalizedCollegeName = NormalizeCollegeName(row.Row.CollegeName);
        if (normalizedCollegeName.Length == 0)
        {
            invalidRows.Add(new BulkStudentUploadRowIssueDto
            {
                RowNumber = row.RowNumber,
                Email = row.Row.Email,
                Message = "CollegeId or College Name is required."
            });
            return null;
        }

        if (!collegesByNormalizedName.TryGetValue(normalizedCollegeName, out var matchingColleges))
        {
            matchingColleges = await context.Colleges
                .AsNoTracking()
                .Where(item => item.CollegeName != null && item.CollegeName.ToLower() == normalizedCollegeName)
                .ToListAsync(cancellationToken);
            collegesByNormalizedName[normalizedCollegeName] = matchingColleges;
        }

        if (matchingColleges.Count == 0)
        {
            invalidRows.Add(new BulkStudentUploadRowIssueDto
            {
                RowNumber = row.RowNumber,
                Email = row.Row.Email,
                Message = $"College '{row.Row.CollegeName.Trim()}' was not found."
            });
            return null;
        }

        if (matchingColleges.Count > 1)
        {
            invalidRows.Add(new BulkStudentUploadRowIssueDto
            {
                RowNumber = row.RowNumber,
                Email = row.Row.Email,
                Message = $"Multiple colleges named '{row.Row.CollegeName.Trim()}' were found. Please use CollegeId."
            });
            return null;
        }

        var resolvedCollege = matchingColleges[0];
        collegesById[resolvedCollege.CollegeId] = resolvedCollege;
        return resolvedCollege;
    }

    private static async Task<Dictionary<string, List<Class>>> GetClassesByCollegeAsync(
        Guid collegeId,
        TaskverseContext context,
        Dictionary<Guid, Dictionary<string, List<Class>>> classLookupByCollege,
        CancellationToken cancellationToken)
    {
        if (classLookupByCollege.TryGetValue(collegeId, out var cachedLookup))
        {
            return cachedLookup;
        }

        var existingClasses = await context.Classes
            .Where(item => item.CollegeId == collegeId)
            .ToListAsync(cancellationToken);

        var lookup = existingClasses
            .GroupBy(item => NormalizeClassName(item.Name))
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        classLookupByCollege[collegeId] = lookup;
        return lookup;
    }

    private static bool TryValidateRow(
        UploadRowContext context,
        Guid? restrictedCollegeId,
        IReadOnlyDictionary<Guid, College> colleges,
        IReadOnlyDictionary<Guid, Class> classes,
        IReadOnlyDictionary<Guid, Batch> batches,
        out string validationMessage,
        out Guid collegeId,
        out Guid? classId,
        out Guid? batchId)
    {
        collegeId = Guid.Empty;
        classId = null;
        batchId = null;

        if (string.IsNullOrWhiteSpace(context.Row.FullName))
        {
            validationMessage = "FullName is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(NormalizeEmail(context.Row.Email)))
        {
            validationMessage = "Email is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.Row.Phone))
        {
            validationMessage = "Phone is required.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(context.Row.EnrollmentNumber) &&
            context.Row.EnrollmentNumber.Trim().Length > EnrollmentNumberMaxLength)
        {
            validationMessage = $"EnrollmentNumber must not exceed {EnrollmentNumberMaxLength} characters.";
            return false;
        }

        if (!Guid.TryParse(context.Row.CollegeId, out collegeId))
        {
            validationMessage = "CollegeId is invalid.";
            return false;
        }

        var rawClassId = context.Row.ClassId?.Trim() ?? string.Empty;
        var rawBatchId = context.Row.BatchId?.Trim() ?? string.Empty;
        var hasClassId = !string.IsNullOrWhiteSpace(rawClassId);
        var hasBatchId = !string.IsNullOrWhiteSpace(rawBatchId);
        Guid parsedClassId = Guid.Empty;
        Guid parsedBatchId = Guid.Empty;

        if (hasBatchId && !hasClassId)
        {
            validationMessage = "BatchId cannot be provided without ClassId.";
            return false;
        }

        if (hasClassId && !Guid.TryParse(rawClassId, out parsedClassId))
        {
            validationMessage = "ClassId is invalid.";
            return false;
        }

        if (hasBatchId && !Guid.TryParse(rawBatchId, out parsedBatchId))
        {
            validationMessage = "BatchId is invalid.";
            return false;
        }

        if (hasClassId)
        {
            classId = parsedClassId;
            batchId = hasBatchId ? parsedBatchId : null;
        }

        if (!hasClassId && !string.IsNullOrWhiteSpace(context.Row.ClassName))
        {
            validationMessage = "Class could not be resolved for this row.";
            return false;
        }

        if (restrictedCollegeId.HasValue && restrictedCollegeId.Value != collegeId)
        {
            validationMessage = "College admins can upload students only for their own college.";
            return false;
        }

        if (!colleges.ContainsKey(collegeId))
        {
            validationMessage = "CollegeId was not found.";
            return false;
        }

        if (!hasClassId)
        {
            validationMessage = string.Empty;
            return true;
        }

        if (!classes.TryGetValue(classId!.Value, out var classEntity) || classEntity.CollegeId != collegeId)
        {
            validationMessage = "ClassId does not belong to the selected college.";
            return false;
        }

        if (!hasBatchId)
        {
            validationMessage = string.Empty;
            return true;
        }

        if (!batches.TryGetValue(batchId!.Value, out var batchEntity) || batchEntity.ClassId != classId.Value || batchEntity.CollegeId != collegeId)
        {
            validationMessage = "BatchId does not belong to the selected class and college.";
            return false;
        }

        validationMessage = string.Empty;
        return true;
    }

    private static string NormalizeEmail(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizeClassName(string? className) =>
        (className ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizeCollegeName(string? collegeName) =>
        (collegeName ?? string.Empty).Trim().ToLowerInvariant();

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;

    private static string BuildSummaryEmailBody(string uploaderName, IEnumerable<CreatedStudentCredential> createdUsers)
    {
        var rows = string.Join(string.Empty, createdUsers.Select(user =>
            $"<tr><td>{WebUtility.HtmlEncode(user.FullName)}</td><td>{WebUtility.HtmlEncode(user.Email)}</td><td>{WebUtility.HtmlEncode(user.TemporaryPassword)}</td></tr>"));

        return $"""
            <html>
              <body style="font-family: Arial, sans-serif; color: #0f172a;">
                <p>Hello {WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(uploaderName) ? "Admin" : uploaderName)},</p>
                <p>Your Taskverse bulk student upload has completed successfully. The temporary passwords are listed below.</p>
                <table style="border-collapse: collapse; width: 100%;">
                  <thead>
                    <tr>
                      <th style="border: 1px solid #cbd5e1; padding: 8px; text-align: left;">Full Name</th>
                      <th style="border: 1px solid #cbd5e1; padding: 8px; text-align: left;">Email</th>
                      <th style="border: 1px solid #cbd5e1; padding: 8px; text-align: left;">Temporary Password</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rows}
                  </tbody>
                </table>
                <p style="margin-top: 16px;">Students must change this password on their first sign-in before they can access the platform.</p>
              </body>
            </html>
            """;
    }

    private sealed record UploadRowContext(int RowNumber, BulkStudentUploadRowDto Row);

    private sealed record CreatedStudentCredential(string FullName, string Email, string TemporaryPassword);

    private static class TemporaryPasswordGenerator
    {
        public static string Generate()
        {
            const int length = 14;
            const string allowed = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(length);
            var chars = bytes.Select(value => allowed[value % allowed.Length]).ToArray();
            return new string(chars);
        }
    }
}
