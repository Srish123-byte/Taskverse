using Taskverse.Api.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Api.Mappings;

public static class UserMappings
{
    public static CreateUserDto ToDto(this CreateUserRequestModel model) => new()
    {
        Email = model.Email,
        FirstName = model.FirstName,
        LastName = model.LastName,
        Role = model.Role,
        Password = model.Password
    };

    public static UpdateUserDto ToDto(this UpdateUserRequestModel model) => new()
    {
        FirstName = model.FirstName,
        LastName = model.LastName,
        IsActive = model.IsActive
    };

    public static UserResponseModel ToResponseModel(this UserDto dto) => new()
    {
        UserId = dto.UserId,
        Email = dto.Email,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Role = dto.Role,
        IsActive = dto.IsActive,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt
    };

    public static PagedUserResponseModel ToResponseModel(this PagedUserDto dto) => new()
    {
        Items = dto.Items.Select(u => u.ToResponseModel()).ToList(),
        TotalCount = dto.TotalCount,
        PageNumber = dto.PageNumber,
        PageSize = dto.PageSize
    };
}
