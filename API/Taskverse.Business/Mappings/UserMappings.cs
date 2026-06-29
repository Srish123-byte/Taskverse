using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Mappings;

public static class UserMappings
{
    /// <summary>Maps a microservice UserModel to a business-layer UserDto.</summary>
    public static UserDto ToDto(this UserModel model)
        => new()
        {
            UserId    = model.UserId,
            FullName  = $"{model.FirstName} {model.LastName}".Trim(),
            Email     = model.Email,
            CollegeId = null,
            ClassId   = null,
            BatchId   = null,
            Role      = model.Role,
            Status    = string.Empty,
            CreatedAt = model.CreatedAt,
            ModifiedAt = model.UpdatedAt
        };

    /// <summary>Maps a database User entity directly to a business-layer UserDto.</summary>
    public static UserDto ToDto(this User entity)
        => new()
        {
            UserId     = entity.Id.ToString(),
            FullName   = entity.FullName,
            Email      = entity.Email,
            Phone      = entity.Phone,
            EnrollmentNumber = entity.EnrollmentNumber,
            CollegeId  = entity.CollegeId,
            CollegeName = entity.CollegeName,
            ClassId    = entity.ClassId,
            BatchId    = entity.BatchId,
            Role       = entity.Role,
            Status     = entity.Status.ToString(),
            CreatedAt  = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt
        };

    /// <summary>Maps a PendingUserModel (from the search microservice result) to a UserDto.</summary>
    public static UserDto ToUserDto(this PendingUserModel model)
        => new()
        {
            UserId     = model.UserId,
            FullName   = model.FullName,
            Email      = model.Email,
            Role       = model.Role,
            Status     = model.Status,
            CreatedAt  = model.CreatedAt,
            CollegeName = model.InstitutionName
        };

    public static PagedUserDto ToPagedUserDto(this PagedPendingUserResultModel model)
        => new()
        {
            Items      = model.Items.Select(u => u.ToUserDto()).ToList(),
            TotalCount = model.TotalCount,
            PageNumber = model.PageNumber,
            PageSize   = model.PageSize
        };

    public static CreateUserModel ToMicroServiceModel(this CreateUserDto dto)
    {
        // Split FullName into first/last for the microservice model
        var parts = dto.FullName.Trim().Split(' ', 2);
        var firstName = parts[0];
        var lastName  = parts.Length > 1 ? parts[1] : string.Empty;
        return new CreateUserModel(dto.Email, firstName, lastName, dto.Role, dto.Password);
    }

    public static UpdateUserModel ToMicroServiceModel(this UpdateUserDto dto)
    {
        // Split FullName into first/last if provided
        string? firstName = null;
        string? lastName  = null;
        if (dto.FullName is not null)
        {
            var parts = dto.FullName.Trim().Split(' ', 2);
            firstName = parts[0];
            lastName  = parts.Length > 1 ? parts[1] : string.Empty;
        }
        return new UpdateUserModel(firstName, lastName, null);
    }
}
