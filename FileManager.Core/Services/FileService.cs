using FileManager.Core.Models;
using FileManager.Core.Helpers;
using Newtonsoft.Json;

namespace FileManager.Core.Services
{
    /// <summary>
    /// Implementación principal del servicio de archivos.
    /// Gestiona toda la interacción con System.IO., la libreria que permite interactuar con el SO.
    /// </summary>
    public class FileService : IFileService, IDisposable
    {
        private FileSystemWatcher? _watcher;
        private string _favoritesFilePath;

        public event EventHandler<FileSystemEventArgs>? FileCreated;
        public event EventHandler<FileSystemEventArgs>? FileDeleted;
        public event EventHandler<FileSystemEventArgs>? FileChanged;
        public event EventHandler<RenamedEventArgs>? FileRenamed;

        public FileService()
        {
            // Guarda favoritos en AppData del usuario
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "FileManagerApp");
            Directory.CreateDirectory(appFolder);
            _favoritesFilePath = Path.Combine(appFolder, "favorites.json");
        }

        // ─────────────────────────────────────────────────────────────
        // NAVEGACIÓN
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<FileSystemItem>> ListDirectoryAsync(string path, bool showHidden = false)
        {
            return await Task.Run(() =>
            {
                var items = new List<FileSystemItem>();

                try
                {
                    var dirInfo = new DirectoryInfo(path);

                    // Carpetas primero
                    foreach (var dir in dirInfo.GetDirectories())
                    {
                        try
                        {
                            if (!showHidden && (dir.Attributes & FileAttributes.Hidden) != 0) continue;
                            if (!showHidden && (dir.Attributes & FileAttributes.System) != 0) continue;

                            items.Add(new FileSystemItem
                            {
                                Name = dir.Name,
                                FullPath = dir.FullName,
                                IsDirectory = true,
                                Size = 0,
                                CreatedAt = dir.CreationTime,
                                ModifiedAt = dir.LastWriteTime,
                                AccessedAt = dir.LastAccessTime,
                                Extension = string.Empty,
                                Attributes = dir.Attributes
                            });
                        }
                        catch (UnauthorizedAccessException) { /* Silencioso: sin permiso */ }
                    }

                    // Luego archivos
                    foreach (var file in dirInfo.GetFiles())
                    {
                        try
                        {
                            if (!showHidden && (file.Attributes & FileAttributes.Hidden) != 0) continue;
                            if (!showHidden && (file.Attributes & FileAttributes.System) != 0) continue;

                            items.Add(new FileSystemItem
                            {
                                Name = file.Name,
                                FullPath = file.FullName,
                                IsDirectory = false,
                                Size = file.Length,
                                CreatedAt = file.CreationTime,
                                ModifiedAt = file.LastWriteTime,
                                AccessedAt = file.LastAccessTime,
                                Extension = file.Extension,
                                Attributes = file.Attributes
                            });
                        }
                        catch (UnauthorizedAccessException) { /* Silencioso: sin permiso */ }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new InvalidOperationException($"Sin acceso a '{path}': {ex.Message}", ex);
                }
                catch (DirectoryNotFoundException ex)
                {
                    throw new InvalidOperationException($"Directorio no encontrado: '{path}'", ex);
                }

                return items.AsEnumerable();
            });
        }

        public async Task<IEnumerable<DriveInfoModel>> GetDrivesAsync()
        {
            return await Task.Run(() =>
            {
                return DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d =>
                    {
                        try
                        {
                            return new DriveInfoModel
                            {
                                Name = d.Name,
                                Label = d.VolumeLabel,
                                DriveType = d.DriveType.ToString(),
                                FileSystem = d.DriveFormat,
                                TotalSize = d.TotalSize,
                                FreeSpace = d.AvailableFreeSpace,
                                IsReady = d.IsReady
                            };
                        }
                        catch
                        {
                            return new DriveInfoModel
                            {
                                Name = d.Name,
                                IsReady = false
                            };
                        }
                    })
                    .ToList();
            });
        }

        public bool DirectoryExists(string path) => Directory.Exists(path);
        public bool FileExists(string path) => File.Exists(path);

        // ─────────────────────────────────────────────────────────────
        // ABRIR ARCHIVO
        // ─────────────────────────────────────────────────────────────

