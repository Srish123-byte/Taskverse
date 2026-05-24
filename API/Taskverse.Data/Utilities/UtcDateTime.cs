namespace Taskverse.Data.Utilities;

public static class UtcDateTime
{
    public static DateTime Normalize(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static DateTime? Normalize(DateTime? value)
        => value.HasValue ? Normalize(value.Value) : null;
}
