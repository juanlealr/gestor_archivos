namespace FileManager.Core.Helpers
{
    /// <summary>
    /// Devuelve un nombre de icono según el tipo de archivo.
    /// La UI usará este string para cargar el icono correcto de Material Design.
    /// </summary>
    public static class IconHelper
    {
        private static readonly Dictionary<string, string> ExtensionIcons = new(StringComparer.OrdinalIgnoreCase)
        {
            // Imágenes
            { ".jpg", "image" }, { ".jpeg", "image" }, { ".png", "image" },
            { ".gif", "image" }, { ".bmp", "image" }, { ".svg", "image" }, { ".webp", "image" },
            // Documentos
            { ".pdf", "picture_as_pdf" }, { ".doc", "description" }, { ".docx", "description" },
            { ".xls", "table_chart" }, { ".xlsx", "table_chart" },
            { ".ppt", "slideshow" }, { ".pptx", "slideshow" },
            { ".txt", "article" }, { ".md", "article" },
            // Código
            { ".cs", "code" }, { ".js", "code" }, { ".ts", "code" },
            { ".py", "code" }, { ".java", "code" }, { ".cpp", "code" },
            { ".html", "html" }, { ".css", "css" }, { ".json", "data_object" },
            // Multimedia
            { ".mp3", "music_note" }, { ".wav", "music_note" }, { ".flac", "music_note" },
            { ".mp4", "movie" }, { ".avi", "movie" }, { ".mkv", "movie" },
            // Comprimidos
            { ".zip", "folder_zip" }, { ".rar", "folder_zip" }, { ".7z", "folder_zip" },
            // Ejecutables
            { ".exe", "terminal" }, { ".msi", "terminal" }, { ".bat", "terminal" },
            // Accesos directos
            { ".lnk", "link" },
        };

        public static string GetIcon(FileManager.Core.Models.FileSystemItem item)
        {
            if (item.IsDirectory) return "folder";
            if (string.IsNullOrEmpty(item.Extension)) return "insert_drive_file";
            return ExtensionIcons.TryGetValue(item.Extension, out var icon) ? icon : "insert_drive_file";
        }

        public static string GetDriveIcon(string driveType)
        {
            return driveType switch
            {
                "CDRom" => "album",
                "Removable" => "usb",
                "Network" => "lan",
                _ => "storage"
            };
        }
    }
}