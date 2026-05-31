using System;
using System.Collections.ObjectModel;
using System.IO;
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
        private readonly IClipboardService _clipboardService;
        private readonly IDialogService _dialogService;
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
        private ICommand? _navigateToBreadcrumbCommand;
        private ICommand? _newItemCommand;
        private ICommand? _cutCommand;
        private ICommand? _copyCommand;
        private ICommand? _pasteCommand;
        private ICommand? _renameCommand;
        private ICommand? _deleteCommand;
        private ICommand? _navigateToDriveCommand;

        public FileExplorerViewModel(IFileService fileService, IClipboardService clipboardService, IDialogService dialogService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
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

        /// <summary>
        /// Comando para navegar desde el breadcrumb.
        /// </summary>
        public ICommand NavigateToBreadcrumbCommand =>
            _navigateToBreadcrumbCommand ??= new RelayCommand<string>(async path =>
            {
                if (!string.IsNullOrEmpty(path) && path != CurrentPath)
                {
                    if (!string.IsNullOrEmpty(CurrentPath))
                        _backHistory.Push(CurrentPath);

                    _forwardHistory.Clear();
                    await NavigateToAsync(path);
                }
            });

        /// <summary>
        /// Comando para crear una nueva carpeta.
        /// </summary>
        public ICommand NewItemCommand =>
            _newItemCommand ??= new RelayCommand<string>(async path =>
            {
                if (string.IsNullOrEmpty(path)) return;

                try
                {
                    var folderName = _dialogService.ShowInputDialog("Nueva Carpeta", "Nueva Carpeta");

                    if (!string.IsNullOrEmpty(folderName))
                    {
                        await _fileService.CreateFolderAsync(path, folderName);
                        await LoadDirectoryAsync(path);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear carpeta: {ex.Message}");
                }
            });

        /// <summary>
        /// Comando para cortar un archivo/carpeta.
        /// </summary>
        public ICommand CutCommand =>
            _cutCommand ??= new RelayCommand<FileSystemItem?>(item =>
            {
                if (item != null)
                {
                    _clipboardService.CutPath(item.FullPath);
                }
            });

        /// <summary>
        /// Comando para copiar un archivo/carpeta.
        /// </summary>
        public ICommand CopyCommand =>
            _copyCommand ??= new RelayCommand<FileSystemItem?>(item =>
            {
                if (item != null)
                {
                    _clipboardService.CopyPath(item.FullPath);
                }
            });

        /// <summary>
        /// Comando para pegar un archivo/carpeta.
        /// </summary>
        public ICommand PasteCommand =>
            _pasteCommand ??= new RelayCommand<string>(async path =>
            {
                if (string.IsNullOrEmpty(path)) return;

                try
                {
                    var clipboardPath = _clipboardService.GetClipboardPath();
                    if (string.IsNullOrEmpty(clipboardPath)) return;

                    var itemName = Path.GetFileName(clipboardPath);
                    var destinationPath = Path.Combine(path, itemName);

                    if (_clipboardService.IsCutOperation)
                    {
                        await _fileService.MoveAsync(clipboardPath, destinationPath, overwrite: false);
                        _clipboardService.Clear();
                    }
                    else
                    {
                        await _fileService.CopyAsync(clipboardPath, destinationPath, overwrite: false);
                    }

                    await LoadDirectoryAsync(path);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al pegar: {ex.Message}");
                }
            },
            path => !string.IsNullOrEmpty(_clipboardService.GetClipboardPath()));

        /// <summary>
        /// Comando para renombrar un archivo/carpeta.
        /// </summary>
        public ICommand RenameCommand =>
            _renameCommand ??= new RelayCommand<FileSystemItem?>(async item =>
            {
                if (item == null) return;

                try
                {
                    var currentName = Path.GetFileName(item.FullPath);
                    var newName = _dialogService.ShowInputDialog("Renombrar", currentName);

                    if (!string.IsNullOrEmpty(newName) && newName != currentName)
                    {
                        await _fileService.RenameAsync(item.FullPath, newName);
                        if (!string.IsNullOrEmpty(CurrentPath))
                            await LoadDirectoryAsync(CurrentPath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al renombrar: {ex.Message}");
                }
            });

        /// <summary>
        /// Comando para eliminar un archivo/carpeta.
        /// </summary>
        public ICommand DeleteCommand =>
            _deleteCommand ??= new RelayCommand<FileSystemItem?>(async item =>
            {
                if (item == null) return;

                try
                {
                    if (_dialogService.ShowConfirmDialog("Confirmar eliminación", $"¿Está seguro de que desea eliminar '{item.Name}'?"))
                    {
                        await _fileService.DeleteAsync(item.FullPath, recursive: true);
                        if (!string.IsNullOrEmpty(CurrentPath))
                            await LoadDirectoryAsync(CurrentPath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar: {ex.Message}");
                }
            });

        /// <summary>
        /// Comando para navegar a una unidad de disco.
        /// </summary>
        public ICommand NavigateToDriveCommand =>
            _navigateToDriveCommand ??= new RelayCommand<DriveInfoModel?>(async drive =>
            {
                if (drive != null && drive.IsReady)
                {
                    if (!string.IsNullOrEmpty(CurrentPath))
                        _backHistory.Push(CurrentPath);

                    _forwardHistory.Clear();
                    await NavigateToAsync(drive.Name);
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
