using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutCoachV2.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "PerformedOn",
                table: "Sessions",
                newName: "Date");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Sessions",
                newName: "PerformedOn");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
