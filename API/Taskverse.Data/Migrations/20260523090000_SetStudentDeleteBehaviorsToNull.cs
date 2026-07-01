using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Taskverse.Data.DataAccess;

#nullable disable

namespace Taskverse.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TaskverseContext))]
    [Migration("20260523090000_SetStudentDeleteBehaviorsToNull")]
    public partial class SetStudentDeleteBehaviorsToNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE students
                ALTER COLUMN class_id DROP NOT NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE students
                ALTER COLUMN batch_id DROP NOT NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE students
                DROP CONSTRAINT IF EXISTS fk_students_class;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE students
                DROP CONSTRAINT IF EXISTS fk_students_batch;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE students
                ADD CONSTRAINT fk_students_class
                FOREIGN KEY (class_id)
                REFERENCES classes (class_id)
                ON DELETE SET NULL;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE students
                ADD CONSTRAINT fk_students_batch
                FOREIGN KEY (batch_id)
                REFERENCES batches (batch_id)
                ON DELETE SET NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE students
                DROP CONSTRAINT IF EXISTS fk_students_class;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE students
                DROP CONSTRAINT IF EXISTS fk_students_batch;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE students
                ADD CONSTRAINT fk_students_class
                FOREIGN KEY (class_id)
                REFERENCES classes (class_id)
                ON DELETE RESTRICT;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE students
                ADD CONSTRAINT fk_students_batch
                FOREIGN KEY (batch_id)
                REFERENCES batches (batch_id)
                ON DELETE RESTRICT;
                """);
        }
    }
}
