namespace Taskverse.API.Reports.Service.Services;

public interface IResultEvaluationStrategyFactory
{
    IResultEvaluationStrategy Resolve(string questionType);
}

public class ResultEvaluationStrategyFactory : IResultEvaluationStrategyFactory
{
    private readonly IReadOnlyCollection<IResultEvaluationStrategy> _strategies;

    public ResultEvaluationStrategyFactory(IEnumerable<IResultEvaluationStrategy> strategies)
    {
        _strategies = strategies.ToArray();
    }

    public IResultEvaluationStrategy Resolve(string questionType)
    {
        var strategy = _strategies.FirstOrDefault(item => item.CanHandle(questionType));
        if (strategy is null)
        {
            throw new InvalidOperationException(
                $"No result evaluation strategy is registered for question type '{questionType}'.");
        }

        return strategy;
    }
}
