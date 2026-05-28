using Microsoft.EntityFrameworkCore;
using Taskverse.Data.Enums;

namespace Taskverse.Data.DataAccess;

public class TaskverseContext : DbContext
{
    public TaskverseContext(DbContextOptions<TaskverseContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<College> Colleges { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Batch> Batches { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<SubjectBatch> SubjectBatches { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<AssessmentQuestion> AssessmentQuestions { get; set; }
    public DbSet<Attempt> Attempts { get; set; }
    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AuthSession> AuthSessions { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Result> Results { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Trainer> Trainers { get; set; }
    public DbSet<TrainerClass> TrainerClasses { get; set; }
    public DbSet<TrainerBatch> TrainerBatches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(r => r.RoleId);
            entity.Property(r => r.RoleId).HasColumnName("role_id");
            entity.Property(r => r.Name).HasColumnName("name").IsRequired();
            entity.Property(r => r.Description).HasColumnName("description");
            entity.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        // Configure College entity
        modelBuilder.Entity<College>(entity =>
        {
            entity.ToTable("colleges");
            entity.HasKey(c => c.CollegeId);
            entity.Property(c => c.CollegeId).HasColumnName("college_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(c => c.CollegeName).HasColumnName("college_name");
            entity.Property(c => c.AdminName).HasColumnName("admin_name");
            entity.Property(c => c.City).HasColumnName("city");
            entity.Property(c => c.State).HasColumnName("state");
            entity.Property(c => c.Status).HasColumnName("status").HasDefaultValue("Active");
            entity.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(c => c.ModifiedAt).HasColumnName("modified_at");
            entity.HasIndex(c => c.CollegeName).IsUnique();
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(u => u.FullName).HasColumnName("full_name").IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").IsRequired();
            entity.Property(u => u.Phone).HasColumnName("phone");
            entity.Property(u => u.CollegeId).HasColumnName("college_id");
            entity.Property(u => u.CollegeName).HasColumnName("college_name");
            entity.Property(u => u.Role).HasColumnName("role").IsRequired();
            entity.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(u => u.ModifiedAt).HasColumnName("modified_at").HasDefaultValueSql("now()");
            entity.Property(u => u.BatchId).HasColumnName("batch_id");
            entity.Property(u => u.ClassId).HasColumnName("class_id");
            entity.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(u => u.Status).HasColumnName("status");

            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.CollegeId);
            entity.HasIndex(u => u.Role);
            entity.HasIndex(u => u.BatchId);
            entity.HasIndex(u => u.ClassId);

            // Foreign key: users.college_id -> colleges.college_id
            entity.HasOne<College>()
                .WithMany()
                .HasForeignKey(u => u.CollegeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_users_college");

            // Foreign key: users.role -> roles.name
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(u => u.Role)
                .HasPrincipalKey(r => r.Name)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_users_role_name");

            // Foreign key: users.batch_id -> batches.batch_id
            entity.HasOne<Batch>()
                .WithMany()
                .HasForeignKey(u => u.BatchId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_users_batch");

            // Foreign key: users.class_id -> classes.class_id
            entity.HasOne<Class>()
                .WithMany()
                .HasForeignKey(u => u.ClassId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_users_class");
        });

        // Configure Class entity
        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("classes");
            entity.HasKey(c => c.ClassId);
            entity.Property(c => c.ClassId).HasColumnName("class_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(c => c.CollegeId).HasColumnName("college_id");
            entity.Property(c => c.Name).HasColumnName("name").IsRequired();
            entity.Property(c => c.Description).HasColumnName("description");
            entity.Property(c => c.AcademicYear).HasColumnName("academic_year");
            entity.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(c => c.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(c => new { c.CollegeId, c.Name, c.AcademicYear }).IsUnique();

            // Foreign key: classes.college_id -> colleges.college_id
            entity.HasOne<College>()
                .WithMany()
                .HasForeignKey(c => c.CollegeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_classes_college");

        });

        // Configure Batch entity
        modelBuilder.Entity<Batch>(entity =>
        {
            entity.ToTable("batches");
            entity.HasKey(b => b.BatchId);
            entity.Property(b => b.BatchId).HasColumnName("batch_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(b => b.ClassId).HasColumnName("class_id");
            entity.Property(b => b.CollegeId).HasColumnName("college_id");
            entity.Property(b => b.Name).HasColumnName("name").IsRequired();
            entity.Property(b => b.Capacity).HasColumnName("capacity");
            entity.Property(b => b.Description).HasColumnName("description");
            entity.Property(b => b.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(b => b.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(b => new { b.ClassId, b.Name }).IsUnique();

            // Foreign key: batches.class_id -> classes.class_id
            entity.HasOne<Class>()
                .WithMany(c => c.Batches)
                .HasForeignKey(b => b.ClassId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_batches_class");

            // Foreign key: batches.college_id -> colleges.college_id
            entity.HasOne<College>()
                .WithMany()
                .HasForeignKey(b => b.CollegeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_batches_college");
        });

        // Configure Subject entity
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("subjects");
            entity.HasKey(s => s.SubjectId);
            entity.Property(s => s.SubjectId).HasColumnName("subject_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(s => s.SubjectName).HasColumnName("subject_name").IsRequired().HasMaxLength(150);
            entity.Property(s => s.Description).HasColumnName("description");
            entity.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(s => s.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(s => s.SubjectName).IsUnique();
        });

        // Configure SubjectBatch entity
        modelBuilder.Entity<SubjectBatch>(entity =>
        {
            entity.ToTable("subject_batches");
            entity.HasKey(sb => sb.SubjectBatchId);
            entity.Property(sb => sb.SubjectBatchId).HasColumnName("subject_batch_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(sb => sb.SubjectId).HasColumnName("subject_id");
            entity.Property(sb => sb.BatchId).HasColumnName("batch_id");
            entity.Property(sb => sb.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasIndex(sb => sb.SubjectId);
            entity.HasIndex(sb => sb.BatchId);
            entity.HasIndex(sb => new { sb.SubjectId, sb.BatchId }).IsUnique();

            entity.HasOne(sb => sb.Subject)
                .WithMany(s => s.SubjectBatches)
                .HasForeignKey(sb => sb.SubjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_subject_batches_subject");

            entity.HasOne(sb => sb.Batch)
                .WithMany(b => b.SubjectBatches)
                .HasForeignKey(sb => sb.BatchId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_subject_batches_batch");
        });

        // Configure Topic entity
        modelBuilder.Entity<Topic>(entity =>
        {
            entity.ToTable("topics");
            entity.HasKey(t => t.TopicId);
            entity.Property(t => t.TopicId).HasColumnName("topic_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(t => t.SubjectId).HasColumnName("subject_id");
            entity.Property(t => t.TopicName).HasColumnName("topic_name").IsRequired().HasMaxLength(150);
            entity.Property(t => t.Description).HasColumnName("description");
            entity.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(t => t.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(t => t.SubjectId);
            entity.HasIndex(t => new { t.SubjectId, t.TopicName }).IsUnique();

            entity.HasOne(t => t.Subject)
                .WithMany(s => s.Topics)
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_topics_subject");
        });

        // Configure Assessment entity
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("assessments");
            entity.HasKey(a => a.AssessmentId);
            entity.Property(a => a.AssessmentId).HasColumnName("assessment_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(a => a.CollegeId).HasColumnName("college_id");
            entity.Property(a => a.SubjectId).HasColumnName("subject_id");
            entity.Property(a => a.TopicId).HasColumnName("topic_id");
            entity.Property(a => a.AssessmentName).HasColumnName("assessment_name").IsRequired().HasMaxLength(120);
            entity.Property(a => a.AssessmentType).HasColumnName("assessment_type").HasConversion<int>();
            entity.Property(a => a.AssessmentStatus).HasColumnName("assessment_status").HasConversion<int>();
            entity.Property(a => a.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(a => a.TotalMarks).HasColumnName("total_marks");
            entity.Property(a => a.DifficultyLevel).HasColumnName("difficulty_level");
            entity.Property(a => a.StartDateTime).HasColumnName("start_datetime");
            entity.Property(a => a.EndDateTime).HasColumnName("end_datetime");
            entity.Property(a => a.Instructions).HasColumnName("instructions").HasMaxLength(2000);
            entity.Property(a => a.AssignedBatchIds).HasColumnName("assigned_batch_ids").HasColumnType("uuid[]");
            entity.Property(a => a.AllowLateEntry).HasColumnName("allow_late_entry");
            entity.Property(a => a.ShowResultsImmediately).HasColumnName("show_results_immediately");
            entity.Property(a => a.AllowQuestionReview).HasColumnName("allow_question_review");
            entity.Property(a => a.NegativeMarking).HasColumnName("negative_marking");
            entity.Property(a => a.MarksPerQuestion).HasColumnName("marks_per_question").HasColumnType("numeric(6,2)");
            entity.Property(a => a.IsTotalMarksAutoCalculated).HasColumnName("is_total_marks_auto_calculated");
            entity.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
            entity.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(a => a.ModifiedAt).HasColumnName("modified_at");
            entity.Property(a => a.SoftDeletedAt).HasColumnName("soft_deleted_at");
            entity.Property(a => a.SoftDeletedBy).HasColumnName("soft_deleted_by").HasMaxLength(200);
            entity.HasQueryFilter(a => a.AssessmentStatus != AssessmentStatus.Soft_Delete);

            entity.HasIndex(a => a.SubjectId);
            entity.HasIndex(a => a.TopicId);

            entity.HasOne(a => a.Subject)
                .WithMany(s => s.Assessments)
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_assessments_subject");

            entity.HasOne(a => a.Topic)
                .WithMany(t => t.Assessments)
                .HasForeignKey(a => a.TopicId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_assessments_topic");
        });

        // Configure AssessmentQuestion entity
        modelBuilder.Entity<AssessmentQuestion>(entity =>
        {
            entity.ToTable("assessment_questions");
            entity.HasKey(aq => aq.AssessmentQuestionId);
            entity.Property(aq => aq.AssessmentQuestionId).HasColumnName("assessment_questions_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(aq => aq.AssessmentId).HasColumnName("assessment_id");
            entity.Property(aq => aq.QuestionId).HasColumnName("question_id");
            entity.Property(aq => aq.DisplayOrder).HasColumnName("display_order");
            entity.Property(aq => aq.Marks).HasColumnName("marks").HasColumnType("numeric(5,2)");
            entity.Property(aq => aq.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(aq => aq.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(aq => aq.AssessmentId);
            entity.HasIndex(aq => aq.QuestionId);
            entity.HasIndex(aq => new { aq.AssessmentId, aq.QuestionId }).IsUnique();

            entity.HasOne(aq => aq.Assessment)
                .WithMany(a => a.AssessmentQuestions)
                .HasForeignKey(aq => aq.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_assessment_questions_assessment");
        });

        // Configure AttemptAnswer entity
        modelBuilder.Entity<AttemptAnswer>(entity =>
        {
            entity.ToTable("attempt_answers");
            entity.HasKey(aa => aa.AttemptAnswerId);
            entity.Property(aa => aa.AttemptAnswerId).HasColumnName("attempt_answer_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(aa => aa.AttemptId).HasColumnName("attempt_id");
            entity.Property(aa => aa.QuestionId).HasColumnName("question_id");
            entity.Property(aa => aa.SelectedAnswer).HasColumnName("selected_answer");
            entity.Property(aa => aa.IsCorrect).HasColumnName("is_correct");
            entity.Property(aa => aa.MarksAwarded).HasColumnName("marks_awarded").HasColumnType("numeric(5,2)");
            entity.Property(aa => aa.AnsweredAt).HasColumnName("answered_at");

            entity.HasIndex(aa => aa.AttemptId);
            entity.HasIndex(aa => aa.QuestionId);
            entity.HasIndex(aa => new { aa.AttemptId, aa.QuestionId }).IsUnique();
        });

        // Configure Attempt entity
        modelBuilder.Entity<Attempt>(entity =>
        {
            entity.ToTable("attempts");
            entity.HasKey(a => a.AttemptId);
            entity.Property(a => a.AttemptId).HasColumnName("attempt_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(a => a.AssessmentId).HasColumnName("assessment_id");
            entity.Property(a => a.StudentId).HasColumnName("student_id");
            entity.Property(a => a.QuestionId).HasColumnName("question_id");
            entity.Property(a => a.StartedAt).HasColumnName("started_at");
            entity.Property(a => a.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(a => a.LastActivityAt).HasColumnName("last_activity_at");
            entity.Property(a => a.ExpiresAt).HasColumnName("expires_at");
            entity.Property(a => a.AttemptStatus).HasColumnName("attempt_status").HasConversion<int>();
            entity.Property(a => a.TotalQuestions).HasColumnName("total_questions");
            entity.Property(a => a.AttemptedQuestions).HasColumnName("attempted_questions");
            entity.Property(a => a.CorrectAnswers).HasColumnName("correct_answers");
            entity.Property(a => a.WrongAnswers).HasColumnName("wrong_answers");
            entity.Property(a => a.UnansweredQuestions).HasColumnName("unanswered_questions");
            entity.Property(a => a.TotalScore).HasColumnName("total_score").HasColumnType("numeric(6,2)");
            entity.Property(a => a.Percentage).HasColumnName("percentage").HasColumnType("numeric(5,2)");
            entity.Property(a => a.TimeTakenSeconds).HasColumnName("time_taken_seconds");
            entity.Property(a => a.IsPassed).HasColumnName("is_passed");
            entity.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasIndex(a => a.AssessmentId);
            entity.HasIndex(a => a.StudentId);
            entity.HasIndex(a => a.QuestionId);
            entity.HasIndex(a => new { a.AssessmentId, a.StudentId })
                .IsUnique()
                .HasDatabaseName("ux_attempts_assessment_student");

            entity.HasOne(a => a.Question)
                .WithMany(q => q.Attempts)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_attempts_question");
        });

        // Configure Question entity
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");
            entity.HasKey(q => q.QuestionId);
            entity.Property(q => q.QuestionId).HasColumnName("question_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(q => q.CollegeId).HasColumnName("college_id");
            entity.Property(q => q.AssessmentId).HasColumnName("assessment_id");
            entity.Property(q => q.Stream).HasColumnName("stream").HasMaxLength(100);
            entity.Property(q => q.Subject).HasColumnName("subject").HasMaxLength(100);
            entity.Property(q => q.Topic).HasColumnName("topic").HasMaxLength(200);
            entity.Property(q => q.TopicTag).HasColumnName("topic_tag").HasMaxLength(200);
            entity.Property(q => q.QuestionType).HasColumnName("question_type").IsRequired().HasMaxLength(50);
            entity.Property(q => q.QuestionText).HasColumnName("question_text").IsRequired();
            entity.Property(q => q.Options).HasColumnName("options").HasColumnType("jsonb");
            entity.Property(q => q.Answer).HasColumnName("answer");
            entity.Property(q => q.Explanation).HasColumnName("explanation").HasMaxLength(1000);
            entity.Property(q => q.Marks).HasColumnName("marks").HasColumnType("numeric(5,2)");
            entity.Property(q => q.NegativeMarks).HasColumnName("negative_marks").HasColumnType("numeric(5,2)");
            entity.Property(q => q.IsActive).HasColumnName("is_active").HasDefaultValue(true).ValueGeneratedOnAdd();
            entity.Property(q => q.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
            entity.Property(q => q.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("(now() at time zone 'utc')").ValueGeneratedOnAdd();
            entity.Property(q => q.ModifiedAt).HasColumnName("modified_at").HasDefaultValueSql("(now() at time zone 'utc')").ValueGeneratedOnAdd();
            entity.Property(q => q.DifficultyLevel).HasColumnName("difficulty_level");
            entity.Property(q => q.Version).HasColumnName("version").HasDefaultValue(1).ValueGeneratedOnAdd();

            entity.HasIndex(q => q.CollegeId);
            entity.HasIndex(q => q.AssessmentId);
            entity.HasIndex(q => q.QuestionType);
            entity.HasIndex(q => q.IsActive);
        });

        // Configure Result entity
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("results");
            entity.HasKey(r => r.ResultId);
            entity.Property(r => r.ResultId).HasColumnName("result_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(r => r.AssessmentId).HasColumnName("assessment_id");
            entity.Property(r => r.AttemptId).HasColumnName("attempt_id");
            entity.Property(r => r.StudentId).HasColumnName("student_id");
            entity.Property(r => r.TotalMarks).HasColumnName("total_marks").HasColumnType("numeric(6,2)");
            entity.Property(r => r.ObtainedMarks).HasColumnName("obtained_marks").HasColumnType("numeric(6,2)");
            entity.Property(r => r.Percentage).HasColumnName("percentage").HasColumnType("numeric(5,2)");
            entity.Property(r => r.Rank).HasColumnName("rank");
            entity.Property(r => r.ResultStatus).HasColumnName("result_status").HasConversion<int>();
            entity.Property(r => r.GeneratedAt).HasColumnName("generated_at").HasDefaultValueSql("now()");

            entity.HasIndex(r => r.AssessmentId);
            entity.HasIndex(r => r.AttemptId);
            entity.HasIndex(r => r.StudentId);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(al => al.Id);
            entity.Property(al => al.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(al => al.UserId).HasColumnName("user_id");
            entity.Property(al => al.Action).HasColumnName("action").IsRequired().HasMaxLength(100);
            entity.Property(al => al.EntityType).HasColumnName("entity_type").HasMaxLength(100);
            entity.Property(al => al.EntityId).HasColumnName("entity_id");
            entity.Property(al => al.Details).HasColumnName("details");
            entity.Property(al => al.OccurredAt).HasColumnName("occurred_at").HasDefaultValueSql("now()");
            entity.Property(al => al.IpAddress).HasColumnName("ip_address").HasMaxLength(45);

            // Foreign key
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_audit_logs_user");
        });

        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.ToTable("auth_sessions");
            entity.HasKey(session => session.AuthSessionId);
            entity.Property(session => session.AuthSessionId)
                .HasColumnName("auth_session_id")
                .HasDefaultValueSql("gen_random_uuid()");
            entity.Property(session => session.UserId)
                .HasColumnName("user_id");
            entity.Property(session => session.RefreshTokenHash)
                .HasColumnName("refresh_token_hash")
                .IsRequired();
            entity.Property(session => session.LastActivityAt)
                .HasColumnName("last_activity_at");
            entity.Property(session => session.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now()");
            entity.Property(session => session.ModifiedAt)
                .HasColumnName("modified_at")
                .HasDefaultValueSql("now()");
            entity.Property(session => session.RevokedAt)
                .HasColumnName("revoked_at");

            entity.HasIndex(session => session.UserId);
            entity.HasIndex(session => session.RefreshTokenHash).IsUnique();
            entity.HasIndex(session => new { session.UserId, session.RevokedAt });

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_auth_sessions_user");
        });

        // Configure Student entity
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("students");
            entity.HasKey(s => s.StudentId);
            entity.Property(s => s.StudentId).HasColumnName("student_id");
            entity.Property(s => s.UserId).HasColumnName("user_id");
            entity.Property(s => s.CollegeId).HasColumnName("college_id");
            entity.Property(s => s.ClassId).HasColumnName("class_id");
            entity.Property(s => s.BatchId).HasColumnName("batch_id");
            entity.Property(s => s.EnrollmentNumber).HasColumnName("enrollment_number").HasMaxLength(50);
            entity.Property(s => s.FullName).HasColumnName("full_name");
            entity.Property(s => s.Email).HasColumnName("email");
            entity.Property(s => s.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(s => s.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(s => s.CurrentStreak).HasColumnName("current_streak");
            entity.Property(s => s.LongestStreak).HasColumnName("longest_streak");
            entity.Property(s => s.LastAssessmentDate).HasColumnName("last_assessment_date");
            entity.Property(s => s.TotalAssessmentsTaken).HasColumnName("total_assessments_taken");
            entity.Property(s => s.CreatedAt).HasColumnName("created_at");
            entity.Property(s => s.ModifiedAt).HasColumnName("modified_at");
            entity.Property(s => s.ApprovedBy).HasColumnName("approved_by");

            // class_id is NOT unique – many students belong to the same class
            entity.HasIndex(s => s.ClassId);
            entity.HasIndex(s => s.BatchId);

            entity.HasOne(s => s.User)
                  .WithOne()
                  .HasForeignKey<Student>(s => s.UserId);

            entity.HasOne(s => s.College)
                  .WithMany()
                  .HasForeignKey(s => s.CollegeId);

            entity.HasOne(s => s.Class)
                  .WithMany(c => c.Students)
                  .HasForeignKey(s => s.ClassId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_students_class");

            // batch_id is nullable – a student may not have a batch when approved
            entity.HasOne(s => s.Batch)
                  .WithMany(b => b.Students)
                  .HasForeignKey(s => s.BatchId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_students_batch");

            // Map enum to int column
            entity.Property(s => s.Status)
                  .HasColumnName("status")
                  .HasConversion<int>();
        });

        // Configure Trainer entity
        modelBuilder.Entity<Trainer>(entity =>
        {
            entity.ToTable("trainers");
            entity.HasKey(t => t.TrainerId);
            entity.Property(t => t.TrainerId).HasColumnName("trainer_id");
            entity.Property(t => t.UserId).HasColumnName("user_id");
            entity.Property(t => t.CollegeId).HasColumnName("college_id");
            entity.Property(t => t.FullName).HasColumnName("full_name");
            entity.Property(t => t.Email).HasColumnName("email");
            entity.Property(t => t.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(t => t.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(t => t.UpcomingAssessmentsCount).HasColumnName("upcoming_assessments_count");
            entity.Property(t => t.LiveAssessmentsCount).HasColumnName("live_assessments_count");
            entity.Property(t => t.CompletedAssessmentsCount).HasColumnName("completed_assessments_count");
            entity.Property(t => t.ApprovedBy).HasColumnName("approved_by");
            entity.Property(t => t.CreatedAt).HasColumnName("created_at");
            entity.Property(t => t.ModifiedAt).HasColumnName("modified_at");

            entity.HasOne(t => t.User)
                  .WithOne()
                  .HasForeignKey<Trainer>(t => t.UserId);

            entity.HasOne(t => t.College)
                  .WithMany()
                  .HasForeignKey(t => t.CollegeId);

            // Map enum to int column
            entity.Property(t => t.Status)
                  .HasColumnName("status")
                  .HasConversion<int>();
        });

        // Junction tables
        modelBuilder.Entity<TrainerClass>(entity =>
        {
            entity.ToTable("trainer_classes");
            entity.HasKey(tc => new { tc.TrainerId, tc.ClassId });
            entity.Property(tc => tc.TrainerId).HasColumnName("trainer_id");
            entity.Property(tc => tc.ClassId).HasColumnName("class_id");

            entity.HasOne(tc => tc.Trainer)
                  .WithMany(t => t.TrainerClasses)
                  .HasForeignKey(tc => tc.TrainerId);

            entity.HasOne(tc => tc.Class)
                  .WithMany(c => c.TrainerClasses)
                  .HasForeignKey(tc => tc.ClassId);
        });

        modelBuilder.Entity<TrainerBatch>(entity =>
        {
            entity.ToTable("trainer_batches");
            entity.HasKey(tb => new { tb.TrainerId, tb.BatchId });
            entity.Property(tb => tb.TrainerId).HasColumnName("trainer_id");
            entity.Property(tb => tb.BatchId).HasColumnName("batch_id");

            entity.HasOne(tb => tb.Trainer)
                  .WithMany(t => t.TrainerBatches)
                  .HasForeignKey(tb => tb.TrainerId);

            entity.HasOne(tb => tb.Batch)
                  .WithMany(b => b.TrainerBatches)
                  .HasForeignKey(tb => tb.BatchId);
        });
    }
}
