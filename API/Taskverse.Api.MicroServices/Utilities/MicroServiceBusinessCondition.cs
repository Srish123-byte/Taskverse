namespace Taskverse.Api.MicroServices.Utilities;

public static class MicroServiceBusinessCondition
{
    public const string AddressNotFound = "MicroService:AddressNotFound";
    public const string ServiceUnavailable = "MicroService:ServiceUnavailable";
    public const string RequestTimeout = "MicroService:RequestTimeout";
    public const string UnauthorizedAccess = "MicroService:UnauthorizedAccess";
}
