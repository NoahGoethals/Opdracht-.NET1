using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Model.Data.Seed
{
    public static class DemoDataSeeder
    {
      
        public static async Task SeedDemoDataForUserAsync(AppDbContext ctx, string ownerId)
        {
            var userHasAnyData =
                await ctx.Exercises.AnyAsync(e => e.OwnerId == ownerId && !e.IsDeleted) ||
                await ctx.Workouts.AnyAsync(w => w.OwnerId == ownerId && !w.IsDeleted) ||
                await ctx.Sessions.AnyAsync(s => s.OwnerId == ownerId && !s.IsDeleted);

            if (userHasAnyData) return;

            var now = DateTime.UtcNow;

            var exercises = new List<Exercise>
            {
                new Exercise { Name = "Bench Press", Category = "Chest", Notes = "Barbell", OwnerId = ownerId, CreatedAt = now, UpdatedAt = now, IsDeleted = false },
                new Exercise { Name = "Incline Dumbbell Press", Category = "Chest", Notes = "Controlled tempo", OwnerId = ownerId, CreatedAt = now, UpdatedAt = now, IsDeleted = false },

                new Exercise { Name = "Back Squat", Category = "Legs", Notes = "Depth focus", OwnerId = ownerId, CreatedAt = now, UpdatedAt = now, IsDeleted = false },
                new Exercise { Name = "Romanian Deadlift", Category = "Legs", Notes = "Hamstrings", OwnerId = ownerId, CreatedAt = now, UpdatedAt = now, IsDeleted = false },

                new Exercise { Name = "Pull-Ups", Category = "Back", Notes = "Bodyweight", OwnerId = ownerId, CreatedAt = now, UpdatedAt = now, IsDeleted = false },
                new Exercise { Name = "Barbell Row", Category = "Back", Notes = "Strict form", OwnerId = ownerId, CreatedAt = now, UpdatedAt = now, IsDeleted = false },

                new Exercise { Name = "Overhead Press", Category = "Shoulders", Notes = "Standing", OwnerId = ownerId, CreatedAt = now, UpdatedAt = now, IsDeleted = false },
                new Exercise { Name = "Lateral Raises", Category = "Shoulders", Notes = "Light weight", OwnerId = ownerId, CreatedAt = now, UpdatedAt = now, IsDeleted = false },

                new Exercise { Name = "Bicep Curls", Category = "Arms", Notes = "Full range", OwnerId = ownerId, CreatedAt = now, UpdatedAt = now, IsDeleted = false },
                new Exercise { Name = "Tricep Pushdowns", Category = "Arms", Notes = "Cable", OwnerId = ownerId, CreatedAt = now, UpdatedAt = now, IsDeleted = false }
            };

            ctx.Exercises.AddRange(exercises);
            await ctx.SaveChangesAsync();

            int ExId(string name) => exercises.First(x => x.Name == name).Id;

            var today = DateTime.Today;

            var workouts = new List<Workout>
            {
                new Workout
                {
                    Title = "Push A",
                    ScheduledOn = today.AddDays(1),
                    OwnerId = ownerId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDeleted = false
                },
                new Workout
                {
                    Title = "Pull A",
                    ScheduledOn = today.AddDays(3),
                    OwnerId = ownerId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDeleted = false
                },
                new Workout
                {
                    Title = "Legs A",
                    ScheduledOn = today.AddDays(5),
                    OwnerId = ownerId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDeleted = false
                }
            };

            ctx.Workouts.AddRange(workouts);
            await ctx.SaveChangesAsync();

            int WkId(string title) => workouts.First(w => w.Title == title).Id;

            var workoutExercises = new List<WorkoutExercise>
            {
                new WorkoutExercise { WorkoutId = WkId("Push A"), ExerciseId = ExId("Bench Press"), Reps = 5, WeightKg = 60 },
                new WorkoutExercise { WorkoutId = WkId("Push A"), ExerciseId = ExId("Incline Dumbbell Press"), Reps = 10, WeightKg = 20 },
                new WorkoutExercise { WorkoutId = WkId("Push A"), ExerciseId = ExId("Overhead Press"), Reps = 8, WeightKg = 35 },
                new WorkoutExercise { WorkoutId = WkId("Push A"), ExerciseId = ExId("Lateral Raises"), Reps = 15, WeightKg = 8 },
                new WorkoutExercise { WorkoutId = WkId("Push A"), ExerciseId = ExId("Tricep Pushdowns"), Reps = 12, WeightKg = 25 },

                new WorkoutExercise { WorkoutId = WkId("Pull A"), ExerciseId = ExId("Pull-Ups"), Reps = 6, WeightKg = 0 },
                new WorkoutExercise { WorkoutId = WkId("Pull A"), ExerciseId = ExId("Barbell Row"), Reps = 8, WeightKg = 50 },
                new WorkoutExercise { WorkoutId = WkId("Pull A"), ExerciseId = ExId("Bicep Curls"), Reps = 12, WeightKg = 12 },

                new WorkoutExercise { WorkoutId = WkId("Legs A"), ExerciseId = ExId("Back Squat"), Reps = 5, WeightKg = 80 },
                new WorkoutExercise { WorkoutId = WkId("Legs A"), ExerciseId = ExId("Romanian Deadlift"), Reps = 8, WeightKg = 70 }
            };

            ctx.WorkoutExercises.AddRange(workoutExercises);
            await ctx.SaveChangesAsync();

            var sessions = new List<Session>
            {
                new Session
                {
                    Title = "Session 1 - Push",
                    Date = today.AddDays(-2),
                    Description = "Demo sessie: push focus",
                    OwnerId = ownerId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDeleted = false
                },
                new Session
                {
                    Title = "Session 2 - Pull",
                    Date = today.AddDays(-1),
                    Description = "Demo sessie: pull focus",
                    OwnerId = ownerId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDeleted = false
                }
            };

            ctx.Sessions.AddRange(sessions);
            await ctx.SaveChangesAsync();

            int SeId(string title) => sessions.First(s => s.Title == title).Id;

            var sets = new List<SessionSet>();
            int setNr;

            setNr = 1;
            sets.Add(new SessionSet { SessionId = SeId("Session 1 - Push"), SetNumber = setNr++, ExerciseId = ExId("Bench Press"), Reps = 5, Weight = 57.5 });
            sets.Add(new SessionSet { SessionId = SeId("Session 1 - Push"), SetNumber = setNr++, ExerciseId = ExId("Bench Press"), Reps = 5, Weight = 57.5 });
            sets.Add(new SessionSet { SessionId = SeId("Session 1 - Push"), SetNumber = setNr++, ExerciseId = ExId("Overhead Press"), Reps = 8, Weight = 30 });
            sets.Add(new SessionSet { SessionId = SeId("Session 1 - Push"), SetNumber = setNr++, ExerciseId = ExId("Tricep Pushdowns"), Reps = 12, Weight = 22.5 });

            setNr = 1;
            sets.Add(new SessionSet { SessionId = SeId("Session 2 - Pull"), SetNumber = setNr++, ExerciseId = ExId("Pull-Ups"), Reps = 6, Weight = 0 });
            sets.Add(new SessionSet { SessionId = SeId("Session 2 - Pull"), SetNumber = setNr++, ExerciseId = ExId("Barbell Row"), Reps = 8, Weight = 45 });
            sets.Add(new SessionSet { SessionId = SeId("Session 2 - Pull"), SetNumber = setNr++, ExerciseId = ExId("Bicep Curls"), Reps = 12, Weight = 10 });

            ctx.SessionSets.AddRange(sets);
            await ctx.SaveChangesAsync();
        }
    }
}
