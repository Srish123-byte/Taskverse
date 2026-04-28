using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Taskverse.Data.DataAccess;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("email")]
    public string Email { get; set; } = default!;

    [BsonElement("firstName")]
    public string FirstName { get; set; } = default!;

    [BsonElement("lastName")]
    public string LastName { get; set; } = default!;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = default!;

    /// <summary>
    /// Valid values: Student, Instructor, Admin, Proctor
    /// </summary>
    [BsonElement("role")]
    public string Role { get; set; } = default!;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("refreshToken")]
    public string? RefreshToken { get; set; }

    [BsonElement("refreshTokenExpiry")]
    public DateTime? RefreshTokenExpiry { get; set; }
}
