namespace Taskverse.API.Assessments.Service.Managers;

public class AssessmentQuestionLimitException : Exception
{
    public AssessmentQuestionLimitException(string message)
        : base(message)
    {
    }
}
