namespace FileManager.Core.Services
{
    /// <summary>
    /// Interfaz para gestionar operaciones de portapapeles (cortar/copiar/pegar).
    /// </summary>
    public interface IClipboardService
    {
        /// <summary>
        /// Copia una ruta al portapapeles.
        /// </summary>
        void CopyPath(string path);

        /// <summary>
        /// Corta una ruta al portapapeles.
        /// </summary>
        void CutPath(string path);

        /// <summary>
        /// Obtiene el contenido del portapapeles (ruta almacenada).
        /// </summary>
        string? GetClipboardPath();

        /// <summary>
        /// Indica si el portapapeles contiene una operación de corte.
        /// </summary>
        bool IsCutOperation { get; }

        /// <summary>
        /// Limpia el portapapeles.
        /// </summary>
        void Clear();
    }
}
