using Microsoft.EntityFrameworkCore;
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

        // ── Users ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            // Map the user_status PostgreSQL enum column (registered via NpgsqlDataSourceBuilder.MapEnum)
            entity.Property(u => u.Status)
                  .HasColumnName("status")
                  .HasColumnType("user_status");

            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("idx_users_email_unique");

            entity.HasIndex(u => u.Role)
                  .HasDatabaseName("idx_users_role");

            entity.HasIndex(u => u.Status)
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
