using FileManager.Core.Helpers;

namespace FileManager.Core.Models
{
    /// <summary>
    /// Información de una unidad de disco del sistema.
    /// </summary>
    public class DriveInfoModel
    {
        public string Name { get; set; } = string.Empty;          // Ej: "C:\"
        public string Label { get; set; } = string.Empty;         // Ej: "Windows"
        public string DriveType { get; set; } = string.Empty;     // Ej: "Fixed", "Removable"
        public string FileSystem { get; set; } = string.Empty;    // Ej: "NTFS"
        public long TotalSize { get; set; }
        public long FreeSpace { get; set; }
        public long UsedSpace => TotalSize - FreeSpace;
        public bool IsReady { get; set; }
        public string DisplayName => string.IsNullOrEmpty(Label)
            ? $"Disco local ({Name.TrimEnd('\\')})"
            : $"{Label} ({Name.TrimEnd('\\')})";
        public string DisplayFreeSpace => $"{FileSizeHelper.Format(FreeSpace)} libres de {FileSizeHelper.Format(TotalSize)}";
    }
}