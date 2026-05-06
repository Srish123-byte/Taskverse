using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;
using Taskverse.Data.DataAccess;

namespace Taskverse.Data;

public class TaskverseContext : DbContext
{
    public TaskverseContext(DbContextOptions<TaskverseContext> options)
        : base(options) { }

    // ── Registered tables (matching actual DB) ────────────────────────────────
    public DbSet<User>    Users    => Set<User>();
    public DbSet<College> Colleges => Set<College>();
    public DbSet<Class>   Classes  => Set<Class>();
    public DbSet<Batch>   Batches  => Set<Batch>();
    public DbSet<Role>    Roles    => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Register user_status PostgreSQL enum at the model level.
        // This prevents EF Core from falling back to integer serialization.
        // NpgsqlNullNameTranslator keeps the C# enum names unchanged (UPPERCASE),
        // matching the PostgreSQL enum labels exactly.
        modelBuilder.HasPostgresEnum<UserStatus>(
            schema: null,
            name: "user_status",
            nameTranslator: new NpgsqlNullNameTranslator());

        // ── Users ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            // HasPostgresEnum<UserStatus> above handles type resolution — do NOT set HasColumnType here,
            // as that overrides Npgsql's enum handler and causes it to fall back to integer serialization.
            entity.Property(u => u.UserStatus)
                  .HasColumnName("status");

            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("idx_users_email_unique");

            entity.HasIndex(u => u.Role)
                  .HasDatabaseName("idx_users_role");

            entity.HasIndex(u => u.UserStatus)
                  .HasDatabaseName("idx_users_status");
        });

        // ── Colleges ──────────────────────────────────────────────────────────
        modelBuilder.Entity<College>(entity =>
        {
            entity.HasIndex(c => c.Name)
                  .HasDatabaseName("idx_colleges_name");
        });

        // ── Classes ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasIndex(c => c.CollegeId)
                  .HasDatabaseName("idx_classes_college_id");
        });

        // ── Batches ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Batch>(entity =>
        {
            entity.HasIndex(b => b.CollegeId)
                  .HasDatabaseName("idx_batches_college_id");

            entity.HasIndex(b => b.ClassId)
                  .HasDatabaseName("idx_batches_class_id");
        });

        // ── Roles ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(r => r.RoleId)
                  .ValueGeneratedNever(); // smallint PK, not auto-generated
        });
    }
}
