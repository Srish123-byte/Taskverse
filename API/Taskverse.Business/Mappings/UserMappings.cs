using Taskverse.Api.MicroServices.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Business.Mappings;

public static class UserMappings
{
    public static UserDto ToDto(this UserModel model)
        => new()
        {
            UserId = model.UserId,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Role = model.Role,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };

    public static PagedUserDto ToDto(this PagedUserResultModel model)
        => new()
        {
            Items = model.Items.Select(u => u.ToDto()).ToList(),
            TotalCount = model.TotalCount,
            PageNumber = model.PageNumber,
            PageSize = model.PageSize
        };

    public static CreateUserModel ToMicroServiceModel(this CreateUserDto dto)
        => new(dto.Email, dto.FirstName, dto.LastName, dto.Role, dto.Password);

    public static UpdateUserModel ToMicroServiceModel(this UpdateUserDto dto)
        => new(dto.FirstName, dto.LastName, dto.IsActive);
}
