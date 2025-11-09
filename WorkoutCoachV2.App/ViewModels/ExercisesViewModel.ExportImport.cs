// Extra: export/import van oefeningen (JSON/CSV) via ExportImportService.

using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WorkoutCoachV2.App.Services;
using WorkoutCoachV2.App.Utils;

namespace WorkoutCoachV2.App.ViewModels
{
    public partial class ExercisesViewModel
    {
        // Lazy service-resolving (hergebruik binnen deze VM).
        private ExportImportService? _exportSvc;
        private ExportImportService ExportSvc =>
            _exportSvc ??= new ExportImportService(_scopeFactory);

        // Async-commands voor export en import.
        private ICommand? _exportExercisesCommand, _importExercisesCommand;
        public ICommand ExportExercisesCommand => _exportExercisesCommand ??= new AsyncRelayCommand(ExportExercisesAsync);
        public ICommand ImportExercisesCommand => _importExercisesCommand ??= new AsyncRelayCommand(ImportExercisesAsync);

        // Export: schrijft JSON of CSV met niet-gedelete oefeningen.
        private async Task ExportExercisesAsync()
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "JSON (*.json)|*.json|CSV (*.csv)|*.csv",
                    FileName = $"exercises_{DateTime.Now:yyyyMMdd}.json"
                };
                if (dlg.ShowDialog() != true) return;

                var count = await ExportSvc.ExportExercisesAsync(dlg.FileName);
                MessageBox.Show($"Export klaar: {count} oefening(en).", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export mislukt:\n{ex.Message}", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Import: leest JSON/CSV, voegt nieuwe toe en “herstelt” soft-deletes.
        private async Task ImportExercisesAsync()
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "JSON/CSV (*.json;*.csv)|*.json;*.csv|JSON (*.json)|*.json|CSV (*.csv)|*.csv"
                };
                if (dlg.ShowDialog() != true) return;

                var created = await ExportSvc.ImportExercisesAsync(dlg.FileName);
                MessageBox.Show($"Import klaar. Nieuwe items: {created}.", "Import",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadAsync(); // refresh tabel
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import mislukt:\n{ex.Message}", "Import",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
