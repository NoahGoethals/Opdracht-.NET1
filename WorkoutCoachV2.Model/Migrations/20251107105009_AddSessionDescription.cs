using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutCoachV2.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Sessions");
        }
    }
}
