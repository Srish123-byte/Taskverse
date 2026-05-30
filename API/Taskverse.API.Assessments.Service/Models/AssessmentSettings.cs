namespace Taskverse.API.Assessments.Service.Models;

public class AssessmentSettings
{
    public bool IsShuffleOn { get; set; }
    public bool IsResultsAvailableImmediately { get; set; } = true;
    public decimal NonCodingTimePerQuestionMinutes { get; set; } = 1.5m;
    public decimal CodingTimePerQuestionMinutes { get; set; } = 20m;
    public bool IsTotalMarksAutoCalculated { get; set; } = true;
}
