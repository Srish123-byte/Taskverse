using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Taskverse.Business.Utilities;

public static class ObjectResultExtensions
{
    public static T? DeserializeValue<T>(this ObjectResult result)
    {
        if (result.Value is T typed)
            return typed;

        string json = JsonConvert.SerializeObject(result.Value);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static bool IsSuccess(this ObjectResult result)
        => result.StatusCode is >= 200 and < 300;

    public static void EnsureSuccess(this ObjectResult result, string operationName)
    {
        if (!result.IsSuccess())
            throw new InvalidOperationException($"{operationName} failed with status {result.StatusCode}");
    }
}