        public async Task OpenFileAsync(string path)
        {
            await Task.Run(() =>
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Archivo no encontrado: '{path}'");

                // Resuelve accesos directos .lnk
                string targetPath = path;
                if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                    targetPath = ResolveLnk(path);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = targetPath,
                    UseShellExecute = true   // Abre con la app predeterminada del sistema
                };

                System.Diagnostics.Process.Start(psi);
            });
        }

        // ─────────────────────────────────────────────────────────────
        // OPERACIONES DE ARCHIVOS
        // ─────────────────────────────────────────────────────────────

        public async Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = false)
        {
            await Task.Run(() =>
            {
                ValidatePath(sourcePath, mustExist: true);

                if (Directory.Exists(sourcePath))
                {
                    CopyDirectoryRecursive(sourcePath, destinationPath, overwrite);
                }
                else
                {
                    var destDir = Path.GetDirectoryName(destinationPath)!;
                    Directory.CreateDirectory(destDir);
                    File.Copy(sourcePath, destinationPath, overwrite);
                }
            });
        }

        public async Task MoveAsync(string sourcePath, string destinationPath, bool overwrite = false)
        {
            await Task.Run(() =>
            {
                ValidatePath(sourcePath, mustExist: true);

                if (overwrite && File.Exists(destinationPath))
                    File.Delete(destinationPath);

                if (Directory.Exists(sourcePath))
                    Directory.Move(sourcePath, destinationPath);
                else
                    File.Move(sourcePath, destinationPath);
            });
        }

        public async Task RenameAsync(string path, string newName)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(newName))
                    throw new ArgumentException("El nuevo nombre no puede estar vacío.");

                char[] invalid = Path.GetInvalidFileNameChars();
                if (newName.Any(c => invalid.Contains(c)))
                    throw new ArgumentException($"El nombre '{newName}' contiene caracteres inválidos.");

                var parent = Path.GetDirectoryName(path)
                    ?? throw new InvalidOperationException("No se puede determinar el directorio padre.");

                var newPath = Path.Combine(parent, newName);

                if (File.Exists(newPath) || Directory.Exists(newPath))
                    throw new InvalidOperationException($"Ya existe un elemento con el nombre '{newName}'.");

                if (Directory.Exists(path))
                    Directory.Move(path, newPath);
                else
                    File.Move(path, newPath);
            });
        }

        public async Task DeleteAsync(string path, bool recursive = false)
        {
            await Task.Run(() =>
            {
                ValidatePath(path, mustExist: true);

                if (Directory.Exists(path))
                    Directory.Delete(path, recursive);
                else
                    File.Delete(path);
            });
        }

        public async Task CreateFolderAsync(string parentPath, string folderName)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(folderName))
                    throw new ArgumentException("El nombre de carpeta no puede estar vacío.");

                var newPath = Path.Combine(parentPath, folderName);

                if (Directory.Exists(newPath))
                    throw new InvalidOperationException($"Ya existe una carpeta con el nombre '{folderName}'.");

                Directory.CreateDirectory(newPath);
            });
        }

        // ─────────────────────────────────────────────────────────────
        // BÚSQUEDA
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<FileSystemItem>> SearchAsync(
            string rootPath, string pattern, bool includeSubfolders = true)
        {
            return await Task.Run(() =>
            {
                var results = new List<FileSystemItem>();
                var option = includeSubfolders
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                try
                {
                    foreach (var file in Directory.EnumerateFiles(rootPath, $"*{pattern}*", option))
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            results.Add(new FileSystemItem
                            {
                                Name = info.Name,
                                FullPath = info.FullName,
                                IsDirectory = false,
                                Size = info.Length,
                                CreatedAt = info.CreationTime,
                                ModifiedAt = info.LastWriteTime,
                                AccessedAt = info.LastAccessTime,
                                Extension = info.Extension,
                                Attributes = info.Attributes
                            });
                        }
                        catch (UnauthorizedAccessException) { }
                    }

                    foreach (var dir in Directory.EnumerateDirectories(rootPath, $"*{pattern}*", option))
                    {
                        try
                        {
                            var info = new DirectoryInfo(dir);
                            results.Add(new FileSystemItem
                            {
                                Name = info.Name,
                                FullPath = info.FullName,
                                IsDirectory = true,
                                Size = 0,
                                CreatedAt = info.CreationTime,
                                ModifiedAt = info.LastWriteTime,
                                AccessedAt = info.LastAccessTime,
                                Extension = string.Empty,
                                Attributes = info.Attributes
                            });
                        }
                        catch (UnauthorizedAccessException) { }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new InvalidOperationException($"Sin acceso al directorio de búsqueda: {ex.Message}", ex);
                }

                return results.AsEnumerable();
            });
        }

        // ─────────────────────────────────────────────────────────────
        // FAVORITOS (persistidos en JSON)
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<string>> GetFavoritesAsync()
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(_favoritesFilePath))
                    return Enumerable.Empty<string>();

                var json = File.ReadAllText(_favoritesFilePath);
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            });
        }

        public async Task AddFavoriteAsync(string path)
        {
            var favorites = (await GetFavoritesAsync()).ToList();
            if (!favorites.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                favorites.Add(path);
                await SaveFavoritesAsync(favorites);
            }
        }

        public async Task RemoveFavoriteAsync(string path)
        {
            var favorites = (await GetFavoritesAsync()).ToList();
            favorites.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
            await SaveFavoritesAsync(favorites);
        }

        // ─────────────────────────────────────────────────────────────
        // FILESYSTEMWATCHER (notificaciones en tiempo real)
        // ─────────────────────────────────────────────────────────────

        public void WatchDirectory(string path)
        {
            _watcher?.Dispose();

            if (!Directory.Exists(path)) return;

            _watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.FileName
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Size,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Created += (s, e) => FileCreated?.Invoke(this, e);
            _watcher.Deleted += (s, e) => FileDeleted?.Invoke(this, e);
            _watcher.Changed += (s, e) => FileChanged?.Invoke(this, e);
            _watcher.Renamed += (s, e) => FileRenamed?.Invoke(this, e);
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVADOS / AUXILIARES
        // ─────────────────────────────────────────────────────────────

        private static void ValidatePath(string path, bool mustExist = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("La ruta no puede estar vacía.");

            if (mustExist && !File.Exists(path) && !Directory.Exists(path))
                throw new FileNotFoundException($"No existe el elemento: '{path}'");
        }

        private static void CopyDirectoryRecursive(string source, string destination, bool overwrite)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source))
            {
                var dest = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, dest, overwrite);
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                var dest = Path.Combine(destination, Path.GetFileName(dir));
                CopyDirectoryRecursive(dir, dest, overwrite);
            }
        }

        private async Task SaveFavoritesAsync(List<string> favorites)
        {
            var json = JsonConvert.SerializeObject(favorites, Formatting.Indented);
            await File.WriteAllTextAsync(_favoritesFilePath, json);
        }

        private static string ResolveLnk(string lnkPath)
        {
            try
            {
                var shell = new object();
                return lnkPath;
            }
            catch { return lnkPath; }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}