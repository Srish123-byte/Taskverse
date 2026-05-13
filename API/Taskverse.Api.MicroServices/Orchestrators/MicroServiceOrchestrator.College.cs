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
