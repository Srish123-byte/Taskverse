namespace Taskverse.API.Assessments.Service.Services;

public interface IStudentAttemptAnswerSaveStrategyFactory
{
    IStudentAttemptAnswerSaveStrategy Resolve(string questionType);
}

public class StudentAttemptAnswerSaveStrategyFactory : IStudentAttemptAnswerSaveStrategyFactory
{
    private readonly IReadOnlyCollection<IStudentAttemptAnswerSaveStrategy> _strategies;

    public StudentAttemptAnswerSaveStrategyFactory(IEnumerable<IStudentAttemptAnswerSaveStrategy> strategies)
    {
        _strategies = strategies.ToArray();
    }

    public IStudentAttemptAnswerSaveStrategy Resolve(string questionType)
    {
        var strategy = _strategies.FirstOrDefault(item => item.CanHandle(questionType));
        if (strategy is null)
        {
            throw new InvalidOperationException(
                $"No answer save strategy is registered for question type '{questionType}'.");
        }

        return strategy;
    }
}
