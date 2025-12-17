using System.Diagnostics.CodeAnalysis;
using WorkoutCoachV2.Maui.Models;

namespace WorkoutCoachV2.Maui.Utilities
{
    /// <summary>
    /// Project Model Extentions
    /// </summary>
    public static class ProjectExtentions
    {
        /// <summary>
        /// Check if the project is null or new.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public static bool IsNullOrNew([NotNullWhen(false)] this Project? project)
        {
            return project is null || project.ID == 0;
        }
    }
}