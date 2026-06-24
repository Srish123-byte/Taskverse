using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.Business.Services;

public class BulkStudentUploadService : IBulkStudentUploadService
{
    private const string StudentRole = "Student";

    private readonly IDbContextFactory<TaskverseContext> _dbContextFactory;
    private readonly IEmailService _emailService;

    public BulkStudentUploadService(
        IDbContextFactory<TaskverseContext> dbContextFactory,
        IEmailService emailService)
    {
        _dbContextFactory = dbContextFactory;
        _emailService = emailService;
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
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

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
        await _emailService.SendEmailAsync(
            new EmailMessage
            {
                ToAddress = request.UploadedByEmail,
                ToName = request.UploadedByDisplayName,
                Subject = $"Taskverse bulk upload summary ({result.CreatedCount} students created)",
                HtmlBody = BuildSummaryEmailBody(request.UploadedByDisplayName, createdUsers)
            },
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return result;
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

        if (hasClassId != hasBatchId)
        {
            validationMessage = "ClassId and BatchId must either both be provided or both be left empty.";
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
            batchId = parsedBatchId;
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
