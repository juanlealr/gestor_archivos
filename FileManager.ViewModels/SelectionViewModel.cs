using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using FileManager.Core.Models;
using FileManager.Core.Services;

namespace FileManager.ViewModels
{
    /// <summary>
    /// ViewModel de selección múltiple y operaciones por lotes.
    /// Gestiona la colección de elementos seleccionados y expone comandos
    /// para ejecutar operaciones (copiar, mover, eliminar, etc.) sobre todos ellos.
    /// </summary>
    public class SelectionViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private readonly IClipboardService _clipboardService;
        private readonly IDialogService _dialogService;
        private readonly ObservableCollection<FileSystemItem> _selectedItems = new();

        private bool _isBatchOperationRunning;
        private int _batchProgress;
        private string _batchStatusMessage = "";
        private CancellationTokenSource? _batchCts;
        private bool _isAllSelected;

        // Commands
        private ICommand? _selectAllCommand;
        private ICommand? _deselectAllCommand;
        private ICommand? _invertSelectionCommand;
        private ICommand? _selectItemCommand;
        private ICommand? _toggleItemCommand;
        private ICommand? _batchCopyCommand;
        private ICommand? _batchMoveToCommand;
        private ICommand? _batchDeleteCommand;
        private ICommand? _batchCopyToClipboardCommand;
        private ICommand? _cancelBatchCommand;

        // ─── Eventos ─────────────────────────────────────────────────────

        /// <summary>Se dispara al finalizar una operación por lotes con su resultado.</summary>
        public event EventHandler<BatchOperationResult>? BatchOperationCompleted;

        public SelectionViewModel(
            IFileService fileService,
            IClipboardService clipboardService,
            IDialogService dialogService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _selectedItems.CollectionChanged += OnSelectedItemsChanged;
        }

        // ─── Propiedades ──────────────────────────────────────────────────

        /// <summary>Colección observable de los elementos actualmente seleccionados.</summary>
        public IReadOnlyCollection<FileSystemItem> SelectedItems => _selectedItems;

        /// <summary>Número de elementos seleccionados.</summary>
        public int SelectionCount => _selectedItems.Count;

        /// <summary>True si hay al menos un elemento seleccionado.</summary>
        public bool HasSelection => _selectedItems.Count > 0;

        /// <summary>True si hay más de un elemento seleccionado.</summary>
        public bool HasMultipleSelection => _selectedItems.Count > 1;

        /// <summary>
        /// True si todos los elementos del directorio actual están seleccionados.
        /// Se sincroniza con la propiedad Items del explorador vía SetSourceItems().
        /// </summary>
        public bool IsAllSelected
        {
            get => _isAllSelected;
            private set => SetProperty(ref _isAllSelected, value);
        }

        /// <summary>Tamaño total de los archivos seleccionados (carpetas cuentan como 0).</summary>
        public long TotalSelectedSize => _selectedItems.Where(i => !i.IsDirectory).Sum(i => i.Size);

        /// <summary>Texto de resumen para la barra de estado.</summary>
        public string SelectionSummary
        {
            get
            {
                if (!HasSelection) return "Ningún elemento seleccionado";
                var files = _selectedItems.Count(i => !i.IsDirectory);
                var dirs = _selectedItems.Count(i => i.IsDirectory);
                var parts = new List<string>();
                if (files > 0) parts.Add($"{files} archivo{(files > 1 ? "s" : "")}");
                if (dirs > 0) parts.Add($"{dirs} carpeta{(dirs > 1 ? "s" : "")}");
                return string.Join(", ", parts) + " seleccionado" + (SelectionCount > 1 ? "s" : "");
            }
        }

        public bool IsBatchOperationRunning
        {
            get => _isBatchOperationRunning;
            private set
            {
                SetProperty(ref _isBatchOperationRunning, value);
                OnPropertyChanged(nameof(CanRunBatchOperation));
            }
        }

        public bool CanRunBatchOperation => HasSelection && !IsBatchOperationRunning;

        public int BatchProgress
        {
            get => _batchProgress;
            private set => SetProperty(ref _batchProgress, value);
        }

        public string BatchStatusMessage
        {
            get => _batchStatusMessage;
            private set => SetProperty(ref _batchStatusMessage, value);
        }

        // ─── Fuente de datos (todos los elementos del directorio actual) ───

        private IList<FileSystemItem>? _sourceItems;

