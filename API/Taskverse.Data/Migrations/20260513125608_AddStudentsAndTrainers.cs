using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Taskverse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentsAndTrainers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          /*  migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    exam_id = table.Column<Guid>(type: "uuid", nullable: true),
                    challenge_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    assigned_to = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "colleges",
                columns: table => new
                {
                    college_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
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
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClassId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batches", x => x.batch_id);
                    table.ForeignKey(
                        name: "FK_batches_classes_ClassId1",
                        column: x => x.ClassId1,
                        principalTable: "classes",
                        principalColumn: "class_id");
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
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    college_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    class_id = table.Column<Guid>(type: "uuid", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
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
            */
            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollegeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    BatchId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.StudentId);
                    table.ForeignKey(
                        name: "FK_students_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "batches",
                        principalColumn: "batch_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_students_batches_BatchId1",
                        column: x => x.BatchId1,
                        principalTable: "batches",
                        principalColumn: "batch_id");
                    table.ForeignKey(
                        name: "FK_students_colleges_CollegeId",
                        column: x => x.CollegeId,
                        principalTable: "colleges",
                        principalColumn: "college_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_students_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trainers",
                columns: table => new
                {
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollegeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainers", x => x.TrainerId);
                    table.ForeignKey(
                        name: "FK_trainers_colleges_CollegeId",
                        column: x => x.CollegeId,
                        principalTable: "colleges",
                        principalColumn: "college_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trainers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trainer_batches",
                columns: table => new
                {
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_batches", x => new { x.TrainerId, x.BatchId });
                    table.ForeignKey(
                        name: "FK_trainer_batches_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "batches",
                        principalColumn: "batch_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trainer_batches_trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "trainers",
                        principalColumn: "TrainerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trainer_classes",
                columns: table => new
                {
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_classes", x => new { x.TrainerId, x.ClassId });
                    table.ForeignKey(
                        name: "FK_trainer_classes_classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "classes",
                        principalColumn: "class_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trainer_classes_trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "trainers",
                        principalColumn: "TrainerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_batches_class_id_name",
                table: "batches",
                columns: new[] { "class_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_batches_ClassId1",
                table: "batches",
                column: "ClassId1");

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
                name: "IX_colleges_name",
                table: "colleges",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_BatchId",
                table: "students",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_students_BatchId1",
                table: "students",
                column: "BatchId1");

            migrationBuilder.CreateIndex(
                name: "IX_students_CollegeId",
                table: "students",
                column: "CollegeId");

            migrationBuilder.CreateIndex(
                name: "IX_students_UserId",
                table: "students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trainer_batches_BatchId",
                table: "trainer_batches",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_trainer_classes_ClassId",
                table: "trainer_classes",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_trainers_CollegeId",
                table: "trainers",
                column: "CollegeId");

            migrationBuilder.CreateIndex(
                name: "IX_trainers_UserId",
                table: "trainers",
                column: "UserId",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "trainer_batches");

            migrationBuilder.DropTable(
                name: "trainer_classes");

            migrationBuilder.DropTable(
                name: "assessments");

            migrationBuilder.DropTable(
                name: "trainers");

            migrationBuilder.DropTable(
                name: "users");

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
