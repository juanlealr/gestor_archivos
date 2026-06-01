using FileManager.Core.Models;
using FileManager.Core.Services;

namespace FileManager.ViewModels
{
    /// <summary>
    /// ViewModel principal que gestiona la lógica de la aplicación.
    /// Utiliza inyección de dependencias para acceder a los servicios.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly FileExplorerViewModel _fileExplorerViewModel;
        private readonly NavigationViewModel _navigationViewModel;
        private readonly FileOperationsViewModel _fileOperationsViewModel;
        private readonly SelectionViewModel _selectionViewModel;
        private readonly PropertiesViewModel _propertiesViewModel;

        public MainViewModel(
            IFileService fileService,
            IClipboardService clipboardService,
            IDialogService dialogService,
            NavigationViewModel navigationViewModel,
            FileOperationsViewModel fileOperationsViewModel,
            SelectionViewModel selectionViewModel,
            PropertiesViewModel propertiesViewModel)
        {
            _navigationViewModel = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
            _fileOperationsViewModel = fileOperationsViewModel ?? throw new ArgumentNullException(nameof(fileOperationsViewModel));
            _selectionViewModel = selectionViewModel ?? throw new ArgumentNullException(nameof(selectionViewModel));
            _propertiesViewModel = propertiesViewModel ?? throw new ArgumentNullException(nameof(propertiesViewModel));

            _fileExplorerViewModel = new FileExplorerViewModel(
                fileService ?? throw new ArgumentNullException(nameof(fileService)),
                clipboardService ?? throw new ArgumentNullException(nameof(clipboardService)),
                dialogService ?? throw new ArgumentNullException(nameof(dialogService)));
        }

        /// <summary>
        /// ViewModel del explorador de archivos.
        /// </summary>
        public FileExplorerViewModel FileExplorerViewModel => _fileExplorerViewModel;

        /// <summary>
        /// ViewModel que controla la navegación y el breadcrumb.
        /// </summary>
        public NavigationViewModel NavigationViewModel => _navigationViewModel;

        /// <summary>
        /// ViewModel que expone operaciones de archivos y estado de operación.
        /// </summary>
        public FileOperationsViewModel FileOperationsViewModel => _fileOperationsViewModel;

        /// <summary>
        /// ViewModel responsable de la selección múltiple y operaciones por lotes.
        /// </summary>
        public SelectionViewModel SelectionViewModel => _selectionViewModel;

        /// <summary>
        /// ViewModel usado por el diálogo de propiedades.
        /// </summary>
        public PropertiesViewModel PropertiesViewModel => _propertiesViewModel;

        /// <summary>
        /// Inicializa la aplicación cargando las unidades disponibles.
        /// </summary>
        public async Task InitializeAsync()
        {
            await FileExplorerViewModel.LoadDrivesAsync();
            SelectionViewModel.SetSourceItems(FileExplorerViewModel.Items);
        }
    }
}
