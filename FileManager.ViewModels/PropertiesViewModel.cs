using System.Windows.Input;
using FileManager.Core.Models;
using FileManager.Core.Services;

namespace FileManager.ViewModels
{
    /// <summary>
    /// ViewModel para mostrar y editar propiedades de un archivo o carpeta.
    /// Soporta lectura de metadatos, edición de atributos (ReadOnly, Hidden)
    /// y cálculo asíncrono del tamaño de carpetas.
    /// </summary>
    public class PropertiesViewModel : ViewModelBase
    {
        private readonly IPropertiesService _propertiesService;

        // ─── Backing fields ──────────────────────────────────────────────────
        private FileProperties? _properties;
        private bool _isLoading;
        private bool _isSaving;
        private string? _errorMessage;
        private long _folderSizeBytes;
        private bool _isFolderSizeLoading;
        private bool _isReadOnly;
        private bool _isHidden;
        private CancellationTokenSource? _folderSizeCts;

        // Commands
        private ICommand? _saveAttributesCommand;
        private ICommand? _cancelFolderSizeCommand;
        private ICommand? _refreshCommand;

        public PropertiesViewModel(IPropertiesService propertiesService)
        {
            _propertiesService = propertiesService
                ?? throw new ArgumentNullException(nameof(propertiesService));
        }

        // ─── Propiedades de solo lectura (del modelo) ─────────────────────

        public FileProperties? Properties
        {
            get => _properties;
            private set
            {
                SetProperty(ref _properties, value);
                // Sincronizar campos editables con el modelo recién cargado
                if (value != null)
                {
                    IsReadOnly = value.IsReadOnly;
                    IsHidden = value.IsHidden;
                }
                OnPropertyChanged(nameof(HasProperties));
                OnPropertyChanged(nameof(IsFolder));
                OnPropertyChanged(nameof(DisplayFolderSize));
            }
        }

        public bool HasProperties => _properties != null;
        public bool IsFolder => _properties?.Type == "Carpeta de archivos";

        // ─── Estado de carga ──────────────────────────────────────────────

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public bool IsSaving
        {
            get => _isSaving;
            private set => SetProperty(ref _isSaving, value);
        }

        public bool IsFolderSizeLoading
        {
            get => _isFolderSizeLoading;
            private set
            {
                SetProperty(ref _isFolderSizeLoading, value);
                OnPropertyChanged(nameof(DisplayFolderSize));
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        // ─── Atributos editables ──────────────────────────────────────────

        /// <summary>Atributo Solo Lectura – editable por el usuario.</summary>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        /// <summary>Atributo Oculto – editable por el usuario.</summary>
        public bool IsHidden
        {
            get => _isHidden;
            set => SetProperty(ref _isHidden, value);
        }

        // ─── Tamaño de carpeta (calculado de forma asíncrona) ─────────────

        public long FolderSizeBytes
        {
            get => _folderSizeBytes;
            private set
            {
                SetProperty(ref _folderSizeBytes, value);
                OnPropertyChanged(nameof(DisplayFolderSize));
            }
        }

        /// <summary>
        /// Devuelve el tamaño de la carpeta formateado, o un placeholder mientras carga.
        /// Para archivos usa el tamaño del modelo.
        /// </summary>
        public string DisplayFolderSize
        {
            get
            {
                if (_properties == null) return string.Empty;
                if (!IsFolder) return _properties.DisplaySize;
                if (IsFolderSizeLoading) return "Calculando...";
                if (FolderSizeBytes == 0) return "—";
                return FileManager.Core.Helpers.FileSizeHelper.Format(FolderSizeBytes);
            }
        }

        // ─── Comandos ─────────────────────────────────────────────────────

        /// <summary>Guarda los atributos modificados (ReadOnly, Hidden).</summary>
        public ICommand SaveAttributesCommand =>
            _saveAttributesCommand ??= new RelayCommand(
                async _ => await SaveAttributesAsync(),
                _ => HasProperties && !IsSaving && !IsLoading);

        /// <summary>Cancela el cálculo de tamaño de carpeta en progreso.</summary>
        public ICommand CancelFolderSizeCommand =>
            _cancelFolderSizeCommand ??= new RelayCommand(
                _ =>
                {
                    _folderSizeCts?.Cancel();
                    IsFolderSizeLoading = false;
                },
                _ => IsFolderSizeLoading);

        /// <summary>Recarga las propiedades del elemento actual desde disco.</summary>
        public ICommand RefreshCommand =>
            _refreshCommand ??= new RelayCommand(
                async _ =>
                {
                    if (_properties?.FullPath != null)
                        await LoadAsync(_properties.FullPath);
                },
                _ => HasProperties && !IsLoading);

        // ─── Métodos Públicos ─────────────────────────────────────────────

        /// <summary>
        /// Carga las propiedades del archivo o carpeta especificado.
        /// Si es carpeta, también inicia el cálculo asíncrono de su tamaño.
        /// </summary>
        public async Task LoadAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            // Cancelar cálculo de tamaño previo si existe
            _folderSizeCts?.Cancel();
            _folderSizeCts = null;
            FolderSizeBytes = 0;
            ErrorMessage = null;

            try
            {
                IsLoading = true;
                Properties = await _propertiesService.GetPropertiesAsync(path);

                // Iniciar cálculo de tamaño si es carpeta
                if (IsFolder)
                    await StartFolderSizeCalculationAsync(path);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No se pudieron cargar las propiedades: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[PropertiesViewModel] Error: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Persiste los atributos editados (ReadOnly e Hidden) en disco.
        /// </summary>
        public async Task SaveAttributesAsync()
        {
            if (_properties == null) return;

            try
            {
                IsSaving = true;
                ErrorMessage = null;

                await _propertiesService.SetAttributesAsync(
                    _properties.FullPath, IsReadOnly, IsHidden);

                // Sincronizar el modelo con los nuevos valores
                _properties.IsReadOnly = IsReadOnly;
                _properties.IsHidden = IsHidden;
            }
            catch (UnauthorizedAccessException)
            {
                ErrorMessage = "Acceso denegado al modificar atributos. Ejecute como administrador.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al guardar atributos: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Limpia el estado del ViewModel (útil al cerrar el diálogo de propiedades).
        /// </summary>
        public void Clear()
        {
            _folderSizeCts?.Cancel();
            _folderSizeCts = null;
            Properties = null;
            FolderSizeBytes = 0;
            ErrorMessage = null;
            IsFolderSizeLoading = false;
        }

        // ─── Métodos Privados ─────────────────────────────────────────────

        private async Task StartFolderSizeCalculationAsync(string folderPath)
        {
            _folderSizeCts = new CancellationTokenSource();
            var token = _folderSizeCts.Token;

            IsFolderSizeLoading = true;

            try
            {
                var size = await _propertiesService.GetFolderSizeAsync(folderPath, token);

                if (!token.IsCancellationRequested)
                    FolderSizeBytes = size;
            }
            catch (OperationCanceledException)
            {
                // Cancelado por el usuario — silencioso
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropertiesViewModel] FolderSize error: {ex.Message}");
            }
            finally
            {
                if (!token.IsCancellationRequested)
                    IsFolderSizeLoading = false;
            }
        }
    }
}
