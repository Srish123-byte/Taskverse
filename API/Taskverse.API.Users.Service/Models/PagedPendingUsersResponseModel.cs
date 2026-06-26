namespace Taskverse.API.Users.Service.Models;

public record PagedPendingUsersResponseModel(
    List<PendingUserResponseModel> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
