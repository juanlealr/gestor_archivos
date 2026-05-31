namespace FileManager.Core.Services
{
    /// <summary>
    /// Implementación del servicio de portapapeles para operaciones de archivos.
    /// Gestiona copiar, cortar y pegar archivos internamente.
    /// </summary>
    public class ClipboardService : IClipboardService
    {
        private string? _clipboardPath;
        private bool _isCutOperation;

        public bool IsCutOperation => _isCutOperation;

        public void CopyPath(string path)
        {
            _clipboardPath = path;
            _isCutOperation = false;
        }

        public void CutPath(string path)
        {
            _clipboardPath = path;
            _isCutOperation = true;
        }

        public string? GetClipboardPath()
        {
            return _clipboardPath;
        }

        public void Clear()
        {
            _clipboardPath = null;
            _isCutOperation = false;
        }
    }
}
