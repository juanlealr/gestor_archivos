using System.Collections.ObjectModel;
using System.Windows.Input;
using FileManager.Core.Models;
using FileManager.Core.Services;

namespace FileManager.ViewModels
{
    /// <summary>
    /// ViewModel para la exploración de archivos con soporte para navegación,
    /// selección de elementos y gestión del historial.
    /// </summary>
    public class FileExplorerViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private readonly Stack<string> _backHistory = new();
        private readonly Stack<string> _forwardHistory = new();
        
        private string _currentPath = "";
        private FileSystemItem? _selectedItem;
        private bool _isLoading;
        private ObservableCollection<FileSystemItem> _items;
        private ObservableCollection<DriveInfoModel> _drives;
        private ICommand? _openFileCommand;
        private ICommand? _navigateBackCommand;
        private ICommand? _navigateForwardCommand;
        private ICommand? _navigateToPathCommand;

        public FileExplorerViewModel(IFileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _items = new ObservableCollection<FileSystemItem>();
            _drives = new ObservableCollection<DriveInfoModel>();
        }

        #region Propiedades

        /// <summary>
        /// Ruta del directorio actual.
        /// </summary>
        public string CurrentPath
        {
            get => _currentPath;
            set => SetProperty(ref _currentPath, value);
        }

        /// <summary>
        /// Item actualmente seleccionado en la vista.
        /// </summary>
        public FileSystemItem? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        /// <summary>
        /// Indica si se está cargando directorio.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Colección de elementos del directorio actual.
        /// </summary>
        public ObservableCollection<FileSystemItem> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// Colección de unidades de disco disponibles.
        /// </summary>
        public ObservableCollection<DriveInfoModel> Drives
        {
            get => _drives;
            set => SetProperty(ref _drives, value);
        }

        #endregion

        #region Comandos

        /// <summary>
        /// Comando para abrir un archivo o navegar a una carpeta.
        /// </summary>
        public ICommand OpenFileCommand =>
            _openFileCommand ??= new RelayCommand<FileSystemItem?>(async item =>
            {
                if (item == null) return;

                if (item.IsDirectory)
                {
                    // Guardar posición actual en historial
                    if (!string.IsNullOrEmpty(CurrentPath))
                        _backHistory.Push(CurrentPath);
                    
                    _forwardHistory.Clear();
                    await NavigateToAsync(item.FullPath);
                }
                else
                {
                    // Abrir archivo
                    try
                    {
                        await _fileService.OpenFileAsync(item.FullPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al abrir archivo: {ex.Message}");
                    }
                }
            });

        /// <summary>
        /// Comando para navegar hacia atrás en el historial.
        /// </summary>
        public ICommand NavigateBackCommand =>
            _navigateBackCommand ??= new RelayCommand(
                async _ =>
                {
                    if (_backHistory.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(CurrentPath))
                            _forwardHistory.Push(CurrentPath);

                        var previousPath = _backHistory.Pop();
                        CurrentPath = previousPath;
                        await LoadDirectoryAsync(previousPath);
                    }
                },
                _ => _backHistory.Count > 0);

        /// <summary>
        /// Comando para navegar hacia adelante en el historial.
        /// </summary>
        public ICommand NavigateForwardCommand =>
            _navigateForwardCommand ??= new RelayCommand(
                async _ =>
                {
                    if (_forwardHistory.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(CurrentPath))
                            _backHistory.Push(CurrentPath);

                        var nextPath = _forwardHistory.Pop();
                        CurrentPath = nextPath;
                        await LoadDirectoryAsync(nextPath);
                    }
                },
                _ => _forwardHistory.Count > 0);

        /// <summary>
        /// Comando para navegar a una ruta específica.
        /// </summary>
        public ICommand NavigateToPathCommand =>
            _navigateToPathCommand ??= new RelayCommand<string>(async path =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    if (!string.IsNullOrEmpty(CurrentPath))
                        _backHistory.Push(CurrentPath);
                    
                    _forwardHistory.Clear();
                    await NavigateToAsync(path);
                }
            });

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Carga las unidades disponibles del sistema.
        /// </summary>
        public async Task LoadDrivesAsync()
        {
            try
            {
                IsLoading = true;
                var drives = await _fileService.GetDrivesAsync();
                Drives.Clear();
                foreach (var drive in drives)
                {
                    Drives.Add(drive);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar unidades: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Navega a una ruta específica.
        /// </summary>
        public async Task NavigateToAsync(string path)
        {
            CurrentPath = path;
            await LoadDirectoryAsync(path);
        }

        #endregion

        #region Métodos Privados

        /// <summary>
        /// Carga el contenido de un directorio.
        /// </summary>
        private async Task LoadDirectoryAsync(string path)
        {
            try
            {
                IsLoading = true;

                if (!_fileService.DirectoryExists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"Directorio no existe: {path}");
                    return;
                }

                var items = await _fileService.ListDirectoryAsync(path, showHidden: false);
                Items.Clear();

                foreach (var item in items.OrderByDescending(x => x.IsDirectory).ThenBy(x => x.Name))
                {
                    Items.Add(item);
                }

                SelectedItem = null;
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Acceso denegado: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar directorio: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}
