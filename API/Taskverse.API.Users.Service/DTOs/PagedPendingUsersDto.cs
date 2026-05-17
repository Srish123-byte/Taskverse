namespace Taskverse.API.Users.Service.DTOs;

public record PagedPendingUsersDto(
    List<PendingUserDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
