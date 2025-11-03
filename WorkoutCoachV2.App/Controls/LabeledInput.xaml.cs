using System.Windows;
using System.Windows.Controls;

namespace WorkoutCoachV2.App.Controls
{
    public partial class LabeledInput : UserControl
    {
        public LabeledInput() => InitializeComponent();

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(LabeledInput), new PropertyMetadata(string.Empty));

        public object? Input
        {
            get => GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }
        public static readonly DependencyProperty InputProperty =
            DependencyProperty.Register(nameof(Input), typeof(object), typeof(LabeledInput), new PropertyMetadata(null));
    }
}
