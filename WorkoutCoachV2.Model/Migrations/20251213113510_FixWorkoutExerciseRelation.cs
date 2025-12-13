using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutCoachV2.Model.Migrations
{
    /// <inheritdoc />
    public partial class FixWorkoutExerciseRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_Workouts_WorkoutId1",
                table: "WorkoutExercises");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutExercises_WorkoutId1",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "WorkoutId1",
                table: "WorkoutExercises");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Workouts",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Sessions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Exercises",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_OwnerId",
                table: "Workouts",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_OwnerId",
                table: "Sessions",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_OwnerId",
                table: "Exercises",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_AspNetUsers_OwnerId",
                table: "Exercises",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_AspNetUsers_OwnerId",
                table: "Sessions",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Workouts_AspNetUsers_OwnerId",
                table: "Workouts",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_AspNetUsers_OwnerId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_AspNetUsers_OwnerId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Workouts_AspNetUsers_OwnerId",
                table: "Workouts");

            migrationBuilder.DropIndex(
                name: "IX_Workouts_OwnerId",
                table: "Workouts");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_OwnerId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_OwnerId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Workouts");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Exercises");

            migrationBuilder.AddColumn<int>(
                name: "WorkoutId1",
                table: "WorkoutExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_WorkoutId1",
                table: "WorkoutExercises",
                column: "WorkoutId1");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_Workouts_WorkoutId1",
                table: "WorkoutExercises",
                column: "WorkoutId1",
                principalTable: "Workouts",
                principalColumn: "Id");
        }
    }
}
