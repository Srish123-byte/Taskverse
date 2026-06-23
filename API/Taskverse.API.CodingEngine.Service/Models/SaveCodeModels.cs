using System.Text.Json.Serialization;

namespace Taskverse.API.CodingEngine.Service.Models;

public record SaveCodeRequest(
    [property: JsonPropertyName("code")]
    string Code,
    [property: JsonPropertyName("coding_language_id")]
    Guid CodingLanguageId);

public record SaveCodeResponse(
    [property: JsonPropertyName("status")]
    string Status,
    [property: JsonPropertyName("last_saved_at")]
    DateTime LastSavedAt);
