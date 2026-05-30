namespace Taskverse.API.Reports.Service.Models;

public class ResultEvaluationSettings
{
    public const string SectionName = "ResultEvaluation";

    public decimal PassingPercentage { get; set; } = 50m;
}
