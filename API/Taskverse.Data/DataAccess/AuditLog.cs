using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Taskverse.Data.DataAccess;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("userId")]
    public string UserId { get; set; } = default!;

    [BsonElement("action")]
    public string Action { get; set; } = default!;

    [BsonElement("entityType")]
    public string? EntityType { get; set; }

    [BsonElement("entityId")]
    public string? EntityId { get; set; }

    [BsonElement("details")]
    public string? Details { get; set; }

    [BsonElement("occurredAt")]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }
}