        /// <summary>
        /// Vincula la selección con la colección de elementos visible en el explorador.
        /// Necesario para SelectAll y para calcular IsAllSelected correctamente.
        /// </summary>
        public void SetSourceItems(IList<FileSystemItem> items)
        {
            _sourceItems = items;
            UpdateIsAllSelected();
        }
        /// <summary>
        /// Reemplaza la selección actual con el conjunto de elementos seleccionados en la UI.
        /// </summary>
        public void SetSelectedItems(IEnumerable<FileSystemItem> items)
        {
            _selectedItems.Clear();
            foreach (var item in items)
                _selectedItems.Add(item);
        }
        // ─── Comandos ─────────────────────────────────────────────────────

        public ICommand SelectAllCommand =>
            _selectAllCommand ??= new RelayCommand(
                _ => SelectAll(),
                _ => _sourceItems?.Count > 0 && !IsAllSelected);

        public ICommand DeselectAllCommand =>
            _deselectAllCommand ??= new RelayCommand(
                _ => DeselectAll(),
                _ => HasSelection);

        public ICommand InvertSelectionCommand =>
            _invertSelectionCommand ??= new RelayCommand(
                _ => InvertSelection(),
                _ => _sourceItems?.Count > 0);

        /// <summary>Agrega o quita un único elemento de la selección.</summary>
        public ICommand ToggleItemCommand =>
            _toggleItemCommand ??= new RelayCommand<FileSystemItem?>(item =>
            {
                if (item == null) return;
                if (_selectedItems.Contains(item))
                    _selectedItems.Remove(item);
                else
                    _selectedItems.Add(item);
            });

        /// <summary>Selecciona un elemento de forma exclusiva (reemplaza la selección actual).</summary>
        public ICommand SelectItemCommand =>
            _selectItemCommand ??= new RelayCommand<FileSystemItem?>(item =>
            {
                if (item == null) return;
                _selectedItems.Clear();
                _selectedItems.Add(item);
            });

        /// <summary>
        /// Copia todos los elementos seleccionados a una ruta destino.
        /// Parámetro: ruta destino (string).
        /// </summary>
        public ICommand BatchCopyCommand =>
            _batchCopyCommand ??= new RelayCommand<string>(
                async dest => await ExecuteBatchCopyAsync(dest),
                dest => !string.IsNullOrEmpty(dest) && CanRunBatchOperation);

        /// <summary>
        /// Mueve todos los elementos seleccionados a una ruta destino.
        /// Parámetro: ruta destino (string).
        /// </summary>
        public ICommand BatchMoveToCommand =>
            _batchMoveToCommand ??= new RelayCommand<string>(
                async dest => await ExecuteBatchMoveAsync(dest),
                dest => !string.IsNullOrEmpty(dest) && CanRunBatchOperation);

        /// <summary>
        /// Elimina todos los elementos seleccionados tras pedir confirmación global.
        /// </summary>
        public ICommand BatchDeleteCommand =>
            _batchDeleteCommand ??= new RelayCommand(
                async _ => await ExecuteBatchDeleteAsync(),
                _ => CanRunBatchOperation);

        /// <summary>
        /// Copia las rutas de todos los elementos seleccionados al portapapeles del sistema.
        /// </summary>
        public ICommand BatchCopyToClipboardCommand =>
            _batchCopyToClipboardCommand ??= new RelayCommand(
                _ => CopyPathsToSystemClipboard(),
                _ => HasSelection);

        /// <summary>Cancela la operación por lotes en progreso.</summary>
        public ICommand CancelBatchCommand =>
            _cancelBatchCommand ??= new RelayCommand(
                _ => _batchCts?.Cancel(),
                _ => IsBatchOperationRunning);

        // ─── Métodos de Selección ─────────────────────────────────────────

        public void SelectAll()
        {
            if (_sourceItems == null) return;
            _selectedItems.Clear();
            foreach (var item in _sourceItems)
                _selectedItems.Add(item);
        }

        public void DeselectAll() => _selectedItems.Clear();

        public void InvertSelection()
        {
            if (_sourceItems == null) return;
            var toAdd = _sourceItems.Except(_selectedItems).ToList();
            _selectedItems.Clear();
            foreach (var item in toAdd)
                _selectedItems.Add(item);
        }

        /// <summary>Selección de rango (Shift+Click): agrega todos los elementos entre from y to.</summary>
        public void SelectRange(FileSystemItem from, FileSystemItem to)
        {
            if (_sourceItems == null) return;

            var fromIdx = _sourceItems.IndexOf(from);
            var toIdx = _sourceItems.IndexOf(to);
            if (fromIdx < 0 || toIdx < 0) return;

            if (fromIdx > toIdx) (fromIdx, toIdx) = (toIdx, fromIdx);

            for (int i = fromIdx; i <= toIdx; i++)
            {
                var item = _sourceItems[i];
                if (!_selectedItems.Contains(item))
                    _selectedItems.Add(item);
            }
        }

