using Taskverse.Data.DataAccess;

namespace Taskverse.API.Assessments.Service.Managers;

public sealed class QuestionImportItem
{
    public int SourceRowNumber { get; init; }
    public Question Question { get; init; } = default!;
}
