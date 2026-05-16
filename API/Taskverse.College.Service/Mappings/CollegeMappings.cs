using Taskverse.API.College.Service.DTOs;
using Taskverse.API.College.Service.Models;

namespace Taskverse.API.College.Service.Mappings;

public static class CollegeMappings
{
    public static CreateCollegeClassDto ToDto(this CreateCollegeClassRequest model) => new()
    {
        Name = model.Name,
        AcademicYear = model.AcademicYear,
        Department = model.Department
    };

    public static CreateCollegeBatchDto ToDto(this CreateCollegeBatchRequest model) => new()
    {
        Name = model.Name,
        Capacity = model.Capacity
    };

    public static CollegeUserActionDto ToDto(this CollegeUserActionRequest model) => new()
    {
        PerformedBy = model.PerformedBy,
        PerformedByUserId = model.PerformedByUserId,
        Reason = model.Reason
    };

    public static CollegeClassSummaryRecord ToModel(this CollegeClassSummaryDto dto) => new(
        dto.ClassId,
        dto.CollegeId,
        dto.Name,
        dto.AcademicYear,
        dto.Department,
        dto.TotalStudents,
        dto.TotalCapacity,
        dto.CreatedAt,
        dto.Batches.Select(batch => batch.ToModel()).ToList());

    public static CollegeBatchSummaryRecord ToModel(this CollegeBatchSummaryDto dto) => new(
        dto.BatchId,
        dto.ClassId,
        dto.CollegeId,
        dto.Name,
        dto.Capacity,
        dto.StudentCount,
        dto.CreatedAt);

    public static PendingUserRecord ToModel(this PendingUserDto dto) => new(
        dto.UserId,
        dto.FullName,
        dto.Email,
        dto.Role,
        dto.Status,
        dto.CreatedAt,
        dto.InstitutionName);
}
