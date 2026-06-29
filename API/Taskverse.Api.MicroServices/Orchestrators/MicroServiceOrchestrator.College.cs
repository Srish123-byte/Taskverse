using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.MicroServices.Orchestrators;

public partial class MicroServiceOrchestrator
{
    public async Task<ObjectResult> GetColleges()
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges";
        return await Get<List<CollegeModel>>(url);
    }

    public async Task<ObjectResult> SearchColleges(CollegeSearchModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/search";
        return await Post<List<CollegeSearchResultModel>>(url, model);
    }

    public async Task<ObjectResult> GetPendingColleges()
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/pending";
        return await Get<List<CollegeModel>>(url);
    }

    public async Task<ObjectResult> GetCollege(string collegeId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}";
        return await Get<CollegeModel>(url);
    }

    public async Task<ObjectResult> GetApprovedRegistrationColleges()
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/registration/colleges";
        return await Get<List<RegistrationCollegeModel>>(url);
    }

    public async Task<ObjectResult> GetRegistrationClasses(string collegeId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/registration/colleges/{collegeId}/classes";
        return await Get<List<RegistrationClassModel>>(url);
    }

    public async Task<ObjectResult> GetRegistrationBatches(string classId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/registration/classes/{classId}/batches";
        return await Get<List<RegistrationBatchModel>>(url);
    }

    public async Task<ObjectResult> GetCollegePendingUsers(string collegeId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/users/pending";
        return await Get<List<PendingUserModel>>(url);
    }

    public async Task<ObjectResult> GetCollegeAdminPendingUsers(string collegeAdminUserId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/college-admins/{collegeAdminUserId}/users/pending";
        return await Get<List<PendingUserModel>>(url);
    }

    public async Task<ObjectResult> GetApprovedCollegeTrainers(string collegeId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/trainers/approved";
        return await Get<List<ApprovedTrainerModel>>(url);
    }

    public async Task<ObjectResult> GetApprovedUnassignedCollegeStudents(string collegeId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/students/approved-unassigned";
        return await Get<List<ApprovedStudentModel>>(url);
    }

    public async Task<ObjectResult> GetCollegeSubjects()
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/subjects";
        return await Get<List<SubjectOptionModel>>(url);
    }

    public async Task<ObjectResult> CreateCollegeClass(string collegeId, CreateCollegeClassModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/classes";
        return await Post<CollegeClassSummaryModel>(url, model);
    }

    public async Task<ObjectResult> UpdateCollegeClass(string collegeId, string classId, UpdateCollegeClassModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/classes/{classId}";
        return await Put<CollegeClassSummaryModel>(url, model);
    }

    public async Task<ObjectResult> CreateCollegeBatch(string collegeId, string classId, CreateCollegeBatchModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/classes/{classId}/batches";
        return await Post<CollegeBatchSummaryModel>(url, model);
    }

    public async Task<ObjectResult> UpdateCollegeBatch(string collegeId, string classId, string batchId, UpdateCollegeBatchModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/classes/{classId}/batches/{batchId}";
        return await Put<CollegeBatchSummaryModel>(url, model);
    }

    public async Task<ObjectResult> AssignCollegeBatchTrainers(string collegeId, string classId, string batchId, AssignBatchTrainersModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/classes/{classId}/batches/{batchId}/trainers";
        return await Post<CollegeBatchSummaryModel>(url, model);
    }

    public async Task<ObjectResult> AssignCollegeBatchStudent(string collegeId, string classId, string batchId, AssignStudentToBatchModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/classes/{classId}/batches/{batchId}/students";
        return await Post<CollegeBatchSummaryModel>(url, model);
    }

    public async Task<ObjectResult> GetAttendanceBatches(Guid collegeId, Guid requesterUserId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/attendance/batches?collegeId={collegeId}&requesterUserId={requesterUserId}";
        return await Get<List<AttendanceBatchGroupModel>>(url);
    }

    public async Task<ObjectResult> GetAttendanceRoster(AttendanceRosterRequestModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/attendance/roster";
        return await Post<AttendanceRosterModel>(url, model);
    }

    public async Task<ObjectResult> SubmitAttendance(SubmitAttendanceRequestModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/attendance/sessions";
        return await Post<AttendanceRosterModel>(url, model);
    }

    public async Task<ObjectResult> GetAttendanceHistory(Guid collegeId, Guid requesterUserId, Guid batchId, DateTime fromDate, DateTime toDate)
    {
        var encodedFromDate = Uri.EscapeDataString(fromDate.ToString("O"));
        var encodedToDate = Uri.EscapeDataString(toDate.ToString("O"));
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/attendance/history?collegeId={collegeId}&requesterUserId={requesterUserId}&batchId={batchId}&fromDate={encodedFromDate}&toDate={encodedToDate}";
        return await Get<AttendanceHistoryModel>(url);
    }

    public async Task<ObjectResult> ExportAttendance(Guid collegeId, Guid requesterUserId, Guid batchId, DateTime fromDate, DateTime toDate)
    {
        var encodedFromDate = Uri.EscapeDataString(fromDate.ToString("O"));
        var encodedToDate = Uri.EscapeDataString(toDate.ToString("O"));
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/attendance/export?collegeId={collegeId}&requesterUserId={requesterUserId}&batchId={batchId}&fromDate={encodedFromDate}&toDate={encodedToDate}";
        return await Get<AttendanceExportModel>(url);
    }

    public async Task<ObjectResult> DeleteCollegeClass(string collegeId, string classId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/classes/{classId}";
        return await Delete(url);
    }

    public async Task<ObjectResult> DeleteCollegeBatch(string collegeId, string classId, string batchId)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/classes/{classId}/batches/{batchId}";
        return await Delete(url);
    }

    public async Task<ObjectResult> ApproveCollegeUser(string collegeId, string userId, CollegeUserActionModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/users/{userId}/approve";
        return await Post<object>(url, model);
    }

    public async Task<ObjectResult> RejectCollegeUser(string collegeId, string userId, CollegeUserActionModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/users/{userId}/reject";
        return await Post<object>(url, model);
    }

    public async Task<ObjectResult> ApproveCollege(string collegeId, CollegeActionModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/approve";
        return await Post<CollegeModel>(url, model);
    }

    public async Task<ObjectResult> RejectCollege(string collegeId, CollegeActionModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/reject";
        return await Post<CollegeModel>(url, model);
    }

    public async Task<ObjectResult> DeactivateCollege(string collegeId, CollegeActionModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/deactivate";
        return await Post<CollegeModel>(url, model);
    }

    public async Task<ObjectResult> ReactivateCollege(string collegeId, CollegeActionModel model)
    {
        var url = $"{GetMicroServiceUrl(MicroService.College)}api/colleges/{collegeId}/reactivate";
        return await Post<CollegeModel>(url, model);
    }
}
