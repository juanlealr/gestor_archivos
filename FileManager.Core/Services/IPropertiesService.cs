using FileManager.Core.Models;

namespace FileManager.Core.Services
{
    /// <summary>
    /// Contrato para obtener propiedades detalladas de archivos y carpetas.
    /// </summary>
    public interface IPropertiesService
    {
        Task<FileProperties> GetPropertiesAsync(string path);
        Task<long> GetFolderSizeAsync(string folderPath, CancellationToken cancellationToken = default);
        Task SetAttributesAsync(string path, bool isReadOnly, bool isHidden);
    }
}