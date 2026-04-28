using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface IExamOrchestrator
{
    Task<ExamDto?> GetExam(string examId);
    Task<ExamDto?> CreateExam(CreateExamDto dto);
    Task<List<QuestionDto>?> GetExamQuestions(string examId);
    Task<ExamResultDto?> SubmitExam(ExamSubmissionDto dto);
    Task<ExamResultDto?> GetExamResult(string submissionId);
    Task<List<ExamDto>?> GetExamsByUser(string userId);
}
