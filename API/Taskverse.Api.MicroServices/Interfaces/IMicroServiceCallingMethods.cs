using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;

namespace Taskverse.Api.MicroServices.Interfaces;

public interface IMicroServiceCallingMethods
{
    Task<ObjectResult> Post<T>(string url, object postData);
    Task<ObjectResult> Put<T>(string url, object postData);
    Task<ObjectResult> Patch<T>(string url, object patchData);
    Task<ObjectResult> Delete(string url);
    Task<ObjectResult> Get<T>(string url);
    string GetMicroServiceUrl(MicroService microService);
}