        /// <summary>Limpia la selección al cambiar de directorio.</summary>
        public void ClearOnDirectoryChange()
        {
            _selectedItems.Clear();
            _sourceItems = null;
        }

        // ─── Operaciones por Lotes ────────────────────────────────────────

        public async Task ExecuteBatchCopyAsync(string destinationFolder)
        {
            if (string.IsNullOrEmpty(destinationFolder) || !HasSelection) return;

            var items = _selectedItems.ToList();
            await RunBatchAsync("Copiando", items, async (item, ct) =>
            {
                var dest = Path.Combine(destinationFolder, item.Name);
                await _fileService.CopyAsync(item.FullPath, dest, overwrite: false);
            });
        }

        public async Task ExecuteBatchMoveAsync(string destinationFolder)
        {
            if (string.IsNullOrEmpty(destinationFolder) || !HasSelection) return;

            var items = _selectedItems.ToList();
            await RunBatchAsync("Moviendo", items, async (item, ct) =>
            {
                var dest = Path.Combine(destinationFolder, item.Name);
                await _fileService.MoveAsync(item.FullPath, dest, overwrite: false);
            });
        }

        public async Task ExecuteBatchDeleteAsync()
        {
            if (!HasSelection) return;

            var count = SelectionCount;
            if (!_dialogService.ShowConfirmDialog(
                    "Eliminar elementos",
                    $"¿Eliminar {count} elemento{(count > 1 ? "s" : "")} de forma permanente?"))
                return;

            var items = _selectedItems.ToList();
            await RunBatchAsync("Eliminando", items, async (item, ct) =>
            {
                await _fileService.DeleteAsync(item.FullPath, recursive: true);
            });
        }

        // ─── Métodos Privados ─────────────────────────────────────────────

        private async Task RunBatchAsync(
            string operationLabel,
            IList<FileSystemItem> items,
            Func<FileSystemItem, CancellationToken, Task> operation)
        {
            _batchCts = new CancellationTokenSource();
            var token = _batchCts.Token;

            IsBatchOperationRunning = true;
            BatchProgress = 0;

            int succeeded = 0, failed = 0;
            var errors = new List<string>();

            for (int i = 0; i < items.Count; i++)
            {
                if (token.IsCancellationRequested) break;

                var item = items[i];
                BatchStatusMessage = $"{operationLabel} {i + 1}/{items.Count}: '{item.Name}'";
                BatchProgress = (int)((i + 1.0) / items.Count * 100);

                try
                {
                    await operation(item, token);
                    succeeded++;
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"'{item.Name}': {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[SelectionViewModel] Batch error on '{item.Name}': {ex.Message}");
                }
            }

            IsBatchOperationRunning = false;
            BatchProgress = 0;
            BatchStatusMessage = $"Completado: {succeeded} exitoso{(succeeded != 1 ? "s" : "")}" +
                                 (failed > 0 ? $", {failed} con error" : "");

            BatchOperationCompleted?.Invoke(this, new BatchOperationResult(succeeded, failed, errors.AsReadOnly()));
        }

        private void CopyPathsToSystemClipboard()
        {
            var paths = string.Join(Environment.NewLine,
                _selectedItems.Select(i => i.FullPath));

            try
            {
                System.Windows.Clipboard.SetText(paths);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SelectionViewModel] Clipboard error: {ex.Message}");
            }
        }

        private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SelectionCount));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(HasMultipleSelection));
            OnPropertyChanged(nameof(TotalSelectedSize));
            OnPropertyChanged(nameof(SelectionSummary));
            OnPropertyChanged(nameof(CanRunBatchOperation));
            UpdateIsAllSelected();
        }

        private void UpdateIsAllSelected()
        {
            IsAllSelected = _sourceItems != null
                && _sourceItems.Count > 0
                && _selectedItems.Count == _sourceItems.Count;
        }
    }

    // ─── Tipos auxiliares ─────────────────────────────────────────────────────

    public record BatchOperationResult(
        int SucceededCount,
        int FailedCount,
        IReadOnlyList<string> Errors)
    {
        public bool HasErrors => FailedCount > 0;
        public bool WasCompleteSuccess => FailedCount == 0 && SucceededCount > 0;
    }
}
