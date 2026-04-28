namespace Taskverse.Api.MicroServices.Models;

public record ErrorModel(string Message, string Name, List<ErrorDetail>? Errors);

public record ErrorDetail(string? Field, string Message);
