using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutCoachV2.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightToWorkoutExercise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "WeightKg",
                table: "WorkoutExercises",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "WorkoutExercises");
        }
    }
}
