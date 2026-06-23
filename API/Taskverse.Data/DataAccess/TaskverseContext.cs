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
    public DbSet<LookupComparisonMode> LookupComparisonModes { get; set; }
    public DbSet<LookupCodeExecutionStatus> LookupCodeExecutionStatuses { get; set; }
    public DbSet<LookupCodeExecutionResultStatus> LookupCodeExecutionResultStatuses { get; set; }
    public DbSet<College> Colleges { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Batch> Batches { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<CodingLanguage> CodingLanguages { get; set; }
    public DbSet<CodingQuestion> CodingQuestions { get; set; }
    public DbSet<CodingSetting> CodingSettings { get; set; }
    public DbSet<StarterCode> StarterCodes { get; set; }
    public DbSet<StudentCode> StudentCodes { get; set; }
    public DbSet<CodeExecutionRequest> CodeExecutionRequests { get; set; }
    public DbSet<CodeExecutionResult> CodeExecutionResults { get; set; }
    public DbSet<TestCase> TestCases { get; set; }
    public DbSet<SubjectBatch> SubjectBatches { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<AssessmentCodingQuestion> AssessmentCodingQuestions { get; set; }
    public DbSet<AssessmentQuestion> AssessmentQuestions { get; set; }
    public DbSet<Attempt> Attempts { get; set; }
    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AuthSession> AuthSessions { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<ProctoringSession> ProctoringSessions { get; set; }
    public DbSet<ProctoringEvent> ProctoringEvents { get; set; }
    public DbSet<ProctoringViolationSummary> ProctoringViolationSummaries { get; set; }
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

        // Configure LookupComparisonMode entity
        modelBuilder.Entity<LookupComparisonMode>(entity =>
        {
            entity.ToTable("lookup_comparison_mode");
            entity.HasKey(lcm => lcm.ComparisonModeId);
            entity.Property(lcm => lcm.ComparisonModeId).HasColumnName("comparison_mode_id");
            entity.Property(lcm => lcm.ComparisonMode).HasColumnName("comparison_mode").IsRequired().HasMaxLength(50);

            entity.HasIndex(lcm => lcm.ComparisonMode).IsUnique();
        });

        // Configure LookupCodeExecutionStatus entity
        modelBuilder.Entity<LookupCodeExecutionStatus>(entity =>
        {
            entity.ToTable("lookup_code_execution_status");
            entity.HasKey(lces => lces.CodeExecutionStatusId);
            entity.Property(lces => lces.CodeExecutionStatusId).HasColumnName("code_execution_status_id");
            entity.Property(lces => lces.CodeExecutionStatus).HasColumnName("code_execution_status").IsRequired().HasMaxLength(30);

            entity.HasIndex(lces => lces.CodeExecutionStatus).IsUnique();
        });

        // Configure LookupCodeExecutionResultStatus entity
        modelBuilder.Entity<LookupCodeExecutionResultStatus>(entity =>
        {
            entity.ToTable("lookup_code_execution_result_status");
            entity.HasKey(lcers => lcers.CodeExecutionResultStatusId);
            entity.Property(lcers => lcers.CodeExecutionResultStatusId).HasColumnName("code_execution_result_status_id");
            entity.Property(lcers => lcers.CodeExecutionResultStatus).HasColumnName("code_execution_result_status").IsRequired().HasMaxLength(30);

            entity.HasIndex(lcers => lcers.CodeExecutionResultStatus).IsUnique();
        });

        // Configure CodeExecutionRequest entity
        modelBuilder.Entity<CodeExecutionRequest>(entity =>
        {
            entity.ToTable("code_execution_requests");
            entity.HasKey(cer => cer.CodeExecutionRequestId);
            entity.Property(cer => cer.CodeExecutionRequestId).HasColumnName("code_execution_request_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(cer => cer.StudentId).HasColumnName("student_id");
            entity.Property(cer => cer.AssessmentId).HasColumnName("assessment_id");
            entity.Property(cer => cer.CodingLanguageId).HasColumnName("coding_language_id");
            entity.Property(cer => cer.Code).HasColumnName("code").IsRequired();
            entity.Property(cer => cer.InputPayload).HasColumnName("input_payload");
            entity.Property(cer => cer.CodeExecutionStatusId).HasColumnName("code_execution_status").HasDefaultValue((short)1);
            entity.Property(cer => cer.RequestedAt).HasColumnName("requested_at").HasDefaultValueSql("now()");
            entity.Property(cer => cer.StartedAt).HasColumnName("started_at");
            entity.Property(cer => cer.CompletedAt).HasColumnName("completed_at");
            entity.Property(cer => cer.WorkerId).HasColumnName("worker_id").HasMaxLength(100);
            entity.Property(cer => cer.CorrelationId).HasColumnName("correlation_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(cer => cer.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(cer => cer.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(cer => cer.StudentId);
            entity.HasIndex(cer => cer.AssessmentId);
            entity.HasIndex(cer => cer.CodingLanguageId);
            entity.HasIndex(cer => cer.CodeExecutionStatusId);
            entity.HasIndex(cer => cer.CorrelationId);

            entity.HasOne(cer => cer.Student)
                  .WithMany(s => s.CodeExecutionRequests)
                  .HasForeignKey(cer => cer.StudentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_code_execution_requests_students");

            entity.HasOne(cer => cer.Assessment)
                  .WithMany(a => a.CodeExecutionRequests)
                  .HasForeignKey(cer => cer.AssessmentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_code_execution_requests_tasks");

            entity.HasOne(cer => cer.CodingLanguage)
                  .WithMany(cl => cl.CodeExecutionRequests)
                  .HasForeignKey(cer => cer.CodingLanguageId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_code_execution_requests_coding_languages");

            entity.HasOne(cer => cer.CodeExecutionStatus)
                  .WithMany(lces => lces.CodeExecutionRequests)
                  .HasForeignKey(cer => cer.CodeExecutionStatusId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure CodeExecutionResult entity
        modelBuilder.Entity<CodeExecutionResult>(entity =>
        {
            entity.ToTable("code_execution_results");
            entity.HasKey(cer => cer.CodeExecutionResultId);
            entity.Property(cer => cer.CodeExecutionResultId).HasColumnName("code_execution_result_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(cer => cer.CodeExecutionRequestId).HasColumnName("code_execution_request_id");
            entity.Property(cer => cer.CodeExecutionResultStatusId).HasColumnName("code_execution_result_status").HasDefaultValue((short)1);
            entity.Property(cer => cer.StandardOutput).HasColumnName("standard_output");
            entity.Property(cer => cer.StandardError).HasColumnName("standard_error");
            entity.Property(cer => cer.CompilerOutput).HasColumnName("compiler_output");
            entity.Property(cer => cer.ExitCode).HasColumnName("exit_code");
            entity.Property(cer => cer.ExecutionTimeMs).HasColumnName("execution_time_ms");
            entity.Property(cer => cer.MemoryUsedKb).HasColumnName("memory_used_kb");
            entity.Property(cer => cer.TotalTestCases).HasColumnName("total_test_cases");
            entity.Property(cer => cer.PassedTestCases).HasColumnName("passed_test_cases");
            entity.Property(cer => cer.CodingScore).HasColumnName("coding_score").HasColumnType("numeric(5,2)");
            entity.Property(cer => cer.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(cer => cer.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(cer => cer.CodeExecutionRequestId)
                  .IsUnique()
                  .HasDatabaseName("uq_code_execution_results_request");
            entity.HasIndex(cer => cer.CodeExecutionResultStatusId);

            entity.HasOne(cer => cer.CodeExecutionRequest)
                  .WithOne(cerq => cerq.CodeExecutionResult)
                  .HasForeignKey<CodeExecutionResult>(cer => cer.CodeExecutionRequestId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_code_execution_results_requests");

            entity.HasOne(cer => cer.CodeExecutionResultStatus)
                  .WithMany(lcers => lcers.CodeExecutionResults)
                  .HasForeignKey(cer => cer.CodeExecutionResultStatusId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure CodingQuestion entity
        modelBuilder.Entity<CodingQuestion>(entity =>
        {
            entity.ToTable("coding_questions");
            entity.HasKey(cq => cq.CodingQuestionId);
            entity.Property(cq => cq.CodingQuestionId).HasColumnName("coding_question_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(cq => cq.CollegeId).HasColumnName("college_id");
            entity.Property(cq => cq.QuestionTitle).HasColumnName("question_title").IsRequired().HasMaxLength(250);
            entity.Property(cq => cq.ProblemStatement).HasColumnName("problem_statement").IsRequired();
            entity.Property(cq => cq.DetailedDescription).HasColumnName("detailed_description");
            entity.Property(cq => cq.DifficultyLevel).HasColumnName("difficulty_level").HasDefaultValue(1);
            entity.Property(cq => cq.QuestionType).HasColumnName("question_type").IsRequired().HasMaxLength(50).HasDefaultValue("coding");
            entity.Property(cq => cq.TopicTag).HasColumnName("topic_tag").HasColumnType("text[]");
            entity.Property(cq => cq.InputFormat).HasColumnName("input_format");
            entity.Property(cq => cq.OutputFormat).HasColumnName("output_format");
            entity.Property(cq => cq.ConstraintsText).HasColumnName("constraints_text");
            entity.Property(cq => cq.Explanation).HasColumnName("explanation");
            entity.Property(cq => cq.Examples).HasColumnName("examples").HasColumnType("jsonb");
            entity.Property(cq => cq.DefaultLanguageCode).HasColumnName("default_language_code").HasMaxLength(50);
            entity.Property(cq => cq.DefaultTimeLimitMs).HasColumnName("default_time_limit_ms").HasDefaultValue(3000);
            entity.Property(cq => cq.DefaultMemoryLimitKb).HasColumnName("default_memory_limit_kb").HasDefaultValue(262144);
            entity.Property(cq => cq.DefaultMaxCodeSizeKb).HasColumnName("default_max_code_size_kb").HasDefaultValue(512);
            entity.Property(cq => cq.Marks).HasColumnName("marks").HasColumnType("numeric(8,2)").HasDefaultValue(100m);
            entity.Property(cq => cq.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(cq => cq.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
            entity.Property(cq => cq.ModifiedBy).HasColumnName("modified_by");
            entity.Property(cq => cq.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(cq => cq.ModifiedAt).HasColumnName("modified_at");
            entity.Property(cq => cq.Version).HasColumnName("version").HasDefaultValue(1);

            entity.HasIndex(cq => cq.CollegeId);

            entity.HasOne(cq => cq.College)
                  .WithMany(c => c.CodingQuestions)
                  .HasForeignKey(cq => cq.CollegeId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_coding_questions_colleges");
        });

        // Configure AssessmentCodingQuestion entity
        modelBuilder.Entity<AssessmentCodingQuestion>(entity =>
        {
            entity.ToTable("assessment_coding_questions");
            entity.HasKey(acq => acq.AssessmentCodingQuestionId);
            entity.Property(acq => acq.AssessmentCodingQuestionId).HasColumnName("assessment_coding_question_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(acq => acq.AssessmentId).HasColumnName("assessment_id");
            entity.Property(acq => acq.CodingQuestionId).HasColumnName("coding_question_id");
            entity.Property(acq => acq.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(acq => acq.ModifiedAt).HasColumnName("modified_at").HasDefaultValueSql("now()");

            entity.HasIndex(acq => acq.AssessmentId);
            entity.HasIndex(acq => acq.CodingQuestionId);

            entity.HasOne(acq => acq.Assessment)
                  .WithMany(a => a.AssessmentCodingQuestions)
                  .HasForeignKey(acq => acq.AssessmentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_assessment_coding_questions_assessments");

            entity.HasOne(acq => acq.CodingQuestion)
                  .WithMany(cq => cq.AssessmentCodingQuestions)
                  .HasForeignKey(acq => acq.CodingQuestionId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_assessment_coding_questions_coding_questions");
        });

        // Configure TestCase entity
        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.ToTable("test_cases", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_test_cases_input_format",
                    "input_format IN ('stdin', 'json', 'function_args')");
            });

            entity.HasKey(tc => tc.TestCaseId);
            entity.Property(tc => tc.TestCaseId).HasColumnName("test_case_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(tc => tc.CodingQuestionId).HasColumnName("coding_question_id");
            entity.Property(tc => tc.InputFormat).HasColumnName("input_format").IsRequired().HasMaxLength(30).HasDefaultValue("stdin");
            entity.Property(tc => tc.InputData).HasColumnName("input_data");
            entity.Property(tc => tc.ExpectedOutput).HasColumnName("expected_output");
            entity.Property(tc => tc.ComparisonMode).HasColumnName("comparison_mode").HasDefaultValue(2);
            entity.Property(tc => tc.NumericTolerance).HasColumnName("numeric_tolerance").HasColumnType("numeric(18,4)");
            entity.Property(tc => tc.IsHidden).HasColumnName("is_hidden").HasDefaultValue(false);
            entity.Property(tc => tc.IsSample).HasColumnName("is_sample").HasDefaultValue(false);
            entity.Property(tc => tc.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(tc => tc.TimeLimitMs).HasColumnName("time_limit_ms");
            entity.Property(tc => tc.MemoryLimitKb).HasColumnName("memory_limit_kb");
            entity.Property(tc => tc.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(tc => tc.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(tc => tc.CodingQuestionId);

            entity.HasOne(tc => tc.CodingQuestion)
                  .WithMany(cq => cq.TestCases)
                  .HasForeignKey(tc => tc.CodingQuestionId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_test_cases_coding_questions");
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
            entity.Property(u => u.TemporaryPassword).HasColumnName("temporary_password");
            entity.Property(u => u.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(u => u.IsBulkUploaded).HasColumnName("is_bulk_uploaded").HasDefaultValue(false);
            entity.Property(u => u.MustChangePassword).HasColumnName("must_change_password").HasDefaultValue(false);
            entity.Property(u => u.TempPasswordIssuedAt).HasColumnName("temp_password_issued_at");
            entity.Property(u => u.PasswordChangedAt).HasColumnName("password_changed_at");
            entity.Property(u => u.Status).HasColumnName("status");

            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.CollegeId);
            entity.HasIndex(u => u.Role);
            entity.HasIndex(u => u.BatchId);
            entity.HasIndex(u => u.ClassId);
            entity.HasIndex(u => u.UploadedBy);

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

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(u => u.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_users_uploaded_by");
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
            entity.HasOne(b => b.Class)
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

        // Configure CodingLanguage entity
        modelBuilder.Entity<CodingLanguage>(entity =>
        {
            entity.ToTable("coding_languages");
            entity.HasKey(cl => cl.CodingLanguageId);
            entity.Property(cl => cl.CodingLanguageId).HasColumnName("coding_language_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(cl => cl.LanguageCode).HasColumnName("language_code").IsRequired().HasMaxLength(50);
            entity.Property(cl => cl.DisplayName).HasColumnName("display_name").IsRequired().HasMaxLength(100);
            entity.Property(cl => cl.MonacoLanguageCode).HasColumnName("monaco_language_code").IsRequired().HasMaxLength(50);
            entity.Property(cl => cl.FileExtension).HasColumnName("file_extension").HasMaxLength(20);
            entity.Property(cl => cl.RuntimeName).HasColumnName("runtime_name").HasMaxLength(100);
            entity.Property(cl => cl.RuntimeVersion).HasColumnName("runtime_version").HasMaxLength(50);
            entity.Property(cl => cl.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(cl => cl.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(cl => cl.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(cl => cl.LanguageCode)
                  .IsUnique()
                  .HasDatabaseName("uq_coding_languages_language_code");
        });

        // Configure CodingSetting entity
        modelBuilder.Entity<CodingSetting>(entity =>
        {
            entity.ToTable("coding_settings");
            entity.HasKey(cs => cs.CodingSettingId);
            entity.Property(cs => cs.CodingSettingId).HasColumnName("coding_setting_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(cs => cs.DefaultLanguageId).HasColumnName("default_language_id");
            entity.Property(cs => cs.TimeLimitMs).HasColumnName("time_limit_ms").HasDefaultValue(3000);
            entity.Property(cs => cs.MemoryLimitKb).HasColumnName("memory_limit_kb").HasDefaultValue(262144);
            entity.Property(cs => cs.MaxCodeSizeKb).HasColumnName("max_code_size_kb").HasDefaultValue(512);
            entity.Property(cs => cs.IsCodeExecutionEnabled).HasColumnName("is_code_execution_enabled").HasDefaultValue(false);
            entity.Property(cs => cs.IsSubmissionEnabled).HasColumnName("is_submission_enabled").HasDefaultValue(true);
            entity.Property(cs => cs.AllowLanguageChange).HasColumnName("allow_language_change").HasDefaultValue(true);
            entity.Property(cs => cs.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(cs => cs.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(cs => cs.DefaultLanguageId);

            entity.HasOne(cs => cs.DefaultLanguage)
                  .WithMany(cl => cl.CodingSettings)
                  .HasForeignKey(cs => cs.DefaultLanguageId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_coding_settings_default_language");
        });

        // Configure StarterCode entity
        modelBuilder.Entity<StarterCode>(entity =>
        {
            entity.ToTable("starter_code");
            entity.HasKey(sc => sc.StarterCodeId);
            entity.Property(sc => sc.StarterCodeId).HasColumnName("starter_code_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(sc => sc.CodingLanguageId).HasColumnName("coding_language_id");
            entity.Property(sc => sc.StarterCodeContent).HasColumnName("starter_code").IsRequired();
            entity.Property(sc => sc.SolutionTemplate).HasColumnName("solution_template");
            entity.Property(sc => sc.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(sc => sc.CodingQuestionId).HasColumnName("coding_question_id");
            entity.Property(sc => sc.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(sc => sc.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(sc => sc.CodingLanguageId);
            entity.HasIndex(sc => sc.CodingQuestionId);

            entity.HasOne(sc => sc.CodingLanguage)
                  .WithMany(cl => cl.StarterCodes)
                  .HasForeignKey(sc => sc.CodingLanguageId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_task_starter_code_coding_languages");

            entity.HasOne(sc => sc.CodingQuestion)
                  .WithMany(cq => cq.StarterCodes)
                  .HasForeignKey(sc => sc.CodingQuestionId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_starter_code_coding_questions");
        });

        // Configure StudentCode entity
        modelBuilder.Entity<StudentCode>(entity =>
        {
            entity.ToTable("student_code");
            entity.HasKey(sc => sc.StudentCodeId);
            entity.Property(sc => sc.StudentCodeId).HasColumnName("student_code_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(sc => sc.StudentId).HasColumnName("student_id");
            entity.Property(sc => sc.AssessmentId).HasColumnName("assessment_id");
            entity.Property(sc => sc.CodingLanguageId).HasColumnName("coding_language_id");
            entity.Property(sc => sc.Code).HasColumnName("code").IsRequired();
            entity.Property(sc => sc.CodingQuestionId).HasColumnName("coding_question_id");
            entity.Property(sc => sc.LastSavedAt).HasColumnName("last_saved_at").HasDefaultValueSql("now()");
            entity.Property(sc => sc.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(sc => sc.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(sc => sc.StudentId);
            entity.HasIndex(sc => sc.AssessmentId);
            entity.HasIndex(sc => sc.CodingLanguageId);
            entity.HasIndex(sc => sc.CodingQuestionId);

            entity.HasOne(sc => sc.Student)
                  .WithMany(s => s.StudentCodes)
                  .HasForeignKey(sc => sc.StudentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_student_code");

            entity.HasOne(sc => sc.Assessment)
                  .WithMany(a => a.StudentCodes)
                  .HasForeignKey(sc => sc.AssessmentId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_student_assessment_code_assessments");

            entity.HasOne(sc => sc.CodingLanguage)
                  .WithMany(cl => cl.StudentCodes)
                  .HasForeignKey(sc => sc.CodingLanguageId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_student_assessment_code_coding_languages");

            entity.HasOne(sc => sc.CodingQuestion)
                  .WithMany(cq => cq.StudentCodes)
                  .HasForeignKey(sc => sc.CodingQuestionId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_student_code_coding_questions");
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
            entity.Property(a => a.PassingPercentage).HasColumnName("passing_percentage").HasDefaultValue(50);
            entity.Property(a => a.AllowQuestionReview).HasColumnName("allow_question_review");
            entity.Property(a => a.NegativeMarking).HasColumnName("negative_marking");
            entity.Property(a => a.IsTotalMarksAutoCalculated).HasColumnName("is_total_marks_auto_calculated");
            entity.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
            entity.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(a => a.ModifiedAt).HasColumnName("modified_at");
            entity.Property(a => a.IsDeleted).HasColumnName("is_deleted");
            entity.Property(a => a.SoftDeletedAt).HasColumnName("soft_deleted_at");
            entity.Property(a => a.SoftDeletedBy).HasColumnName("soft_deleted_by").HasMaxLength(200);
            entity.HasQueryFilter(a => a.AssessmentStatus != AssessmentStatus.Soft_Deleted);

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
            entity.HasIndex(a => new { a.AssessmentId, a.StudentId })
                .IsUnique()
                .HasDatabaseName("ux_attempts_assessment_student");
        });

        // Configure Question entity
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");
            entity.HasKey(q => q.QuestionId);
            entity.Property(q => q.QuestionId).HasColumnName("question_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(q => q.CollegeId).HasColumnName("college_id");
            entity.Property(q => q.Stream).HasColumnName("stream").HasMaxLength(100);
            entity.Property(q => q.Subject).HasColumnName("subject").HasMaxLength(100);
            entity.Property(q => q.Topic).HasColumnName("topic").HasMaxLength(200);
            entity.Property(q => q.TopicTag).HasColumnName("topic_tag").HasColumnType("text[]");
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
            entity.HasIndex(r => r.AttemptId).IsUnique();
            entity.HasIndex(r => r.StudentId);
        });

        // Configure ProctoringSession entity
        modelBuilder.Entity<ProctoringSession>(entity =>
        {
            entity.ToTable("proctoring_sessions");
            entity.HasKey(ps => ps.ProctoringSessionId);
            entity.Property(ps => ps.ProctoringSessionId).HasColumnName("proctoring_session_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(ps => ps.AttemptId).HasColumnName("attempt_id");
            entity.Property(ps => ps.AssessmentId).HasColumnName("assessment_id");
            entity.Property(ps => ps.StudentId).HasColumnName("student_id");
            entity.Property(ps => ps.ProctoringStatus).HasColumnName("proctoring_status");
            entity.Property(ps => ps.StartedAt).HasColumnName("started_at");
            entity.Property(ps => ps.EndedAt).HasColumnName("ended_at");
            entity.Property(ps => ps.LastHeartbeatAt).HasColumnName("last_heartbeat_at");
            entity.Property(ps => ps.LastKnownQuestionId).HasColumnName("last_known_question_id");
            entity.Property(ps => ps.LastKnownIsFullscreen).HasColumnName("last_known_is_fullscreen");
            entity.Property(ps => ps.LastKnownVisibilityState).HasColumnName("last_known_visibility_state").HasConversion<int>();
            entity.Property(ps => ps.LastKnownNetworkStatus).HasColumnName("last_known_network_status").HasConversion<int>();
            entity.Property(ps => ps.BrowserName).HasColumnName("browser_name").HasMaxLength(100);
            entity.Property(ps => ps.BrowserVersion).HasColumnName("browser_version").HasMaxLength(100);
            entity.Property(ps => ps.OperatingSystem).HasColumnName("operating_system").HasMaxLength(100);
            entity.Property(ps => ps.DeviceType).HasColumnName("device_type").HasMaxLength(50);
            entity.Property(ps => ps.UserAgent).HasColumnName("user_agent").HasMaxLength(200);
            entity.Property(ps => ps.IpAddress).HasColumnName("ip_address").HasMaxLength(100);
            entity.Property(ps => ps.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(ps => ps.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(ps => ps.AttemptId);
            entity.HasIndex(ps => ps.AssessmentId);
            entity.HasIndex(ps => ps.StudentId);

            entity.HasOne(ps => ps.Attempt)
                .WithMany()
                .HasForeignKey(ps => ps.AttemptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("proctoring_sessions_attempt_id_fkey");

            entity.HasOne(ps => ps.Assessment)
                .WithMany()
                .HasForeignKey(ps => ps.AssessmentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("proctoring_sessions_assessment_id_fkey");

            entity.HasOne(ps => ps.Student)
                .WithMany()
                .HasForeignKey(ps => ps.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("proctoring_sessions_student_id_fkey");
        });

        // Configure ProctoringEvent entity
        modelBuilder.Entity<ProctoringEvent>(entity =>
        {
            entity.ToTable("proctoring_events");
            entity.HasKey(pe => pe.ProctoringEventId);
            entity.Property(pe => pe.ProctoringEventId).HasColumnName("proctoring_event_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(pe => pe.ProctoringSessionId).HasColumnName("proctoring_session_id");
            entity.Property(pe => pe.AttemptId).HasColumnName("attempt_id");
            entity.Property(pe => pe.AssessmentId).HasColumnName("assessment_id");
            entity.Property(pe => pe.StudentId).HasColumnName("student_id");
            entity.Property(pe => pe.EventType).HasColumnName("event_type").HasConversion<int>();
            entity.Property(pe => pe.Severity).HasColumnName("severity").HasMaxLength(50);
            entity.Property(pe => pe.ClientTimestamp).HasColumnName("client_timestamp");
            entity.Property(pe => pe.ServerReceivedAt).HasColumnName("server_received_at").HasDefaultValueSql("now()");
            entity.Property(pe => pe.QuestionId).HasColumnName("question_id");
            entity.Property(pe => pe.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(pe => pe.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(pe => pe.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(pe => pe.EventType).HasDatabaseName("idx_proctoring_events_event_type");
            entity.HasIndex(pe => pe.AttemptId).HasDatabaseName("idx_proctoring_events_attempt_id");
            entity.HasIndex(pe => pe.ProctoringSessionId);
            entity.HasIndex(pe => pe.StudentId);
            entity.HasIndex(pe => pe.QuestionId);

            entity.HasOne(pe => pe.ProctoringSession)
                .WithMany()
                .HasForeignKey(pe => pe.ProctoringSessionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("proctoring_events_proctoring_session_id_fkey");

            entity.HasOne(pe => pe.Attempt)
                .WithMany()
                .HasForeignKey(pe => pe.AttemptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("proctoring_events_attempt_id_fkey");

            entity.HasOne(pe => pe.Assessment)
                .WithMany()
                .HasForeignKey(pe => pe.AssessmentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("proctoring_events_assessment_id_fkey");

            entity.HasOne(pe => pe.Student)
                .WithMany()
                .HasForeignKey(pe => pe.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("proctoring_events_student_id_fkey");

            entity.HasOne(pe => pe.Question)
                .WithMany()
                .HasForeignKey(pe => pe.QuestionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("proctoring_events_question_id_fkey");
        });

        // Configure ProctoringViolationSummary entity
        modelBuilder.Entity<ProctoringViolationSummary>(entity =>
        {
            entity.ToTable("proctoring_violation_summaries");
            entity.HasKey(pvs => pvs.ProctoringViolationSummaryId);
            entity.Property(pvs => pvs.ProctoringViolationSummaryId).HasColumnName("proctoring_violation_summary_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(pvs => pvs.AttemptId).HasColumnName("attempt_id");
            entity.Property(pvs => pvs.ProctoringSessionId).HasColumnName("proctoring_session_id");
            entity.Property(pvs => pvs.TabSwitchCount).HasColumnName("tab_switch_count").HasDefaultValue(0);
            entity.Property(pvs => pvs.FullScreenExitCount).HasColumnName("full_screen_exit_count").HasDefaultValue(0);
            entity.Property(pvs => pvs.CopyAttemptCount).HasColumnName("copy_attempt_count").HasDefaultValue(0);
            entity.Property(pvs => pvs.PasteAttemptCount).HasColumnName("paste_attempt_count").HasDefaultValue(0);
            entity.Property(pvs => pvs.CutAttemptCount).HasColumnName("cut_attempt_count").HasDefaultValue(0);
            entity.Property(pvs => pvs.ContextMenuAttemptCount).HasColumnName("context_menu_attempt_count").HasDefaultValue(0);
            entity.Property(pvs => pvs.BlockedShortcutCount).HasColumnName("blocked_shortcut_count").HasDefaultValue(0);
            entity.Property(pvs => pvs.PossibleDevtoolsCount).HasColumnName("possible_devtools_count").HasDefaultValue(0);
            entity.Property(pvs => pvs.NetworkDisconnectCount).HasColumnName("network_disconnect_count").HasDefaultValue(0);
            entity.Property(pvs => pvs.RiskScore).HasColumnName("risk_score").HasDefaultValue(0);
            entity.Property(pvs => pvs.RiskLevel).HasColumnName("risk_level").HasConversion<int>().HasDefaultValue(RiskLevel.Low);
            entity.Property(pvs => pvs.LastEventAt).HasColumnName("last_event_at");
            entity.Property(pvs => pvs.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(pvs => pvs.ModifiedAt).HasColumnName("modified_at");

            entity.HasIndex(pvs => pvs.AttemptId)
                .HasDatabaseName("idx_proctoring_violation_summaries_attempt_id");

            entity.HasOne(pvs => pvs.Attempt)
                .WithMany()
                .HasForeignKey(pvs => pvs.AttemptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("proctoring_violation_summaries_attempt_id_fkey");

            entity.HasOne(pvs => pvs.ProctoringSession)
                .WithMany()
                .HasForeignKey(pvs => pvs.ProctoringSessionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("proctoring_violation_summaries_proctoring_session_id_fkey");
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
