using FileManager.Core.Models;

namespace FileManager.Core.Services
{
    /// <summary>
    /// Contrato principal del servicio de archivos.
    /// Persona 3 (ViewModels) usará este interface con mocks hasta que esté implementado.
    /// </summary>
    public interface IFileService
    {
        // Navegación
        Task<IEnumerable<FileSystemItem>> ListDirectoryAsync(string path, bool showHidden = false);
        Task<IEnumerable<DriveInfoModel>> GetDrivesAsync();
        bool DirectoryExists(string path);
        bool FileExists(string path);

        // Operaciones básicas
        Task OpenFileAsync(string path);
        Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = false);
        Task MoveAsync(string sourcePath, string destinationPath, bool overwrite = false);
        Task RenameAsync(string path, string newName);
        Task DeleteAsync(string path, bool recursive = false);
        Task CreateFolderAsync(string parentPath, string folderName);

        // Búsqueda
        Task<IEnumerable<FileSystemItem>> SearchAsync(string rootPath, string pattern, bool includeSubfolders = true);

        // Favoritos
        Task<IEnumerable<string>> GetFavoritesAsync();
        Task AddFavoriteAsync(string path);
        Task RemoveFavoriteAsync(string path);

        // Eventos (para FileSystemWatcher)
        event EventHandler<FileSystemEventArgs>? FileCreated;
        event EventHandler<FileSystemEventArgs>? FileDeleted;
        event EventHandler<FileSystemEventArgs>? FileChanged;
        event EventHandler<RenamedEventArgs>? FileRenamed;
    }
}