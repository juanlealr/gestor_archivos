using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using FileManager.Core.Models;
using FileManager.Core.Services;

namespace FileManager.ViewModels
{
    /// <summary>
    /// ViewModel de operaciones de archivos: Copy, Move, NewFolder, Rename y Delete.
    /// Funciona como capa coordinadora entre la UI y el IFileService,
    /// notificando el resultado vía OperationCompleted y OperationFailed.
    /// </summary>
    public class FileOperationsViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private readonly IClipboardService _clipboardService;
        private readonly IDialogService _dialogService;

        // ─── Backing fields ──────────────────────────────────────────────
        private bool _isBusy;
        private string? _statusMessage;
        private OperationProgress? _currentProgress;

        private ICommand? _copyCommand;
        private ICommand? _moveCommand;
        private ICommand? _newFolderCommand;
        private ICommand? _renameCommand;
        private ICommand? _deleteCommand;
        private ICommand? _copyToCommand;
        private ICommand? _moveToCommand;

        // ─── Eventos ─────────────────────────────────────────────────────

        /// <summary>Se dispara cuando una operación finaliza correctamente.</summary>
        public event EventHandler<FileOperationResult>? OperationCompleted;

        /// <summary>Se dispara cuando una operación falla.</summary>
        public event EventHandler<FileOperationError>? OperationFailed;

