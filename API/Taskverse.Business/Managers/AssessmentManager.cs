using MongoDB.Driver;
using Taskverse.Business.Interface;
using Taskverse.Data;
using Taskverse.Data.DataAccess;

namespace Taskverse.Business.Managers;

public class AssessmentManager : IAssessmentManager
{
    private readonly TaskverseContext _context;

    public AssessmentManager(TaskverseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Assessment?> GetById(string assessmentId)
    {
        FilterDefinition<Assessment> filter = Builders<Assessment>.Filter.Eq(a => a.Id, assessmentId);
        return await _context.Assessments.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<Assessment>> GetByUserId(string userId)
    {
        FilterDefinition<Assessment> filter = Builders<Assessment>.Filter.AnyEq(a => a.AssignedTo, userId);
        return await _context.Assessments.Find(filter).ToListAsync();
    }

    public async Task<Assessment> Create(Assessment assessment)
    {
        await _context.Assessments.InsertOneAsync(assessment);
        return assessment;
    }

    public async Task<AssessmentResult?> GetResult(string assessmentId, string userId)
    {
        FilterDefinition<AssessmentResult> filter = Builders<AssessmentResult>.Filter.And(
            Builders<AssessmentResult>.Filter.Eq(r => r.AssessmentId, assessmentId),
            Builders<AssessmentResult>.Filter.Eq(r => r.UserId, userId));

        return await _context.AssessmentResults.Find(filter).FirstOrDefaultAsync();
    }

    public async Task UpsertResult(AssessmentResult result)
    {
        FilterDefinition<AssessmentResult> filter = Builders<AssessmentResult>.Filter.And(
            Builders<AssessmentResult>.Filter.Eq(r => r.AssessmentId, result.AssessmentId),
            Builders<AssessmentResult>.Filter.Eq(r => r.UserId, result.UserId));

        ReplaceOptions options = new() { IsUpsert = true };
        await _context.AssessmentResults.ReplaceOneAsync(filter, result, options);
    }
}
