using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taskverse.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillNullCollegeNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE colleges
                SET college_name = COALESCE(NULLIF(TRIM(college_name), ''), 'Unnamed College')
                WHERE college_name IS NULL OR BTRIM(college_name) = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
