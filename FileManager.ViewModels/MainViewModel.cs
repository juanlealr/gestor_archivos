using FileManager.Core.Services;
using FileManager.Core.Models;

namespace FileManager.ViewModels
{
    /// <summary>
    /// ViewModel principal que gestiona la lógica de la aplicación.
    /// Utiliza inyección de dependencias para acceder a los servicios.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private FileExplorerViewModel? _fileExplorerViewModel;

        public MainViewModel(IFileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            
            // Inicializar el FileExplorerViewModel
            FileExplorerViewModel = new FileExplorerViewModel(_fileService);
        }

        /// <summary>
        /// ViewModel del explorador de archivos.
        /// </summary>
        public FileExplorerViewModel? FileExplorerViewModel
        {
            get => _fileExplorerViewModel;
            set => SetProperty(ref _fileExplorerViewModel, value);
        }

        /// <summary>
        /// Inicializa la aplicación cargando las unidades disponibles.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (FileExplorerViewModel != null)
            {
                await FileExplorerViewModel.LoadDrivesAsync();
            }
        }
    }
}
