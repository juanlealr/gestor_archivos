namespace FileManager.Core.Helpers
{
    /// <summary>
    /// Convierte tamaños en bytes a formato legible para humanos.
    /// </summary>
    public static class FileSizeHelper
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

        public static string Format(long bytes)
        {
            if (bytes < 0) return "Desconocido";
            if (bytes == 0) return "0 B";

            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < Units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? $"{bytes} {Units[unitIndex]}"
                : $"{size:F2} {Units[unitIndex]}";
        }

        /// <summary>
        /// Devuelve el tamaño exacto en bytes con separador de miles.
        /// </summary>
        public static string FormatExact(long bytes)
        {
            return $"{bytes:N0} bytes";
        }
    }
}