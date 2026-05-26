using Taskverse.API.Reports.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Reports.Service.Services;

public interface IResultEvaluationStrategy
{
    bool CanHandle(string questionType);
    QuestionEvaluationResult Evaluate(
        AssessmentQuestionEvaluationContext question,
        AttemptAnswer? attemptAnswer);
}
