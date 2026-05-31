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
        public string? ShowInputDialog(string title, string defaultValue = "")
        {
            return InputDialog.Show(title, defaultValue);
        }

        public bool ShowConfirmDialog(string title, string message)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}
