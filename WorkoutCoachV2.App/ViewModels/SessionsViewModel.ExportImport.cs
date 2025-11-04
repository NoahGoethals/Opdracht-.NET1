using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WorkoutCoachV2.App.Services;
using WorkoutCoachV2.App.Utils;

namespace WorkoutCoachV2.App.ViewModels
{
    public partial class SessionsViewModel
    {
        private ExportImportService? _exportSvc;
        private ExportImportService ExportSvc =>
            _exportSvc ??= new ExportImportService(_scopeFactory);

        private ICommand? _exportSessionsCommand, _importSessionsCommand;
        public ICommand ExportSessionsCommand => _exportSessionsCommand ??= new AsyncRelayCommand(ExportSessionsAsync);
        public ICommand ImportSessionsCommand => _importSessionsCommand ??= new AsyncRelayCommand(ImportSessionsAsync);

        private async Task ExportSessionsAsync()
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "JSON (*.json)|*.json",
                    FileName = $"sessions_{DateTime.Now:yyyyMMdd}.json"
                };
                if (dlg.ShowDialog() != true) return;

                var count = await ExportSvc.ExportSessionsAsync(dlg.FileName);
                MessageBox.Show($"Export klaar: {count} sessie(s).", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export mislukt:\n{ex.Message}", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task ImportSessionsAsync()
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "JSON (*.json)|*.json"
                };
                if (dlg.ShowDialog() != true) return;

                var created = await ExportSvc.ImportSessionsAsync(dlg.FileName);
                MessageBox.Show($"Import klaar. Nieuwe sessies: {created}.", "Import",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadAsync(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import mislukt:\n{ex.Message}", "Import",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
