using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taskverse.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCollegeNameToUsersAndColleges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "name",
                table: "colleges",
                newName: "college_name");

            migrationBuilder.RenameIndex(
                name: "IX_colleges_name",
                table: "colleges",
                newName: "IX_colleges_college_name");

            migrationBuilder.AlterColumn<string>(
                name: "college_name",
                table: "colleges",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "college_name",
                table: "users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "college_name",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "college_name",
                table: "colleges",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "college_name",
                table: "colleges",
                newName: "name");

            migrationBuilder.RenameIndex(
                name: "IX_colleges_college_name",
                table: "colleges",
                newName: "IX_colleges_name");
        }
    }
}
