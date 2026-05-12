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
            CollegeId  = entity.CollegeId,
            Role       = entity.Role,
            Status     = entity.Status.ToString(),
            CreatedAt  = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt
        };

    public static PagedUserDto ToDto(this PagedUserResultModel model)
        => new()
        {
            Items      = model.Items.Select(u => u.ToDto()).ToList(),
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
