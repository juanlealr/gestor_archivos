using FileManager.Core.Models;

namespace FileManager.Core.Services
{
    /// <summary>
    /// Implementación del servicio de propiedades de archivos y carpetas.
    /// </summary>
    public class PropertiesService : IPropertiesService
    {
        public async Task<FileProperties> GetPropertiesAsync(string path)
        {
            return await Task.Run(() =>
            {
                if (File.Exists(path))
                    return GetFileProperties(path);
                else if (Directory.Exists(path))
                    return GetFolderProperties(path);
                else
                    throw new FileNotFoundException($"No se encontró: '{path}'");
            });
        }

        public async Task<long> GetFolderSizeAsync(string folderPath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                long total = 0;
                try
                {
                    foreach (var file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try { total += new FileInfo(file).Length; }
                        catch (UnauthorizedAccessException) { }
                    }
                }
                catch (OperationCanceledException) { /* el usuario canceló */ }
                catch (UnauthorizedAccessException) { }

                return total;
            }, cancellationToken);
        }

        public async Task SetAttributesAsync(string path, bool isReadOnly, bool isHidden)
        {
            await Task.Run(() =>
            {
                var attrs = File.Exists(path)
                    ? new FileInfo(path).Attributes
                    : new DirectoryInfo(path).Attributes;

                attrs = isReadOnly
                    ? attrs | FileAttributes.ReadOnly
                    : attrs & ~FileAttributes.ReadOnly;

                attrs = isHidden
                    ? attrs | FileAttributes.Hidden
                    : attrs & ~FileAttributes.Hidden;

                if (File.Exists(path))
                    File.SetAttributes(path, attrs);
                else
                    new DirectoryInfo(path).Attributes = attrs;
            });
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVADOS
        // ─────────────────────────────────────────────────────────────

        private static FileProperties GetFileProperties(string path)
        {
            var info = new FileInfo(path);
            return new FileProperties
            {
                Name = info.Name,
                FullPath = info.FullName,
                Type = info.Extension.TrimStart('.').ToUpper() + " File",
                Location = info.DirectoryName ?? string.Empty,
                SizeBytes = info.Length,
                SizeOnDisk = GetSizeOnDisk(info.FullName),
                CreatedAt = info.CreationTime,
                ModifiedAt = info.LastWriteTime,
                AccessedAt = info.LastAccessTime,
                Attributes = info.Attributes,
                IsReadOnly = info.IsReadOnly,
                IsHidden = (info.Attributes & FileAttributes.Hidden) != 0,
                IsSystem = (info.Attributes & FileAttributes.System) != 0,
                IsArchive = (info.Attributes & FileAttributes.Archive) != 0,
            };
        }

        private static FileProperties GetFolderProperties(string path)
        {
            var info = new DirectoryInfo(path);
            int files = 0, folders = 0;

            try
            {
                files = Directory.GetFiles(path).Length;
                folders = Directory.GetDirectories(path).Length;
            }
            catch (UnauthorizedAccessException) { }

            return new FileProperties
            {
                Name = info.Name,
                FullPath = info.FullName,
                Type = "Carpeta de archivos",
                Location = info.Parent?.FullName ?? string.Empty,
                SizeBytes = 0,       // Se calcula async con GetFolderSizeAsync
                SizeOnDisk = 0,
                CreatedAt = info.CreationTime,
                ModifiedAt = info.LastWriteTime,
                AccessedAt = info.LastAccessTime,
                Attributes = info.Attributes,
                IsReadOnly = (info.Attributes & FileAttributes.ReadOnly) != 0,
                IsHidden = (info.Attributes & FileAttributes.Hidden) != 0,
                IsSystem = (info.Attributes & FileAttributes.System) != 0,
                ContainsFiles = files,
                ContainsFolders = folders
            };
        }

        /// <summary>
        /// Calcula el tamaño real en disco (considera el cluster size de NTFS).
        /// </summary>
        private static long GetSizeOnDisk(string filePath)
        {
            try
            {
                var info = new FileInfo(filePath);
                // Aproximación: redondear al múltiplo de 4096 bytes (cluster típico NTFS)
                const long clusterSize = 4096;
                return ((info.Length + clusterSize - 1) / clusterSize) * clusterSize;
            }
            catch { return 0; }
        }
    }
}