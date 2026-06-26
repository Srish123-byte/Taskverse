using System.ComponentModel.DataAnnotations.Schema;
using Taskverse.Data.Enums;
namespace Taskverse.Data.DataAccess;

[Table("users")]
public class User
{
        public Guid Id { get; set; }

        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }

        public Guid? CollegeId { get; set; }
        public string? CollegeName { get; set; }

        public string Role { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public Guid? BatchId { get; set; }
        public Guid? ClassId { get; set; }

        public string PasswordHash { get; set; } = null!;
        public string? TemporaryPassword { get; set; }
        public Guid? UploadedBy { get; set; }
        public bool IsBulkUploaded { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? TempPasswordIssuedAt { get; set; }
        public DateTime? PasswordChangedAt { get; set; }

        public UserStatus Status { get; set; }
}
