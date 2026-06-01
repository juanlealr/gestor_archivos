namespace FileManager.Core.Services
{
    /// <summary>
    /// Interfaz para mostrar diálogos de entrada y confirmación.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Muestra un diálogo de entrada de texto.
        /// </summary>
        string? ShowInputDialog(string title, string defaultValue = "");

        /// <summary>
        /// Muestra un diálogo de confirmación.
        /// </summary>
        bool ShowConfirmDialog(string title, string message);

        /// <summary>
        /// Muestra un diálogo de error.
        /// </summary>
        void ShowError(string title, string message);
    }
}
