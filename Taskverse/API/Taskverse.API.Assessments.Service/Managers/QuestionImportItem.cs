using Taskverse.API.Assessments.Service.Models;

namespace Taskverse.API.Assessments.Service.Managers;

public sealed class QuestionImportItem
{
    public int InputOrder { get; init; }
    public int SourceRowNumber { get; init; }
    public CreateQuestionRequest Request { get; init; } = default!;
}
