using FileManager.Core.Helpers;

namespace FileManager.Core.Models
{
    /// <summary>
    /// Propiedades detalladas de un archivo o carpeta, para mostrar en el diálogo de propiedades.
    /// </summary>
    public class FileProperties
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public long SizeOnDisk { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime AccessedAt { get; set; }
        public FileAttributes Attributes { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsHidden { get; set; }
        public bool IsSystem { get; set; }
        public bool IsArchive { get; set; }

        // Solo para carpetas o directorios
        public int ContainsFiles { get; set; }
        public int ContainsFolders { get; set; }

        public string DisplaySize => FileSizeHelper.Format(SizeBytes);
        public string DisplaySizeOnDisk => FileSizeHelper.Format(SizeOnDisk);
    }
}