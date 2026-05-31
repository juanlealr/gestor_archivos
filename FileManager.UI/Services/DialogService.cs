using FileManager.Core.Services;
using FileManager.UI.Dialogs;
using System.Windows;

namespace FileManager.UI.Services
{
    /// <summary>
    /// Implementación del servicio de diálogos.
    /// </summary>
    public class DialogService : IDialogService
    {
        private Window? _mainWindow;

        public DialogService()
        {
            // Obtener la ventana principal
            _mainWindow = Application.Current?.MainWindow;
        }

        public string? ShowInputDialog(string title, string defaultValue = "")
        {
            var dialog = new InputDialog(title, defaultValue);
            if (_mainWindow != null)
                dialog.Owner = _mainWindow;

            if (dialog.ShowDialog() == true)
            {
                return dialog.InputValue;
            }
            return null;
        }

        public bool ShowConfirmDialog(string title, string message)
        {
            var result = MessageBox.Show(_mainWindow, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}
