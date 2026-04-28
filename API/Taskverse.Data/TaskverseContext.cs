using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Taskverse.Data.DataAccess;

namespace Taskverse.Data;

public class TaskverseContext
{
    private readonly IMongoDatabase _database;

    public TaskverseContext(IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("TaskverseDb")
            ?? throw new InvalidOperationException("Connection string 'TaskverseDb' is not configured.");

        string databaseName = configuration["DatabaseName"] ?? "taskverse";

        MongoClient client = new(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("users");

    public IMongoCollection<Assessment> Assessments =>
        _database.GetCollection<Assessment>("assessments");

    public IMongoCollection<AssessmentResult> AssessmentResults =>
        _database.GetCollection<AssessmentResult>("assessment_results");

    public IMongoCollection<AuditLog> AuditLogs =>
        _database.GetCollection<AuditLog>("audit_logs");

    public async Task EnsureIndexesAsync()
    {
        // Users: unique index on Email
        IndexKeysDefinition<User> userEmailKey = Builders<User>.IndexKeys.Ascending(u => u.Email);
        CreateIndexModel<User> userEmailIndex = new(
            userEmailKey,
            new CreateIndexOptions { Unique = true, Name = "idx_users_email_unique" }
        );
        await Users.Indexes.CreateOneAsync(userEmailIndex);

        // AssessmentResults: compound index on AssessmentId + UserId
        IndexKeysDefinition<AssessmentResult> resultKey = Builders<AssessmentResult>.IndexKeys
            .Ascending(r => r.AssessmentId)
            .Ascending(r => r.UserId);
        CreateIndexModel<AssessmentResult> resultIndex = new(
            resultKey,
            new CreateIndexOptions { Name = "idx_results_assessmentId_userId" }
        );
        await AssessmentResults.Indexes.CreateOneAsync(resultIndex);

        // AuditLogs: compound index on UserId + OccurredAt
        IndexKeysDefinition<AuditLog> auditKey = Builders<AuditLog>.IndexKeys
            .Ascending(a => a.UserId)
            .Ascending(a => a.OccurredAt);
        CreateIndexModel<AuditLog> auditIndex = new(
            auditKey,
            new CreateIndexOptions { Name = "idx_auditlogs_userId_occurredAt" }
        );
        await AuditLogs.Indexes.CreateOneAsync(auditIndex);
    }
}
