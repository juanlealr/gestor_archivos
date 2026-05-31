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
        private readonly IClipboardService _clipboardService;
        private readonly IDialogService _dialogService;
        private FileExplorerViewModel? _fileExplorerViewModel;

        public MainViewModel(IFileService fileService, IClipboardService clipboardService, IDialogService dialogService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Inicializar el FileExplorerViewModel
            FileExplorerViewModel = new FileExplorerViewModel(_fileService, _clipboardService, _dialogService);
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
