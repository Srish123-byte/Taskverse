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

    public static UpdateCollegeClassDto ToDto(this UpdateCollegeClassRequest model) => new()
    {
        Name = model.Name,
        AcademicYear = model.AcademicYear,
        Department = model.Department
    };

    public static CreateCollegeBatchDto ToDto(this CreateCollegeBatchRequest model) => new()
    {
        Name = model.Name,
        Description = model.Description,
        Capacity = model.Capacity,
        SubjectId = model.SubjectId,
        SubjectName = model.SubjectName
    };

    public static UpdateCollegeBatchDto ToDto(this UpdateCollegeBatchRequest model) => new()
    {
        Name = model.Name,
        Description = model.Description,
        Capacity = model.Capacity,
        SubjectId = model.SubjectId,
        SubjectName = model.SubjectName
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
        dto.Description,
        dto.SubjectId,
        dto.SubjectName,
        dto.Capacity,
        dto.StudentCount,
        dto.CreatedAt,
        dto.AssignedTrainers.Select(trainer => trainer.ToModel()).ToList(),
        dto.AssignedStudents.Select(student => student.ToModel()).ToList());

    public static SubjectOptionRecord ToModel(this SubjectOptionDto dto) => new(
        dto.SubjectId,
        dto.SubjectName);

    public static ApprovedTrainerRecord ToModel(this ApprovedTrainerDto dto) => new(
        dto.TrainerId,
        dto.UserId,
        dto.FullName,
        dto.Email);

    public static ApprovedStudentRecord ToModel(this ApprovedStudentDto dto) => new(
        dto.StudentId,
        dto.UserId,
        dto.FullName,
        dto.Email,
        dto.CurrentClassId,
        dto.CurrentClassName,
        dto.CurrentBatchId,
        dto.CurrentBatchName);

    public static AssignBatchTrainersDto ToDto(this AssignBatchTrainersRequest model) => new()
    {
        TrainerIds = model.TrainerIds ?? []
    };

    public static AssignStudentToBatchDto ToDto(this AssignStudentToBatchRequest model) => new()
    {
        StudentIds = model.StudentIds ?? []
    };

    public static PendingUserRecord ToModel(this PendingUserDto dto) => new(
        dto.UserId,
        dto.FullName,
        dto.Email,
        dto.Role,
        dto.Status,
        dto.CreatedAt,
        dto.InstitutionName);
}
