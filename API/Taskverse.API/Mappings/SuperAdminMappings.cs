using Taskverse.Api.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Api.Mappings;

public static class SuperAdminMappings
{
    public static CollegeActionDto ToDto(this CollegeActionRequestModel model, string performedBy) => new()
    {
        PerformedBy = performedBy,
        Reason = model.Reason
    };

    public static CollegeSearchDto ToDto(this CollegeSearchRequestModel model) => new()
    {
        Query = model.Query,
        Status = string.IsNullOrWhiteSpace(model.Status) ? "all" : model.Status
    };

    public static UserActionDto ToDto(this UserActionRequestModel model, string performedBy, Guid? performedByUserId) => new()
    {
        PerformedBy = performedBy,
        PerformedByUserId = performedByUserId,
        Reason = model.Reason
    };

    public static UserSearchCriteriaDto ToDto(this UserSearchRequestModel model) => new()
    {
        Status    = model.Status,
        Role      = model.Role,
        SearchTerm = model.SearchTerm,
        PageNumber = model.PageNumber > 0 ? model.PageNumber : 1,
        PageSize   = model.PageSize   > 0 ? model.PageSize   : 10
    };

    public static BulkStudentUploadRequestDto ToDto(
        this BulkStudentUploadRequestModel model,
        Guid uploadedByUserId,
        string uploadedByEmail,
        string uploadedByDisplayName,
        Guid? restrictedCollegeId = null) => new()
    {
        UploadedByUserId = uploadedByUserId,
        UploadedByEmail = uploadedByEmail,
        UploadedByDisplayName = uploadedByDisplayName,
        RestrictedCollegeId = restrictedCollegeId,
        Rows = (model.Rows ?? []).Select(row => new BulkStudentUploadRowDto
        {
            FullName = row.FullName,
            Email = row.Email,
            Phone = row.Phone,
            EnrollmentNumber = row.EnrollmentNumber,
            CollegeId = row.CollegeId,
            CollegeName = row.CollegeName,
            ClassId = row.ClassId,
            BatchId = row.BatchId,
            ClassName = row.ClassName
        }).ToList()
    };

    public static PagedUserSearchResponseModel ToResponseModel(this PagedUsersResultDto dto) => new()
    {
        Items      = dto.Items.Select(x => x.ToResponseModel()).ToList(),
        TotalCount = dto.TotalCount,
        PageNumber = dto.PageNumber,
        PageSize   = dto.PageSize
    };

    public static CollegeSearchResponseModel ToResponseModel(this CollegeSearchResultDto dto) => new()
    {
        CollegeId = dto.CollegeId,
        Name = dto.Name,
        City = dto.City,
        State = dto.State,
        AdminName = dto.AdminName,
        AdminEmail = dto.AdminEmail,
        TotalUsers = dto.TotalUsers,
        Status = dto.Status
    };

    public static CollegeResponseModel ToResponseModel(this CollegeDto dto) => new()
    {
        CollegeId = dto.CollegeId,
        Name = dto.Name,
        AdminName = dto.AdminName,
        City = dto.City,
        State = dto.State,
        Status = dto.Status,
        ApprovalStatus = dto.ApprovalStatus,
        IsActive = dto.IsActive,
        RequestedAt = dto.RequestedAt,
        RequestedBy = dto.RequestedBy,
        ApprovedAt = dto.ApprovedAt,
        ApprovedBy = dto.ApprovedBy,
        Notes = dto.Notes
    };

    public static PendingUserResponseModel ToResponseModel(this PendingUserDto dto) => new()
    {
        UserId = dto.UserId,
        FullName = dto.FullName,
        Email = dto.Email,
        Role = dto.Role,
        Status = dto.Status,
        CreatedAt = dto.CreatedAt,
        InstitutionName = dto.InstitutionName
    };

    public static BulkStudentUploadResultResponseModel ToResponseModel(this BulkStudentUploadResultDto dto) => new()
    {
        CreatedCount = dto.CreatedCount,
        DuplicateCount = dto.DuplicateCount,
        InvalidCount = dto.InvalidCount,
        CreatedUsers = dto.CreatedUsers.Select(item => new BulkStudentUploadCreatedUserResponseModel
        {
            FullName = item.FullName,
            Email = item.Email
        }).ToList(),
        DuplicateRows = dto.DuplicateRows.Select(item => new BulkStudentUploadRowIssueResponseModel
        {
            RowNumber = item.RowNumber,
            Email = item.Email,
            Message = item.Message
        }).ToList(),
        InvalidRows = dto.InvalidRows.Select(item => new BulkStudentUploadRowIssueResponseModel
        {
            RowNumber = item.RowNumber,
            Email = item.Email,
            Message = item.Message
        }).ToList()
    };

    public static SuperAdminDashboardResponseModel ToResponseModel(this SuperAdminDashboardDto dto) => new()
    {
        Totals = new SuperAdminTotalsResponseModel
        {
            PendingApprovals = dto.Totals.PendingApprovals,
            ActiveColleges = dto.Totals.ActiveColleges,
            RegisteredStudents = dto.Totals.RegisteredStudents,
            AssessmentsThisMonth = dto.Totals.AssessmentsThisMonth
        }
    };
}
