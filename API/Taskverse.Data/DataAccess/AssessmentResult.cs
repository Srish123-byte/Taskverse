using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Taskverse.Data.DataAccess;

public class AssessmentResult
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("assessmentId")]
    public string AssessmentId { get; set; } = default!;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("userId")]
    public string UserId { get; set; } = default!;

    /// <summary>
    /// Valid values: Pending, InProgress, Completed, Expired
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = default!;

    [BsonElement("score")]
    public int? Score { get; set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
