using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkoutCoachV2.Model.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel_20251107 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "Sets",
                table: "WorkoutExercises");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "WorkoutExercises",
                newName: "WorkoutId1");

            migrationBuilder.AddColumn<int>(
                name: "ExerciseId1",
                table: "WorkoutExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExerciseId1",
                table: "SessionSets",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_ExerciseId1",
                table: "WorkoutExercises",
                column: "ExerciseId1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_WorkoutId1",
                table: "WorkoutExercises",
                column: "WorkoutId1");

            migrationBuilder.CreateIndex(
                name: "IX_SessionSets_ExerciseId1",
                table: "SessionSets",
                column: "ExerciseId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionSets_Exercises_ExerciseId1",
                table: "SessionSets",
                column: "ExerciseId1",
                principalTable: "Exercises",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_Exercises_ExerciseId1",
                table: "WorkoutExercises",
                column: "ExerciseId1",
                principalTable: "Exercises",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_Workouts_WorkoutId1",
                table: "WorkoutExercises",
                column: "WorkoutId1",
                principalTable: "Workouts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionSets_Exercises_ExerciseId1",
                table: "SessionSets");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_Exercises_ExerciseId1",
                table: "WorkoutExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_Workouts_WorkoutId1",
                table: "WorkoutExercises");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutExercises_ExerciseId1",
                table: "WorkoutExercises");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutExercises_WorkoutId1",
                table: "WorkoutExercises");

            migrationBuilder.DropIndex(
                name: "IX_SessionSets_ExerciseId1",
                table: "SessionSets");

            migrationBuilder.DropColumn(
                name: "ExerciseId1",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "ExerciseId1",
                table: "SessionSets");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "WorkoutId1",
                table: "WorkoutExercises",
                newName: "Order");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WorkoutExercises",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sets",
                table: "WorkoutExercises",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
