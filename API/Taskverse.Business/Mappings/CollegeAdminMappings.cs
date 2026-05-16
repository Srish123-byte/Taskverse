using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Business.Mappings;

public static class CollegeAdminMappings
{
    public static CreateCollegeClassModel ToMicroServiceModel(this CreateCollegeClassDto dto) => new(
        dto.Name,
        dto.AcademicYear,
        dto.Department);

    public static CreateCollegeBatchModel ToMicroServiceModel(this CreateCollegeBatchDto dto) => new(
        dto.Name,
        dto.Description,
        dto.Capacity);

    public static CollegeUserActionModel ToMicroServiceModel(this UserActionDto dto) => new(
        dto.PerformedBy,
        dto.PerformedByUserId,
        dto.Reason);

    public static CollegeClassSummaryDto ToDto(this CollegeClassSummaryModel model) => new()
    {
        ClassId = model.ClassId,
        CollegeId = model.CollegeId,
        Name = model.Name,
        AcademicYear = model.AcademicYear,
        Department = model.Department,
        TotalStudents = model.TotalStudents,
        TotalCapacity = model.TotalCapacity,
        CreatedAt = model.CreatedAt,
        Batches = model.Batches.Select(batch => batch.ToDto()).ToList()
    };

    public static CollegeBatchSummaryDto ToDto(this CollegeBatchSummaryModel model) => new()
    {
        BatchId = model.BatchId,
        ClassId = model.ClassId,
        CollegeId = model.CollegeId,
        Name = model.Name,
        Description = model.Description,
        Capacity = model.Capacity,
        StudentCount = model.StudentCount,
        CreatedAt = model.CreatedAt
    };
}
