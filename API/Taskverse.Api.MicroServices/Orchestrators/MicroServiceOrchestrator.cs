using CorrelationId.Abstractions;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;
using Taskverse.Api.MicroServices.Utilities;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator : IMicroServiceOrchestrator
{
    private const string ClientName = "TaskverseMicroServiceClient";
    private const string XCorrelationIdKey = "X-CorrelationId";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly ILog _log;

    private readonly string _baseUrl;
    private readonly string _baseUrlDev;
    private readonly bool _useLocalMicroservices;
    private readonly int _serviceTimeoutSeconds;

    public MicroServiceOrchestrator(
        IHttpClientFactory httpClientFactory,
        ICorrelationContextAccessor correlationContextAccessor,
        IOptions<MicroServiceSettings> microServiceSettings)
    {
        _httpClientFactory = httpClientFactory;
        _correlationContextAccessor = correlationContextAccessor;
        _log = LogManager.GetLogger(typeof(MicroServiceOrchestrator));

        var settings = microServiceSettings.Value ?? throw new InvalidOperationException("MicroServiceSettings are not configured.");

        _baseUrl = NormalizeBaseUrl(settings.BaseUrl);
        _baseUrlDev = NormalizeBaseUrl(settings.BaseUrlDev);
        _useLocalMicroservices = settings.UseLocalMicroservices;
        _serviceTimeoutSeconds = settings.ServiceTimeoutSeconds > 0 ? settings.ServiceTimeoutSeconds : 60;
    }

    public string GetMicroServiceUrl(MicroService microService)
    {
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        if (isDevelopment && _useLocalMicroservices)
        {
            var port = (int)microService;
            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                throw new InvalidOperationException("MicroServiceSettings:BaseUrl is missing for local microservice routing.");
            }

            return $"{_baseUrl}:{port}/";
        }

        if (isDevelopment)
        {
            if (string.IsNullOrWhiteSpace(_baseUrlDev))
            {
                throw new InvalidOperationException("MicroServiceSettings:BaseUrlDev is missing for development microservice routing.");
            }

            return $"{_baseUrlDev}/{microService}/";
        }

        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            throw new InvalidOperationException("MicroServiceSettings:BaseUrl is missing for microservice routing.");
        }

        return $"{_baseUrl}/{microService}/";
    }

    private static string NormalizeBaseUrl(string? baseUrl)
        => string.IsNullOrWhiteSpace(baseUrl) ? string.Empty : baseUrl.TrimEnd('/');

    private void PrepareClient(HttpClient client, Uri uri)
    {
        client.Timeout = TimeSpan.FromSeconds(_serviceTimeoutSeconds);
        client.BaseAddress = new Uri($"{uri.Scheme}://{uri.Authority}");

        var correlationId = _correlationContextAccessor.CorrelationContext?.CorrelationId ?? string.Empty;

        if (client.DefaultRequestHeaders.Contains(XCorrelationIdKey))
        {
            client.DefaultRequestHeaders.Remove(XCorrelationIdKey);
        }

        client.DefaultRequestHeaders.Add(XCorrelationIdKey, correlationId);
    }

    private string GetCorrelationId() =>
        _correlationContextAccessor.CorrelationContext?.CorrelationId ?? string.Empty;

    private void LogRequestStart(HttpMethod method, Uri uri, object? payload = null)
    {
        if (!_log.IsDebugEnabled)
        {
            return;
        }

        var payloadType = payload?.GetType().Name ?? "none";
        _log.Debug(
            $"[MicroServiceOrchestrator] {method} -> {uri} | correlationId={GetCorrelationId()} | timeoutSeconds={_serviceTimeoutSeconds} | payloadType={payloadType}");
    }

    private void LogRequestCompletion(HttpMethod method, Uri uri, HttpStatusCode statusCode, long elapsedMilliseconds)
    {
        if (!_log.IsDebugEnabled)
        {
            return;
        }

        _log.Debug(
            $"[MicroServiceOrchestrator] {method} <- {uri} | statusCode={(int)statusCode} ({statusCode}) | elapsedMs={elapsedMilliseconds} | correlationId={GetCorrelationId()}");
    }

    private Uri GetValidatedUri(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _log.Error($"[MicroServiceOrchestrator] Invalid or non-HTTP/HTTPS URL: {url}");
            throw new InvalidOperationException(MicroServiceBusinessCondition.AddressNotFound);
        }

        return uri;
    }

    private async Task<ObjectResult> GetResult<T>(HttpResponseMessage response, string url)
    {
        var content = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(content);
                return new ObjectResult(result) { StatusCode = statusCode };
            }
            catch (Exception ex)
            {
                _log.Error($"[MicroServiceOrchestrator] Deserialization error for URL {url}: {ex.Message}", ex);
                return new ObjectResult(content) { StatusCode = statusCode };
            }
        }

        try
        {
            var errorModel = JsonConvert.DeserializeObject<ErrorModel>(content);
            if (errorModel is not null)
            {
                var validationErrors = GetValidationErrors(errorModel);
                var message = string.IsNullOrWhiteSpace(validationErrors)
                    ? errorModel.Message
                    : $"{errorModel.Message} | {validationErrors}";

                return new ObjectResult(new { errorModel.Name, Message = message }) { StatusCode = statusCode };
            }
        }
        catch (Exception ex)
        {
            _log.Error($"[MicroServiceOrchestrator] Error model deserialization failed for URL {url}: {ex.Message}", ex);
        }

        return new ObjectResult(content) { StatusCode = statusCode };
    }

    private static string GetValidationErrors(ErrorModel errorModel)
    {
        if (errorModel.Errors is null || errorModel.Errors.Count == 0)
            return string.Empty;

        return string.Join(" | ", errorModel.Errors.Select(e => e.Message));
    }

    public async Task<ObjectResult> Get<T>(string url)
    {
        var uri = GetValidatedUri(url);
        var client = _httpClientFactory.CreateClient(ClientName);
        PrepareClient(client, uri);
        LogRequestStart(HttpMethod.Get, uri);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await client.GetAsync(uri);
            stopwatch.Stop();
            LogRequestCompletion(HttpMethod.Get, uri, response.StatusCode, stopwatch.ElapsedMilliseconds);
            return await GetResult<T>(response, url);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] GET connection failed for URL {url}: {ex.Message}", ex);
            return CreateServiceUnavailableResult(url, ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] GET request failed for URL {url}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<ObjectResult> Post<T>(string url, object postData)
    {
        var uri = GetValidatedUri(url);
        var client = _httpClientFactory.CreateClient(ClientName);
        PrepareClient(client, uri);
        LogRequestStart(HttpMethod.Post, uri, postData);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var json = JsonConvert.SerializeObject(postData);
            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            stopwatch.Stop();
            LogRequestCompletion(HttpMethod.Post, uri, response.StatusCode, stopwatch.ElapsedMilliseconds);
            return await GetResult<T>(response, url);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] POST connection failed for URL {url}: {ex.Message}", ex);
            return CreateServiceUnavailableResult(url, ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] POST request failed for URL {url}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<ObjectResult> Put<T>(string url, object postData)
    {
        var uri = GetValidatedUri(url);
        var client = _httpClientFactory.CreateClient(ClientName);
        PrepareClient(client, uri);
        LogRequestStart(HttpMethod.Put, uri, postData);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var json = JsonConvert.SerializeObject(postData);
            using var request = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            stopwatch.Stop();
            LogRequestCompletion(HttpMethod.Put, uri, response.StatusCode, stopwatch.ElapsedMilliseconds);
            return await GetResult<T>(response, url);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] PUT connection failed for URL {url}: {ex.Message}", ex);
            return CreateServiceUnavailableResult(url, ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] PUT request failed for URL {url}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<ObjectResult> Patch<T>(string url, object patchData)
    {
        var uri = GetValidatedUri(url);
        var client = _httpClientFactory.CreateClient(ClientName);
        PrepareClient(client, uri);
        LogRequestStart(HttpMethod.Patch, uri, patchData);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var json = JsonConvert.SerializeObject(patchData);
            using var request = new HttpRequestMessage(HttpMethod.Patch, uri)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            stopwatch.Stop();
            LogRequestCompletion(HttpMethod.Patch, uri, response.StatusCode, stopwatch.ElapsedMilliseconds);
            return await GetResult<T>(response, url);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] PATCH connection failed for URL {url}: {ex.Message}", ex);
            return CreateServiceUnavailableResult(url, ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] PATCH request failed for URL {url}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<ObjectResult> Delete(string url)
    {
        var uri = GetValidatedUri(url);
        var client = _httpClientFactory.CreateClient(ClientName);
        PrepareClient(client, uri);
        LogRequestStart(HttpMethod.Delete, uri);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await client.DeleteAsync(uri);
            stopwatch.Stop();
            LogRequestCompletion(HttpMethod.Delete, uri, response.StatusCode, stopwatch.ElapsedMilliseconds);
            return new ObjectResult(null) { StatusCode = (int)response.StatusCode };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] DELETE connection failed for URL {url}: {ex.Message}", ex);
            return CreateServiceUnavailableResult(url, ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _log.Error($"[MicroServiceOrchestrator] DELETE request failed for URL {url}: {ex.Message}", ex);
            throw;
        }
    }

    private static ObjectResult CreateServiceUnavailableResult(string url, HttpRequestException ex)
    {
        return new ObjectResult(new
        {
            Name = "MicroServiceUnavailable",
            Message = $"Unable to reach microservice at {url}. Ensure the target local service is running.",
            Detail = ex.Message
        })
        {
            StatusCode = (int)HttpStatusCode.ServiceUnavailable
        };
    }
}
