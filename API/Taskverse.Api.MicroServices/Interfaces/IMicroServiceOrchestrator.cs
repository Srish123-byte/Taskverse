using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Interfaces;

public interface IMicroServiceOrchestrator : IMicroServiceCallingMethods
{
    // Users
    Task<ObjectResult> GetUser(string userId);
    Task<ObjectResult> GetPendingUsers();
    Task<ObjectResult> SearchUsers(UserSearchCriteriaModel criteria);
    Task<ObjectResult> CreateUser(CreateUserModel model);
    Task<ObjectResult> UpdateUser(string userId, UpdateUserModel model);
    Task<ObjectResult> DeleteUser(string userId);
    Task<ObjectResult> GetUserRoles(string userId);

    // Auth
    Task<ObjectResult> Login(LoginRequestModel model);
    Task<ObjectResult> RefreshToken(RefreshTokenRequestModel model);
    Task<ObjectResult> Logout(LogoutRequestModel model);
    Task<ObjectResult> ValidateToken(ValidateTokenRequestModel model);
    Task<ObjectResult> ChangeTemporaryPassword(ChangeTemporaryPasswordRequestModel model);

    // Exam Engine
    Task<ObjectResult> GetExam(string examId);
    Task<ObjectResult> CreateExam(CreateExamModel model);
    Task<ObjectResult> GetExamQuestions(string examId);
    Task<ObjectResult> SubmitExam(ExamSubmissionModel model);
    Task<ObjectResult> GetExamResult(string submissionId);
    Task<ObjectResult> GetExamsByUser(string userId);

    // Assessment
    Task<ObjectResult> CreateAssessment(CreateQuestionBankAssessmentModel model);
    Task<ObjectResult> GetAssessment(Guid assessmentId, Guid collegeId, string requesterRole, string requesterName);
    Task<ObjectResult> UpdateAssessment(UpdateQuestionBankAssessmentModel model);
    Task<ObjectResult> PublishAssessment(PublishQuestionBankAssessmentModel model);
    Task<ObjectResult> DeleteAssessment(DeleteAssessmentModel model);
    Task<ObjectResult> PublishAssessment(Guid assessmentId);
    Task<ObjectResult> CreateQuestions(List<CreateQuestionModel> models);
    Task<ObjectResult> GetQuestion(Guid questionId, Guid collegeId);
    Task<ObjectResult> UpdateQuestion(Guid questionId, CreateQuestionModel model);
    Task<ObjectResult> DeleteQuestions(DeleteQuestionsModel model);
    Task<ObjectResult> GetQuestionClassificationCatalog();
    Task<ObjectResult> GetTrainerAssignedClassesAndBatches(AssessmentBootstrapModel model);
    Task<ObjectResult> SearchQuestionBank(QuestionBankSearchModel model);
    Task<ObjectResult> SearchAssessments(AssessmentSearchModel model);
    Task<ObjectResult> GetAssessmentQuestionList(Guid assessmentId, AssessmentQuestionListSearchModel model);
    Task<ObjectResult> GetStudentAssessments(StudentAssessmentListSearchModel model, IReadOnlyCollection<string> assessmentStatuses);
    Task<ObjectResult> GetStudentAssessmentDetail(Guid assessmentId, Guid studentUserId);
    Task<ObjectResult> StartStudentAssessment(Guid assessmentId, Guid studentUserId);
    Task<ObjectResult> GetStudentAttemptRecovery(Guid attemptId, Guid studentUserId);
    Task<ObjectResult> SaveStudentAttemptAnswer(Guid attemptId, Guid questionId, Guid studentUserId, SaveStudentAttemptAnswerModel model);
    Task<ObjectResult> SubmitStudentAttempt(Guid attemptId, Guid studentUserId);

    // Reports
    Task<ObjectResult> GenerateReport(GenerateReportRequestModel model);
    Task<ObjectResult> GetReport(string reportId);
    Task<ObjectResult> GetUserPerformanceReport(string userId);
    Task<ObjectResult> GetAssessmentReport(string assessmentId);
    Task<ObjectResult> GetReportsByUser(string userId);
    Task<ObjectResult> GetStudentResults(Guid studentId);
    Task<ObjectResult> GetStudentAttemptResult(Guid attemptId);

    // Enterprise Reports
    Task<ObjectResult> GetCollegeWiseReport(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear);
    Task<byte[]> ExportCollegeWisePdf(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear);
    Task<byte[]> ExportCollegeWiseExcel(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear);

