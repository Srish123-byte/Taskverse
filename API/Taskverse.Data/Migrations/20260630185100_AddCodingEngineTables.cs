using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Taskverse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCodingEngineTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attempt_answers",
                columns: table => new
                {
                    attempt_answer_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selected_answer = table.Column<string>(type: "text", nullable: true),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    marks_awarded = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attempt_answers", x => x.attempt_answer_id);
                });

            migrationBuilder.CreateTable(
                name: "attempts",
                columns: table => new
                {
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_status = table.Column<int>(type: "integer", nullable: false),
                    total_questions = table.Column<int>(type: "integer", nullable: false),
                    attempted_questions = table.Column<int>(type: "integer", nullable: false),
                    correct_answers = table.Column<int>(type: "integer", nullable: false),
                    wrong_answers = table.Column<int>(type: "integer", nullable: false),
                    unanswered_questions = table.Column<int>(type: "integer", nullable: false),
                    total_score = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    time_taken_seconds = table.Column<int>(type: "integer", nullable: false),
                    is_passed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attempts", x => x.attempt_id);
                });

            migrationBuilder.CreateTable(
                name: "coding_engine_counters",
                columns: table => new
                {
                    counter_key = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    active_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_active = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coding_engine_counters", x => x.counter_key);
                });

            migrationBuilder.CreateTable(
                name: "coding_languages",
                columns: table => new
                {
                    coding_language_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    language_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    monaco_language_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_extension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    runtime_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    runtime_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    judge0_language_id = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coding_languages", x => x.coding_language_id);
                });

            migrationBuilder.CreateTable(
                name: "colleges",
                columns: table => new
                {
                    college_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    college_name = table.Column<string>(type: "text", nullable: true),
                    admin_name = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true, defaultValue: "Active"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_colleges", x => x.college_id);
                });

            migrationBuilder.CreateTable(
                name: "judge0_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    base_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    health_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Unknown"),
                    active_slots = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    reserved_final_slots = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_health_check_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cooldown_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_judge0_nodes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_code_execution_result_status",
                columns: table => new
                {
                    code_execution_result_status_id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code_execution_result_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_code_execution_result_status", x => x.code_execution_result_status_id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_code_execution_status",
                columns: table => new
                {
                    code_execution_status_id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code_execution_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_code_execution_status", x => x.code_execution_status_id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_comparison_mode",
                columns: table => new
                {
                    comparison_mode_id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comparison_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_comparison_mode", x => x.comparison_mode_id);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    question_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    college_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stream = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subject = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    topic_tag = table.Column<string[]>(type: "text[]", nullable: true),
                    question_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    question_text = table.Column<string>(type: "text", nullable: false),
                    options = table.Column<string>(type: "jsonb", nullable: true),
                    answer = table.Column<string>(type: "text", nullable: true),
                    explanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    negative_marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "(now() at time zone 'utc')"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "(now() at time zone 'utc')"),
                    difficulty_level = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.question_id);
                });

            migrationBuilder.CreateTable(
                name: "results",
                columns: table => new
                {
                    result_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_marks = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    obtained_marks = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    result_status = table.Column<int>(type: "integer", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_results", x => x.result_id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    role_id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.role_id);
                    table.UniqueConstraint("AK_roles_name", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    subject_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subjects", x => x.subject_id);
                });

            migrationBuilder.CreateTable(
                name: "coding_settings",
                columns: table => new
                {
                    coding_setting_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    default_language_id = table.Column<Guid>(type: "uuid", nullable: true),
                    time_limit_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 3000),
                    memory_limit_kb = table.Column<int>(type: "integer", nullable: false, defaultValue: 262144),
                    max_code_size_kb = table.Column<int>(type: "integer", nullable: false, defaultValue: 512),
                    is_code_execution_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_submission_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_language_change = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coding_settings", x => x.coding_setting_id);
                    table.ForeignKey(
                        name: "fk_coding_settings_default_language",
                        column: x => x.default_language_id,
                        principalTable: "coding_languages",
                        principalColumn: "coding_language_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    class_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    college_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    academic_year = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classes", x => x.class_id);
                    table.ForeignKey(
                        name: "fk_classes_college",
                        column: x => x.college_id,
                        principalTable: "colleges",
                        principalColumn: "college_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "coding_questions",
                columns: table => new
                {
                    coding_question_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    college_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    problem_statement = table.Column<string>(type: "text", nullable: false),
                    detailed_description = table.Column<string>(type: "text", nullable: true),
                    difficulty_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    question_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "coding"),
                    topic_tag = table.Column<string[]>(type: "text[]", nullable: true),
                    input_format = table.Column<string>(type: "text", nullable: true),
                    output_format = table.Column<string>(type: "text", nullable: true),
                    constraints_text = table.Column<string>(type: "text", nullable: true),
                    explanation = table.Column<string>(type: "text", nullable: true),
                    examples = table.Column<string>(type: "jsonb", nullable: true),
                    default_language_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    default_time_limit_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 3000),
                    default_memory_limit_kb = table.Column<int>(type: "integer", nullable: false, defaultValue: 262144),
                    default_max_code_size_kb = table.Column<int>(type: "integer", nullable: false, defaultValue: 512),
                    marks = table.Column<decimal>(type: "numeric(8,2)", nullable: false, defaultValue: 100m),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coding_questions", x => x.coding_question_id);
                    table.ForeignKey(
                        name: "fk_coding_questions_colleges",
                        column: x => x.college_id,
                        principalTable: "colleges",
                        principalColumn: "college_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "topics",
                columns: table => new
                {
                    topic_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topics", x => x.topic_id);
                    table.ForeignKey(
                        name: "fk_topics_subject",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "subject_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "batches",
                columns: table => new
                {
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    college_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batches", x => x.batch_id);
                    table.ForeignKey(
                        name: "fk_batches_class",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "class_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_batches_college",
                        column: x => x.college_id,
                        principalTable: "colleges",
                        principalColumn: "college_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "starter_code",
                columns: table => new
                {
                    starter_code_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coding_language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starter_code = table.Column<string>(type: "text", nullable: false),
                    solution_template = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    coding_question_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_starter_code", x => x.starter_code_id);
                    table.ForeignKey(
                        name: "fk_starter_code_coding_questions",
                        column: x => x.coding_question_id,
                        principalTable: "coding_questions",
                        principalColumn: "coding_question_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_task_starter_code_coding_languages",
                        column: x => x.coding_language_id,
                        principalTable: "coding_languages",
                        principalColumn: "coding_language_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "test_cases",
                columns: table => new
                {
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coding_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input_format = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "stdin"),
                    input_data = table.Column<string>(type: "text", nullable: true),
                    expected_output = table.Column<string>(type: "text", nullable: true),
                    comparison_mode = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    numeric_tolerance = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_sample = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    time_limit_ms = table.Column<int>(type: "integer", nullable: true),
                    memory_limit_kb = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_cases", x => x.test_case_id);
                    table.CheckConstraint("ck_test_cases_input_format", "input_format IN ('stdin', 'json', 'function_args')");
                    table.ForeignKey(
                        name: "fk_test_cases_coding_questions",
                        column: x => x.coding_question_id,
                        principalTable: "coding_questions",
                        principalColumn: "coding_question_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    college_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    topic_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assessment_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    assessment_type = table.Column<int>(type: "integer", nullable: false),
                    assessment_status = table.Column<int>(type: "integer", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    total_marks = table.Column<int>(type: "integer", nullable: false),
                    difficulty_level = table.Column<int>(type: "integer", nullable: false),
                    start_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    assigned_batch_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    allow_late_entry = table.Column<bool>(type: "boolean", nullable: false),
                    show_results_immediately = table.Column<bool>(type: "boolean", nullable: false),
                    passing_percentage = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    allow_question_review = table.Column<bool>(type: "boolean", nullable: false),
                    negative_marking = table.Column<bool>(type: "boolean", nullable: false),
                    is_total_marks_auto_calculated = table.Column<bool>(type: "boolean", nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true),
                    soft_deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    soft_deleted_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessments", x => x.assessment_id);
                    table.ForeignKey(
                        name: "fk_assessments_subject",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "subject_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_assessments_topic",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "topic_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "subject_batches",
                columns: table => new
                {
                    subject_batch_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subject_batches", x => x.subject_batch_id);
                    table.ForeignKey(
                        name: "fk_subject_batches_batch",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "batch_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_subject_batches_subject",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "subject_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    college_id = table.Column<Guid>(type: "uuid", nullable: true),
                    college_name = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    temporary_password = table.Column<string>(type: "text", nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_bulk_uploaded = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    temp_password_issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_batch",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "batch_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_class",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "class_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_college",
                        column: x => x.college_id,
                        principalTable: "colleges",
                        principalColumn: "college_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_users_role_name",
                        column: x => x.role,
                        principalTable: "roles",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "assessment_coding_questions",
                columns: table => new
                {
                    assessment_coding_question_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coding_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessment_coding_questions", x => x.assessment_coding_question_id);
                    table.ForeignKey(
                        name: "fk_assessment_coding_questions_assessments",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "assessment_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assessment_coding_questions_coding_questions",
                        column: x => x.coding_question_id,
                        principalTable: "coding_questions",
                        principalColumn: "coding_question_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assessment_questions",
                columns: table => new
                {
                    assessment_questions_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    marks = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessment_questions", x => x.assessment_questions_id);
                    table.ForeignKey(
                        name: "fk_assessment_questions_assessment",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "assessment_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_sessions",
                columns: table => new
                {
                    auth_session_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token_hash = table.Column<string>(type: "text", nullable: false),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_sessions", x => x.auth_session_id);
                    table.ForeignKey(
                        name: "fk_auth_sessions_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    college_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    enrollment_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    current_streak = table.Column<int>(type: "integer", nullable: true),
                    longest_streak = table.Column<int>(type: "integer", nullable: true),
                    last_assessment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_assessments_taken = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.student_id);
                    table.ForeignKey(
                        name: "FK_students_colleges_college_id",
                        column: x => x.college_id,
                        principalTable: "colleges",
                        principalColumn: "college_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_students_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_students_batch",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "batch_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_students_class",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "class_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "trainers",
                columns: table => new
                {
                    trainer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    college_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    upcoming_assessments_count = table.Column<int>(type: "integer", nullable: true),
                    live_assessments_count = table.Column<int>(type: "integer", nullable: true),
                    completed_assessments_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainers", x => x.trainer_id);
                    table.ForeignKey(
                        name: "FK_trainers_colleges_college_id",
                        column: x => x.college_id,
                        principalTable: "colleges",
                        principalColumn: "college_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trainers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "code_execution_requests",
                columns: table => new
                {
                    code_execution_request_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coding_language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    input_payload = table.Column<string>(type: "text", nullable: true),
                    execution_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code_execution_status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    worker_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    judge0_batch_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    lease_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    claimed_by_instance = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lease_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    judge0_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_code_execution_requests", x => x.code_execution_request_id);
                    table.ForeignKey(
                        name: "FK_code_execution_requests_lookup_code_execution_status_code_e~",
                        column: x => x.code_execution_status,
                        principalTable: "lookup_code_execution_status",
                        principalColumn: "code_execution_status_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_code_execution_requests_coding_languages",
                        column: x => x.coding_language_id,
                        principalTable: "coding_languages",
                        principalColumn: "coding_language_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_code_execution_requests_judge0_node",
                        column: x => x.judge0_node_id,
                        principalTable: "judge0_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_code_execution_requests_students",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_code_execution_requests_tasks",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "assessment_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proctoring_sessions",
                columns: table => new
                {
                    proctoring_session_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proctoring_status = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_known_question_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_known_visibility_state = table.Column<int>(type: "integer", nullable: true),
                    last_known_is_fullscreen = table.Column<bool>(type: "boolean", nullable: true),
                    last_known_network_status = table.Column<int>(type: "integer", nullable: true),
                    browser_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    browser_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    operating_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    device_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proctoring_sessions", x => x.proctoring_session_id);
                    table.ForeignKey(
                        name: "proctoring_sessions_assessment_id_fkey",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "assessment_id");
                    table.ForeignKey(
                        name: "proctoring_sessions_attempt_id_fkey",
                        column: x => x.attempt_id,
                        principalTable: "attempts",
                        principalColumn: "attempt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "proctoring_sessions_student_id_fkey",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_code",
                columns: table => new
                {
                    student_code_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coding_language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    coding_question_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_code", x => x.student_code_id);
                    table.ForeignKey(
                        name: "fk_student_assessment_code_assessments",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "assessment_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_assessment_code_coding_languages",
                        column: x => x.coding_language_id,
                        principalTable: "coding_languages",
                        principalColumn: "coding_language_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_code",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_code_coding_questions",
                        column: x => x.coding_question_id,
                        principalTable: "coding_questions",
                        principalColumn: "coding_question_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trainer_batches",
                columns: table => new
                {
                    trainer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_batches", x => new { x.trainer_id, x.batch_id });
                    table.ForeignKey(
                        name: "FK_trainer_batches_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "batch_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trainer_batches_trainers_trainer_id",
                        column: x => x.trainer_id,
                        principalTable: "trainers",
                        principalColumn: "trainer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trainer_classes",
                columns: table => new
                {
                    trainer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_classes", x => new { x.trainer_id, x.class_id });
                    table.ForeignKey(
                        name: "FK_trainer_classes_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "class_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trainer_classes_trainers_trainer_id",
                        column: x => x.trainer_id,
                        principalTable: "trainers",
                        principalColumn: "trainer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "code_execution_results",
                columns: table => new
                {
                    code_execution_result_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code_execution_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_execution_result_status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    standard_output = table.Column<string>(type: "text", nullable: true),
                    standard_error = table.Column<string>(type: "text", nullable: true),
                    compiler_output = table.Column<string>(type: "text", nullable: true),
                    exit_code = table.Column<int>(type: "integer", nullable: true),
                    execution_time_ms = table.Column<int>(type: "integer", nullable: true),
                    memory_used_kb = table.Column<int>(type: "integer", nullable: true),
                    total_test_cases = table.Column<int>(type: "integer", nullable: true),
                    passed_test_cases = table.Column<int>(type: "integer", nullable: true),
                    coding_score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_code_execution_results", x => x.code_execution_result_id);
                    table.ForeignKey(
                        name: "FK_code_execution_results_lookup_code_execution_result_status_~",
                        column: x => x.code_execution_result_status,
                        principalTable: "lookup_code_execution_result_status",
                        principalColumn: "code_execution_result_status_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_code_execution_results_requests",
                        column: x => x.code_execution_request_id,
                        principalTable: "code_execution_requests",
                        principalColumn: "code_execution_request_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "code_execution_submissions",
                columns: table => new
                {
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code_execution_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coding_language_id = table.Column<Guid>(type: "uuid", nullable: true),
                    judge0_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    judge0_status_id = table.Column<short>(type: "smallint", nullable: true),
                    judge0_status_description = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    judge0_submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    judge0_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    stdout = table.Column<string>(type: "text", nullable: true),
                    stderr = table.Column<string>(type: "text", nullable: true),
                    compile_output = table.Column<string>(type: "text", nullable: true),
                    exit_code = table.Column<int>(type: "integer", nullable: true),
                    time_seconds = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    memory_kilobytes = table.Column<int>(type: "integer", nullable: true),
                    passed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    actual_output = table.Column<string>(type: "text", nullable: true),
                    execution_time_ms = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_code_execution_submissions", x => x.submission_id);
                    table.ForeignKey(
                        name: "fk_submissions_language",
                        column: x => x.coding_language_id,
                        principalTable: "coding_languages",
                        principalColumn: "coding_language_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_submissions_request",
                        column: x => x.code_execution_request_id,
                        principalTable: "code_execution_requests",
                        principalColumn: "code_execution_request_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_submissions_test_case",
                        column: x => x.test_case_id,
                        principalTable: "test_cases",
                        principalColumn: "test_case_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proctoring_events",
                columns: table => new
                {
                    proctoring_event_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    proctoring_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<int>(type: "integer", nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    client_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    server_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    question_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proctoring_events", x => x.proctoring_event_id);
                    table.ForeignKey(
                        name: "proctoring_events_assessment_id_fkey",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "assessment_id");
                    table.ForeignKey(
                        name: "proctoring_events_attempt_id_fkey",
                        column: x => x.attempt_id,
                        principalTable: "attempts",
                        principalColumn: "attempt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "proctoring_events_proctoring_session_id_fkey",
                        column: x => x.proctoring_session_id,
                        principalTable: "proctoring_sessions",
                        principalColumn: "proctoring_session_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "proctoring_events_question_id_fkey",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "question_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "proctoring_events_student_id_fkey",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proctoring_violation_summaries",
                columns: table => new
                {
                    proctoring_violation_summary_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proctoring_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tab_switch_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    full_screen_exit_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    copy_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    paste_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    cut_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    context_menu_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    blocked_shortcut_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    possible_devtools_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    network_disconnect_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    risk_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    risk_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    last_event_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proctoring_violation_summaries", x => x.proctoring_violation_summary_id);
                    table.ForeignKey(
                        name: "proctoring_violation_summaries_attempt_id_fkey",
                        column: x => x.attempt_id,
                        principalTable: "attempts",
                        principalColumn: "attempt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "proctoring_violation_summaries_proctoring_session_id_fkey",
                        column: x => x.proctoring_session_id,
                        principalTable: "proctoring_sessions",
                        principalColumn: "proctoring_session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assessment_coding_questions_assessment_id",
                table: "assessment_coding_questions",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_assessment_coding_questions_coding_question_id",
                table: "assessment_coding_questions",
                column: "coding_question_id");

            migrationBuilder.CreateIndex(
                name: "IX_assessment_questions_assessment_id",
                table: "assessment_questions",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_assessment_questions_assessment_id_question_id",
                table: "assessment_questions",
                columns: new[] { "assessment_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assessment_questions_question_id",
                table: "assessment_questions",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_assessments_subject_id",
                table: "assessments",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_assessments_topic_id",
                table: "assessments",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "IX_attempt_answers_attempt_id",
                table: "attempt_answers",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_attempt_answers_attempt_id_question_id",
                table: "attempt_answers",
                columns: new[] { "attempt_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attempt_answers_question_id",
                table: "attempt_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_assessment_id",
                table: "attempts",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_student_id",
                table: "attempts",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ux_attempts_assessment_student",
                table: "attempts",
                columns: new[] { "assessment_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_refresh_token_hash",
                table: "auth_sessions",
                column: "refresh_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_user_id",
                table: "auth_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_user_id_revoked_at",
                table: "auth_sessions",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_batches_class_id_name",
                table: "batches",
                columns: new[] { "class_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_batches_college_id",
                table: "batches",
                column: "college_id");

            migrationBuilder.CreateIndex(
                name: "IX_classes_college_id_name_academic_year",
                table: "classes",
                columns: new[] { "college_id", "name", "academic_year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_requests_assessment_id",
                table: "code_execution_requests",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_requests_code_execution_status",
                table: "code_execution_requests",
                column: "code_execution_status");

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_requests_coding_language_id",
                table: "code_execution_requests",
                column: "coding_language_id");

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_requests_correlation_id",
                table: "code_execution_requests",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_requests_judge0_node_id",
                table: "code_execution_requests",
                column: "judge0_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_requests_student_id",
                table: "code_execution_requests",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_results_code_execution_result_status",
                table: "code_execution_results",
                column: "code_execution_result_status");

            migrationBuilder.CreateIndex(
                name: "uq_code_execution_results_request",
                table: "code_execution_results",
                column: "code_execution_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_submissions_code_execution_request_id",
                table: "code_execution_submissions",
                column: "code_execution_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_submissions_coding_language_id",
                table: "code_execution_submissions",
                column: "coding_language_id");

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_submissions_judge0_token",
                table: "code_execution_submissions",
                column: "judge0_token");

            migrationBuilder.CreateIndex(
                name: "IX_code_execution_submissions_test_case_id",
                table: "code_execution_submissions",
                column: "test_case_id");

            migrationBuilder.CreateIndex(
                name: "uq_coding_languages_language_code",
                table: "coding_languages",
                column: "language_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coding_questions_college_id",
                table: "coding_questions",
                column: "college_id");

            migrationBuilder.CreateIndex(
                name: "IX_coding_settings_default_language_id",
                table: "coding_settings",
                column: "default_language_id");

            migrationBuilder.CreateIndex(
                name: "IX_colleges_college_name",
                table: "colleges",
                column: "college_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_judge0_nodes_base_url",
                table: "judge0_nodes",
                column: "base_url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_code_execution_result_status_code_execution_result_s~",
                table: "lookup_code_execution_result_status",
                column: "code_execution_result_status",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_code_execution_status_code_execution_status",
                table: "lookup_code_execution_status",
                column: "code_execution_status",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_comparison_mode_comparison_mode",
                table: "lookup_comparison_mode",
                column: "comparison_mode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_proctoring_events_attempt_id",
                table: "proctoring_events",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "idx_proctoring_events_event_type",
                table: "proctoring_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_proctoring_events_assessment_id",
                table: "proctoring_events",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_proctoring_events_proctoring_session_id",
                table: "proctoring_events",
                column: "proctoring_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_proctoring_events_question_id",
                table: "proctoring_events",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_proctoring_events_student_id",
                table: "proctoring_events",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_proctoring_sessions_assessment_id",
                table: "proctoring_sessions",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_proctoring_sessions_attempt_id",
                table: "proctoring_sessions",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_proctoring_sessions_student_id",
                table: "proctoring_sessions",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "idx_proctoring_violation_summaries_attempt_id",
                table: "proctoring_violation_summaries",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_proctoring_violation_summaries_proctoring_session_id",
                table: "proctoring_violation_summaries",
                column: "proctoring_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_questions_college_id",
                table: "questions",
                column: "college_id");

            migrationBuilder.CreateIndex(
                name: "IX_questions_is_active",
                table: "questions",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_questions_question_type",
                table: "questions",
                column: "question_type");

            migrationBuilder.CreateIndex(
                name: "IX_results_assessment_id",
                table: "results",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_results_attempt_id",
                table: "results",
                column: "attempt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_results_student_id",
                table: "results",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_starter_code_coding_language_id",
                table: "starter_code",
                column: "coding_language_id");

            migrationBuilder.CreateIndex(
                name: "IX_starter_code_coding_question_id",
                table: "starter_code",
                column: "coding_question_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_code_assessment_id",
                table: "student_code",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_code_coding_language_id",
                table: "student_code",
                column: "coding_language_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_code_coding_question_id",
                table: "student_code",
                column: "coding_question_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_code_student_id",
                table: "student_code",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_batch_id",
                table: "students",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_class_id",
                table: "students",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_college_id",
                table: "students",
                column: "college_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_user_id",
                table: "students",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subject_batches_batch_id",
                table: "subject_batches",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_batches_subject_id",
                table: "subject_batches",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_batches_subject_id_batch_id",
                table: "subject_batches",
                columns: new[] { "subject_id", "batch_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subjects_subject_name",
                table: "subjects",
                column: "subject_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_cases_coding_question_id",
                table: "test_cases",
                column: "coding_question_id");

            migrationBuilder.CreateIndex(
                name: "IX_topics_subject_id",
                table: "topics",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_topics_subject_id_topic_name",
                table: "topics",
                columns: new[] { "subject_id", "topic_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trainer_batches_batch_id",
                table: "trainer_batches",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_trainer_classes_class_id",
                table: "trainer_classes",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_trainers_college_id",
                table: "trainers",
                column: "college_id");

            migrationBuilder.CreateIndex(
                name: "IX_trainers_user_id",
                table: "trainers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_batch_id",
                table: "users",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_class_id",
                table: "users",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_college_id",
                table: "users",
                column: "college_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_users_uploaded_by",
                table: "users",
                column: "uploaded_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessment_coding_questions");

            migrationBuilder.DropTable(
                name: "assessment_questions");

            migrationBuilder.DropTable(
                name: "attempt_answers");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "auth_sessions");

            migrationBuilder.DropTable(
                name: "code_execution_results");

            migrationBuilder.DropTable(
                name: "code_execution_submissions");

            migrationBuilder.DropTable(
                name: "coding_engine_counters");

            migrationBuilder.DropTable(
                name: "coding_settings");

            migrationBuilder.DropTable(
                name: "lookup_comparison_mode");

            migrationBuilder.DropTable(
                name: "proctoring_events");

            migrationBuilder.DropTable(
                name: "proctoring_violation_summaries");

            migrationBuilder.DropTable(
                name: "results");

            migrationBuilder.DropTable(
                name: "starter_code");

            migrationBuilder.DropTable(
                name: "student_code");

            migrationBuilder.DropTable(
                name: "subject_batches");

            migrationBuilder.DropTable(
                name: "trainer_batches");

            migrationBuilder.DropTable(
                name: "trainer_classes");

            migrationBuilder.DropTable(
                name: "lookup_code_execution_result_status");

            migrationBuilder.DropTable(
                name: "code_execution_requests");

            migrationBuilder.DropTable(
                name: "test_cases");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "proctoring_sessions");

            migrationBuilder.DropTable(
                name: "trainers");

            migrationBuilder.DropTable(
                name: "lookup_code_execution_status");

            migrationBuilder.DropTable(
                name: "coding_languages");

            migrationBuilder.DropTable(
                name: "judge0_nodes");

            migrationBuilder.DropTable(
                name: "coding_questions");

            migrationBuilder.DropTable(
                name: "assessments");

            migrationBuilder.DropTable(
                name: "attempts");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "topics");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "subjects");

            migrationBuilder.DropTable(
                name: "batches");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "classes");

            migrationBuilder.DropTable(
                name: "colleges");
        }
    }
}