        public FileOperationsViewModel(
            IFileService fileService,
            IClipboardService clipboardService,
            IDialogService dialogService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        // ─── Propiedades de estado ────────────────────────────────────────

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public OperationProgress? CurrentProgress
        {
            get => _currentProgress;
            private set => SetProperty(ref _currentProgress, value);
        }

        // ─── Comandos ─────────────────────────────────────────────────────

        /// <summary>
        /// Copia un elemento al portapapeles interno (sin mover en disco todavía).
        /// El pegado efectivo ocurre con PasteCommand en FileExplorerViewModel.
        /// </summary>
        public ICommand CopyCommand =>
            _copyCommand ??= new RelayCommand<FileSystemItem?>(item =>
            {
                if (item == null) return;
                _clipboardService.CopyPath(item.FullPath);
                SetStatus($"'{item.Name}' copiado al portapapeles.");
            });

        /// <summary>
        /// Marca un elemento para mover (cut) al portapapeles interno.
        /// </summary>
        public ICommand MoveCommand =>
            _moveCommand ??= new RelayCommand<FileSystemItem?>(item =>
            {
                if (item == null) return;
                _clipboardService.CutPath(item.FullPath);
                SetStatus($"'{item.Name}' marcado para mover.");
            });

        /// <summary>
        /// Copia físicamente un elemento a la ruta destino indicada.
        /// Parámetro: CopyOperationArgs { Source, Destination, Overwrite }
        /// </summary>
        public ICommand CopyToCommand =>
            _copyToCommand ??= new RelayCommand<CopyMoveArgs?>(
                async args => await ExecuteCopyAsync(args!),
                args => args != null && !IsBusy);

        /// <summary>
        /// Mueve físicamente un elemento a la ruta destino indicada.
        /// Parámetro: CopyMoveArgs { Source, Destination, Overwrite }
        /// </summary>
        public ICommand MoveToCommand =>
            _moveToCommand ??= new RelayCommand<CopyMoveArgs?>(
                async args => await ExecuteMoveAsync(args!),
                args => args != null && !IsBusy);

        /// <summary>
        /// Crea una nueva carpeta en el directorio actual.
        /// Muestra un diálogo para ingresar el nombre.
        /// Parámetro: ruta del directorio padre (string).
        /// </summary>
        public ICommand NewFolderCommand =>
            _newFolderCommand ??= new RelayCommand<string>(
                async path => await ExecuteNewFolderAsync(path),
                path => !string.IsNullOrEmpty(path) && !IsBusy);

        /// <summary>
        /// Renombra el elemento seleccionado.
        /// Muestra un diálogo para ingresar el nuevo nombre.
        /// Parámetro: FileSystemItem a renombrar.
        /// </summary>
        public ICommand RenameCommand =>
            _renameCommand ??= new RelayCommand<FileSystemItem?>(
                async item => await ExecuteRenameAsync(item!),
                item => item != null && !IsBusy);

        /// <summary>
        /// Elimina el elemento seleccionado tras pedir confirmación.
        /// Parámetro: FileSystemItem a eliminar.
        /// </summary>
        public ICommand DeleteCommand =>
            _deleteCommand ??= new RelayCommand<FileSystemItem?>(
                async item => await ExecuteDeleteAsync(item!),
                item => item != null && !IsBusy);

        // ─── Métodos Públicos de operación directa ────────────────────────

        public async Task ExecuteCopyAsync(CopyMoveArgs args)
        {
            if (args == null) return;
            await RunOperationAsync(
                async () =>
                {
                    SetStatus($"Copiando '{Path.GetFileName(args.SourcePath)}'...");
                    await _fileService.CopyAsync(args.SourcePath, args.DestinationPath, args.Overwrite);
                    SetStatus($"'{Path.GetFileName(args.SourcePath)}' copiado.");
                },
                new FileOperationResult(FileOperationKind.Copy, args.SourcePath, args.DestinationPath));
        }

        public async Task ExecuteMoveAsync(CopyMoveArgs args)
        {
            if (args == null) return;
            await RunOperationAsync(
                async () =>
                {
                    SetStatus($"Moviendo '{Path.GetFileName(args.SourcePath)}'...");
                    await _fileService.MoveAsync(args.SourcePath, args.DestinationPath, args.Overwrite);
                    SetStatus($"'{Path.GetFileName(args.SourcePath)}' movido.");
                },
                new FileOperationResult(FileOperationKind.Move, args.SourcePath, args.DestinationPath));
        }

        public async Task ExecuteNewFolderAsync(string parentPath)
        {
            if (string.IsNullOrEmpty(parentPath)) return;

            var name = _dialogService.ShowInputDialog("Nueva Carpeta", "Nueva carpeta");
            if (string.IsNullOrEmpty(name)) return;

            await RunOperationAsync(
                async () =>
                {
                    await _fileService.CreateFolderAsync(parentPath, name);
                    SetStatus($"Carpeta '{name}' creada.");
                },
                new FileOperationResult(FileOperationKind.NewFolder, parentPath, Path.Combine(parentPath, name)));
        }

        public async Task ExecuteRenameAsync(FileSystemItem item)
        {
            if (item == null) return;

            var currentName = Path.GetFileName(item.FullPath);
            var newName = _dialogService.ShowInputDialog("Renombrar", currentName);
            if (string.IsNullOrEmpty(newName) || newName == currentName) return;

            await RunOperationAsync(
                async () =>
                {
                    await _fileService.RenameAsync(item.FullPath, newName);
                    SetStatus($"Renombrado a '{newName}'.");
                },
                new FileOperationResult(FileOperationKind.Rename, item.FullPath,
                    Path.Combine(Path.GetDirectoryName(item.FullPath) ?? "", newName)));
        }

        public async Task ExecuteDeleteAsync(FileSystemItem item)
        {
            if (item == null) return;

            if (!_dialogService.ShowConfirmDialog(
                    "Confirmar eliminación",
                    $"¿Desea eliminar '{item.Name}' de forma permanente?"))
                return;

            await RunOperationAsync(
                async () =>
                {
                    await _fileService.DeleteAsync(item.FullPath, recursive: true);
                    SetStatus($"'{item.Name}' eliminado.");
                },
                new FileOperationResult(FileOperationKind.Delete, item.FullPath, null));
        }

        // ─── Métodos Privados ─────────────────────────────────────────────

        private async Task RunOperationAsync(Func<Task> operation, FileOperationResult successResult)
        {
            try
            {
                IsBusy = true;
                await operation();
                OperationCompleted?.Invoke(this, successResult);
            }
            catch (UnauthorizedAccessException ex)
            {
                var err = new FileOperationError(successResult.Kind, successResult.SourcePath, $"Acceso denegado: {ex.Message}");
                OperationFailed?.Invoke(this, err);
                SetStatus($"Error: acceso denegado.");
                System.Diagnostics.Debug.WriteLine($"[FileOperationsViewModel] {err}");
            }
            catch (IOException ex)
            {
                var err = new FileOperationError(successResult.Kind, successResult.SourcePath, ex.Message);
                OperationFailed?.Invoke(this, err);
                SetStatus($"Error de I/O: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[FileOperationsViewModel] {err}");
            }
            catch (Exception ex)
            {
                var err = new FileOperationError(successResult.Kind, successResult.SourcePath, ex.Message);
                OperationFailed?.Invoke(this, err);
                SetStatus($"Error inesperado.");
                System.Diagnostics.Debug.WriteLine($"[FileOperationsViewModel] {err}");
            }
            finally
            {
                IsBusy = false;
                CurrentProgress = null;
            }
        }

        private void SetStatus(string message)
        {
            StatusMessage = message;
            System.Diagnostics.Debug.WriteLine($"[FileOperationsViewModel] {message}");
        }
    }

    // ─── Tipos auxiliares ─────────────────────────────────────────────────────

    public record CopyMoveArgs(string SourcePath, string DestinationPath, bool Overwrite = false);

    public enum FileOperationKind { Copy, Move, NewFolder, Rename, Delete }

    public record FileOperationResult(FileOperationKind Kind, string SourcePath, string? DestinationPath);

    public record FileOperationError(FileOperationKind Kind, string SourcePath, string ErrorMessage);

    public class OperationProgress : ViewModelBase
    {
        private int _percentage;
        private string _description = "";

        public int Percentage
        {
            get => _percentage;
            set => SetProperty(ref _percentage, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }
    }
}