    Task<ObjectResult> GetBranchWiseReport(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo);
    Task<byte[]> ExportBranchWisePdf(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo);
    Task<byte[]> ExportBranchWiseExcel(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo);

    Task<ObjectResult> GetStudentPerformanceReport(Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId, Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo, string? performanceLevel);
    Task<byte[]> ExportStudentPerformancePdf(Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId, Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo, string? performanceLevel);
    Task<byte[]> ExportStudentPerformanceExcel(Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId, Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo, string? performanceLevel);

    Task<ObjectResult> GetCollegesFilter();
    Task<ObjectResult> GetBranchesFilter(Guid? collegeId);
    Task<ObjectResult> GetBatchesFilter(Guid? classId);
    Task<ObjectResult> GetTrainersFilter(Guid? collegeId);

    // Proctor
    Task<ObjectResult> StartProctorSession(Guid attemptId, Guid studentUserId, StartProctorSessionModel model);
    Task<ObjectResult> HeartbeatProctorSession(Guid sessionId, Guid studentUserId, SessionHeartbeatModel model);
    Task<ObjectResult> RecordProctorEvents(Guid sessionId, Guid studentUserId, ProctorEventBatchModel model);
    Task<ObjectResult> EndProctorSession(Guid sessionId, Guid studentUserId, EndProctorSessionModel model);
    Task<ObjectResult> GetProctorSession(Guid sessionId, Guid studentUserId);
    Task<ObjectResult> GetProctorSessionByAttempt(Guid attemptId, Guid studentUserId);
    Task<ObjectResult> GetAttemptProctorSession(Guid attemptId, Guid collegeId, string requesterRole, string requesterName);
    Task<ObjectResult> RecordProctorEvent(ProctorEventModel model);
    Task<ObjectResult> GetProctorSummary(string sessionId);

    // Coding Engine
    Task<ObjectResult> GetChallenge(string challengeId);
    Task<ObjectResult> ExecuteCode(CodeExecutionRequestModel model);
    Task<ObjectResult> GetSubmission(string submissionId);
    Task<ObjectResult> GetSubmissionsByUser(string userId);
    Task<ObjectResult> GetChallengesByAssessment(string assessmentId);

    // College
    Task<ObjectResult> GetColleges();
    Task<ObjectResult> SearchColleges(CollegeSearchModel model);
    Task<ObjectResult> GetPendingColleges();
    Task<ObjectResult> GetCollege(string collegeId);
    Task<ObjectResult> GetApprovedRegistrationColleges();
    Task<ObjectResult> GetRegistrationClasses(string collegeId);
    Task<ObjectResult> GetRegistrationBatches(string classId);
    Task<ObjectResult> GetCollegePendingUsers(string collegeId);
    Task<ObjectResult> GetCollegeAdminPendingUsers(string collegeAdminUserId);
    Task<ObjectResult> GetApprovedCollegeTrainers(string collegeId);
    Task<ObjectResult> GetApprovedUnassignedCollegeStudents(string collegeId);
    Task<ObjectResult> GetCollegeSubjects();
    Task<ObjectResult> CreateCollegeClass(string collegeId, CreateCollegeClassModel model);
    Task<ObjectResult> UpdateCollegeClass(string collegeId, string classId, UpdateCollegeClassModel model);
    Task<ObjectResult> CreateCollegeBatch(string collegeId, string classId, CreateCollegeBatchModel model);
    Task<ObjectResult> UpdateCollegeBatch(string collegeId, string classId, string batchId, UpdateCollegeBatchModel model);
    Task<ObjectResult> AssignCollegeBatchTrainers(string collegeId, string classId, string batchId, AssignBatchTrainersModel model);
    Task<ObjectResult> AssignCollegeBatchStudent(string collegeId, string classId, string batchId, AssignStudentToBatchModel model);
    Task<ObjectResult> DeleteCollegeClass(string collegeId, string classId);
    Task<ObjectResult> DeleteCollegeBatch(string collegeId, string classId, string batchId);
    Task<ObjectResult> ApproveCollegeUser(string collegeId, string userId, CollegeUserActionModel model);
    Task<ObjectResult> RejectCollegeUser(string collegeId, string userId, CollegeUserActionModel model);
    Task<ObjectResult> ApproveCollege(string collegeId, CollegeActionModel model);
    Task<ObjectResult> RejectCollege(string collegeId, CollegeActionModel model);
    Task<ObjectResult> DeactivateCollege(string collegeId, CollegeActionModel model);
    Task<ObjectResult> ReactivateCollege(string collegeId, CollegeActionModel model);
}
