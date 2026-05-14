using Taskverse.API.Users.Service.DTOs;
using Taskverse.API.Users.Service.Models;

namespace Taskverse.API.Users.Service.Mappings;

internal static class PendingUserMappings
{
    private const string GlobalInstitutionName = "Global System Access";

    public static PendingUserDto ToPendingUserDto(
        this PendingUserProjection projection)
    {
        return new PendingUserDto(
            projection.UserId,
            projection.FullName,
            projection.Email,
            projection.Role,
            projection.Status,
            projection.CreatedAt,
            string.IsNullOrWhiteSpace(projection.CollegeName) ? GlobalInstitutionName : projection.CollegeName);
    }

    public static PendingUserResponseModel ToPendingUserResponseModel(
        this PendingUserDto dto)
    {
        return new PendingUserResponseModel(
            dto.UserId,
            dto.FullName,
            dto.Email,
            dto.Role,
            dto.Status,
            dto.CreatedAt,
            dto.InstitutionName);
    }
}

internal record PendingUserProjection(
    string UserId,
    string FullName,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    string? CollegeName);
