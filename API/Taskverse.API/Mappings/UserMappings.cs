using Taskverse.Api.Models;
using Taskverse.Business.DTOs;

namespace Taskverse.Api.Mappings;

public static class UserMappings
{
    public static CreateUserDto ToDto(this CreateUserRequestModel model) => new()
    {
        FullName  = model.FullName,
        Email     = model.Email,
        Phone     = model.Phone,
        CollegeId = model.CollegeId,
        Role      = model.Role,
        Password  = model.Password
    };

    public static UpdateUserDto ToDto(this UpdateUserRequestModel model) => new()
    {
        FullName  = model.FullName,
        Phone     = model.Phone,
        CollegeId = model.CollegeId,
        BatchId   = model.BatchId,
        ClassId   = model.ClassId,
        Status    = model.Status
    };

    public static UserResponseModel ToResponseModel(this UserDto dto) => new()
    {
        UserId    = dto.UserId,
        FullName  = dto.FullName,
        Email     = dto.Email,
        Phone     = dto.Phone,
        CollegeId = dto.CollegeId,
        Role      = dto.Role,
        Status    = dto.Status,
        CreatedAt = dto.CreatedAt,
        ModifiedAt = dto.ModifiedAt
    };

    public static PagedUserResponseModel ToResponseModel(this PagedUserDto dto) => new()
    {
        Items      = dto.Items.Select(u => u.ToResponseModel()).ToList(),
        TotalCount = dto.TotalCount,
        PageNumber = dto.PageNumber,
        PageSize   = dto.PageSize
    };
}
