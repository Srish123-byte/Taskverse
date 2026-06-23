using System.Text.Json.Serialization;

namespace Taskverse.API.CodingEngine.Service.Models;

public record EditorStateResponse(
    [property: JsonPropertyName("coding_question")]
    CodingQuestionDetail CodingQuestion,
    [property: JsonPropertyName("languages")]
    List<AvailableLanguage> Languages,
    [property: JsonPropertyName("starter_codes")]
    List<StarterCodeDetail> StarterCodes,
    [property: JsonPropertyName("student_code")]
    string? StudentCode,
    [property: JsonPropertyName("selected_language_id")]
    Guid? SelectedLanguageId,
    [property: JsonPropertyName("settings")]
    CodingSettingsDetail Settings);

public record CodingQuestionDetail(
    [property: JsonPropertyName("coding_question_id")]
    Guid CodingQuestionId,
    [property: JsonPropertyName("question_title")]
    string QuestionTitle,
    [property: JsonPropertyName("problem_statement")]
    string ProblemStatement,
    [property: JsonPropertyName("detailed_description")]
    string? DetailedDescription,
    [property: JsonPropertyName("difficulty_level")]
    int DifficultyLevel,
    [property: JsonPropertyName("input_format")]
    string? InputFormat,
    [property: JsonPropertyName("output_format")]
    string? OutputFormat,
    [property: JsonPropertyName("constraints_text")]
    string? ConstraintsText,
    [property: JsonPropertyName("explanation")]
    string? Explanation,
    [property: JsonPropertyName("examples")]
    string? Examples,
    [property: JsonPropertyName("marks")]
    decimal Marks,
    [property: JsonPropertyName("default_language_code")]
    string? DefaultLanguageCode);

public record AvailableLanguage(
    [property: JsonPropertyName("coding_language_id")]
    Guid CodingLanguageId,
    [property: JsonPropertyName("language_code")]
    string LanguageCode,
    [property: JsonPropertyName("display_name")]
    string DisplayName,
    [property: JsonPropertyName("monaco_language_code")]
    string MonacoLanguageCode,
    [property: JsonPropertyName("file_extension")]
    string? FileExtension);

public record StarterCodeDetail(
    [property: JsonPropertyName("coding_language_id")]
    Guid CodingLanguageId,
    [property: JsonPropertyName("starter_code")]
    string StarterCode);

public record CodingSettingsDetail(
    [property: JsonPropertyName("time_limit_ms")]
    int TimeLimitMs,
    [property: JsonPropertyName("memory_limit_kb")]
    int MemoryLimitKb,
    [property: JsonPropertyName("max_code_size_kb")]
    int MaxCodeSizeKb,
    [property: JsonPropertyName("is_code_execution_enabled")]
    bool IsCodeExecutionEnabled,
    [property: JsonPropertyName("is_submission_enabled")]
    bool IsSubmissionEnabled,
    [property: JsonPropertyName("allow_language_change")]
    bool AllowLanguageChange);
