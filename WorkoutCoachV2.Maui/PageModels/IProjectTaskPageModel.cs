using CommunityToolkit.Mvvm.Input;
using WorkoutCoachV2.Maui.Models;

namespace WorkoutCoachV2.Maui.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}