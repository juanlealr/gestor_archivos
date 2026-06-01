using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FileManager.Core.Models;
using FileManager.Core.Services;

namespace FileManager.ViewModels
{
    /// <summary>
    /// ViewModel para la exploración de archivos con soporte para navegación,
    /// selección de elementos y gestión del historial.
    /// </summary>
    public class FileExplorerViewModel : ViewModelBase, IDisposable
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
        private string? _statusMessage;
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
        private ICommand? _changeViewCommand;
        private int _viewMode = 1; // 0=Detalles, 1=Iconos

        public FileExplorerViewModel(IFileService fileService, IClipboardService clipboardService, IDialogService dialogService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _items = new ObservableCollection<FileSystemItem>();
            _drives = new ObservableCollection<DriveInfoModel>();

            // Suscribirse a eventos del FileService
            _fileService.FileCreated += OnFileCreated;
            _fileService.FileDeleted += OnFileDeleted;
            _fileService.FileChanged += OnFileChanged;
            _fileService.FileRenamed += OnFileRenamed;
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
        /// Mensaje de estado para operaciones de pegado y errores de explorador.
        /// </summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Colección de unidades de disco disponibles.
        /// </summary>
        public ObservableCollection<DriveInfoModel> Drives
        {
            get => _drives;
            set => SetProperty(ref _drives, value);
        }

        /// <summary>
        /// Modo de vista actual (0=Detalles, 1=Iconos).
        /// </summary>
        public int ViewMode
        {
            get => _viewMode;
            set
            {
                SetProperty(ref _viewMode, value);
                OnPropertyChanged(nameof(IsDetailsView));
                OnPropertyChanged(nameof(IsListView));
                OnPropertyChanged(nameof(IsIconsView));
            }
        }

        /// <summary>
        /// Indica si la vista actual es Detalles.
        /// </summary>
        public bool IsDetailsView => ViewMode == 0;

        /// <summary>
        /// Indica si la vista actual es Lista (obsoleto - ya no se usa).
        /// </summary>
        public bool IsListView => false;

        /// <summary>
        /// Indica si la vista actual es Iconos.
        /// </summary>
        public bool IsIconsView => ViewMode == 1;

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

                string? itemName = null;
                var destinationPath = string.Empty;
                var clipboardPath = string.Empty;

                try
                {
                    clipboardPath = _clipboardService.GetClipboardPath();
                    if (string.IsNullOrEmpty(clipboardPath))
                    {
                        SetStatus("No hay nada para pegar.");
                        return;
                    }

                    itemName = Path.GetFileName(clipboardPath);
                    if (string.IsNullOrEmpty(itemName))
                    {
                        SetStatus("La ruta del portapapeles no es válida.");
                        return;
                    }

                    destinationPath = Path.Combine(path, itemName);
                    var normalizedSource = Path.GetFullPath(clipboardPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var normalizedDestination = Path.GetFullPath(destinationPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (_clipboardService.IsCutOperation)
                    {
                        if (string.Equals(normalizedSource, normalizedDestination, StringComparison.OrdinalIgnoreCase))
                        {
                            ShowError($"No se puede mover '{itemName}' a la misma ubicación.");
                            return;
                        }

                        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                        {
                            ShowError($"No se puede mover '{itemName}' porque ya existe un elemento con el mismo nombre en '{path}'.");
                            return;
                        }

                        await _fileService.MoveAsync(clipboardPath, destinationPath, overwrite: false);
                        _clipboardService.Clear();
                        SetStatus($"'{itemName}' movido a '{path}'.");
                    }
                    else
                    {
                        var uniqueDestination = GetUniqueDestinationPath(destinationPath);
                        await _fileService.CopyAsync(clipboardPath, uniqueDestination, overwrite: false);
                        var copiedName = Path.GetFileName(uniqueDestination);
                        SetStatus($"'{copiedName}' copiado a '{path}'.");
                    }

                    await LoadDirectoryAsync(path);
                }
                catch (IOException ex)
                {
                    var lowercase = ex.Message.ToLowerInvariant();
                    var safeName = string.IsNullOrEmpty(itemName) ? "el archivo" : itemName;

                    if (lowercase.Contains("ya existe") || lowercase.Contains("already exists"))
                    {
                        var message = $"No se puede mover '{safeName}' porque ya existe un elemento con el mismo nombre en '{path}'.";
                        SetStatus(message);
                        System.Diagnostics.Debug.WriteLine(message);
                    }
                    else if (lowercase.Contains("same file") || lowercase.Contains("same path") || lowercase.Contains("source and destination path are the same"))
                    {
                        var message = $"No se puede mover '{safeName}' a la misma ubicación.";
                        ShowError(message);
                    }
                    else
                    {
                        var message = $"Error al pegar: {ex.Message}";
                        ShowError(message);
                    }
                }
                catch (Exception ex)
                {
                    var message = $"Error al pegar: {ex.Message}";
                    SetStatus(message);
                    System.Diagnostics.Debug.WriteLine(message);
                }
            },
            path => !string.IsNullOrEmpty(_clipboardService.GetClipboardPath()));

        private static string GetUniqueDestinationPath(string destinationPath)
        {
            if (!File.Exists(destinationPath) && !Directory.Exists(destinationPath))
                return destinationPath;

            var directory = Path.GetDirectoryName(destinationPath) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(destinationPath);
            var extension = Path.GetExtension(destinationPath);
            var current = destinationPath;
            var index = 1;

            do
            {
                var suffix = index == 1 ? " - Copia" : $" - Copia ({index})";
                current = Path.Combine(directory, name + suffix + extension);
                index++;
            }
            while (File.Exists(current) || Directory.Exists(current));

            return current;
        }

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

        /// <summary>
        /// Comando para cambiar el modo de vista.
        /// </summary>
        public ICommand ChangeViewCommand =>
            _changeViewCommand ??= new RelayCommand<int>(mode =>
            {
                ViewMode = mode;
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

                // Activar el watcher para este directorio
                _fileService.WatchDirectory(path);
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

        private void SetStatus(string message)
        {
            StatusMessage = message;
            System.Diagnostics.Debug.WriteLine($"[FileExplorerViewModel] {message}");
        }

        private void ShowError(string message)
        {
            SetStatus(message);
            MessageBox.Show(message, "Error al pegar", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Helper para construir un FileSystemItem desde una ruta.
        /// </summary>
        private static FileSystemItem? BuildItem(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    var dir = new DirectoryInfo(path);
                    return new FileSystemItem
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
                    };
                }
                else if (File.Exists(path))
                {
                    var file = new FileInfo(path);
                    return new FileSystemItem
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
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al construir item: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Manejador para archivos creados.
        /// </summary>
        private void OnFileCreated(object? sender, FileSystemEventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // No agregar si ya existe
                if (Items.Any(i => i.FullPath == e.FullPath)) return;

                var newItem = BuildItem(e.FullPath);
                if (newItem != null)
                {
                    Items.Add(newItem);
                    System.Diagnostics.Debug.WriteLine($"[FileWatcher] Creado: {e.Name}");
                }
            });
        }

        /// <summary>
        /// Manejador para archivos eliminados.
        /// </summary>
        private void OnFileDeleted(object? sender, FileSystemEventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var item = Items.FirstOrDefault(i => i.FullPath == e.FullPath);
                if (item != null)
                {
                    Items.Remove(item);
                    System.Diagnostics.Debug.WriteLine($"[FileWatcher] Eliminado: {e.Name}");
                }
            });
        }

        /// <summary>
        /// Manejador para cambios en archivos.
        /// </summary>
        private void OnFileChanged(object? sender, FileSystemEventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var item = Items.FirstOrDefault(i => i.FullPath == e.FullPath);
                if (item == null) return;

                var idx = Items.IndexOf(item);
                var updated = BuildItem(e.FullPath);
                if (updated != null)
                {
                    Items[idx] = updated;
                    System.Diagnostics.Debug.WriteLine($"[FileWatcher] Modificado: {e.Name}");
                }
            });
        }

        /// <summary>
        /// Manejador para archivos renombrados.
        /// </summary>
        private void OnFileRenamed(object? sender, RenamedEventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var item = Items.FirstOrDefault(i => i.FullPath == e.OldFullPath);
                if (item == null) return;

                var idx = Items.IndexOf(item);
                var updated = BuildItem(e.FullPath);
                if (updated != null)
                {
                    Items[idx] = updated;
                    System.Diagnostics.Debug.WriteLine($"[FileWatcher] Renombrado: {e.OldName} -> {e.Name}");
                }
            });
        }

        /// <summary>
        /// Limpia las suscripciones a eventos.
        /// </summary>
        public void Dispose()
        {
            _fileService.FileCreated -= OnFileCreated;
            _fileService.FileDeleted -= OnFileDeleted;
            _fileService.FileChanged -= OnFileChanged;
            _fileService.FileRenamed -= OnFileRenamed;
        }
    }
}
