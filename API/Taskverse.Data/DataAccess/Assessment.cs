using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Taskverse.Data.DataAccess;

public class Assessment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("title")]
    public string Title { get; set; } = default!;

    [BsonElement("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Valid values: Exam, Coding, Mixed
    /// </summary>
    [BsonElement("type")]
    public string Type { get; set; } = default!;

    [BsonElement("examId")]
    public string? ExamId { get; set; }

    [BsonElement("challengeIds")]
    public List<string> ChallengeIds { get; set; } = [];

    /// <summary>
    /// List of UserIds this assessment is assigned to.
    /// </summary>
    [BsonElement("assignedTo")]
    public List<string> AssignedTo { get; set; } = [];

    [BsonElement("dueDate")]
    public DateTime? DueDate { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = default!;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
