namespace Taskverse.API.Assessments.Service.Models;

public class AssessmentSettings
{
    public bool IsLateEntryAllowed { get; set; }
    public int AssessmentMaxDurationInMinutes { get; set; } = 120;
    public bool IsShuffleOn { get; set; }
    public bool IsResultsAvailableImmediately { get; set; } = true;
    public decimal MarksPerQuestion { get; set; } = 4;
    public decimal NegativeMarksPerQuestion { get; set; } = 1;
    public decimal NonCodingTimePerQuestionMinutes { get; set; } = 1.5m;
    public decimal CodingTimePerQuestionMinutes { get; set; } = 20m;
    public bool IsTotalMarksAutoCalculated { get; set; } = true;
    public bool AllowQuestionReview { get; set; } = true;
    public bool NegativeMarking { get; set; } = true;
    public string? Instructions { get; set; }
}
