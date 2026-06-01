using FileManager.Core.Helpers;

namespace FileManager.Core.Models
{
    /// <summary>
    /// Representa un elemento del sistema de archivos (archivo o carpeta).
    /// </summary>
    public class FileSystemItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime AccessedAt { get; set; }
        public string Extension { get; set; } = string.Empty;
        public FileAttributes Attributes { get; set; }
        public bool IsHidden => (Attributes & FileAttributes.Hidden) != 0;
        public bool IsSystem => (Attributes & FileAttributes.System) != 0;
        public bool IsReadOnly => (Attributes & FileAttributes.ReadOnly) != 0;

        // Para mostrar en la UI de forma amigable
        public string DisplaySize => IsDirectory ? "<Carpeta>" : FileSizeHelper.Format(Size);
        public string DisplayType => IsDirectory ? "Carpeta de archivos" : (Extension.TrimStart('.').ToUpper() + " File");

        public override string ToString()
        {
            return Name;
        }
    }
}