using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Taskverse.Data.DataAccess;

#nullable disable

namespace Taskverse.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TaskverseContext))]
    [Migration("20260514164000_BackfillNullCollegeNames")]
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
