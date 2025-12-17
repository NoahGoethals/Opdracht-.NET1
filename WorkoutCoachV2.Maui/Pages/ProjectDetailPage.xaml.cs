using WorkoutCoachV2.Maui.Models;

namespace WorkoutCoachV2.Maui.Pages
{
    public partial class ProjectDetailPage : ContentPage
    {
        public ProjectDetailPage(ProjectDetailPageModel model)
        {
            InitializeComponent();

            BindingContext = model;
        }
    }
}
