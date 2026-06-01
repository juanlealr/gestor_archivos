using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace FileManager.ViewModels
{
    /// <summary>
    /// ViewModel de navegación: encapsula todo el historial back/forward,
    /// la ruta actual y el breadcrumb, desacoplándolos del FileExplorerViewModel.
    /// </summary>
    public class NavigationViewModel : ViewModelBase
    {
        private readonly Stack<string> _backHistory = new();
        private readonly Stack<string> _forwardHistory = new();

        private string _currentPath = "";
        private ObservableCollection<BreadcrumbSegment> _breadcrumbs = new();

        private ICommand? _goBackCommand;
        private ICommand? _goForwardCommand;
        private ICommand? _navigateToCommand;
        private ICommand? _navigateUpCommand;
        private ICommand? _navigateToBreadcrumbCommand;

        // ─── Eventos ─────────────────────────────────────────────────────

        /// <summary>
        /// Se dispara cuando la navegación cambia y el explorador debe cargar el nuevo directorio.
        /// </summary>
        public event EventHandler<NavigationChangedEventArgs>? NavigationChanged;

        // ─── Propiedades ──────────────────────────────────────────────────

        public string CurrentPath
        {
            get => _currentPath;
            private set
            {
                if (SetProperty(ref _currentPath, value))
                {
                    RebuildBreadcrumbs(value);
                    OnPropertyChanged(nameof(CanGoBack));
                    OnPropertyChanged(nameof(CanGoForward));
                    OnPropertyChanged(nameof(CanGoUp));
                    OnPropertyChanged(nameof(ParentPath));
                }
            }
        }

        public bool CanGoBack => _backHistory.Count > 0;
        public bool CanGoForward => _forwardHistory.Count > 0;
        public bool CanGoUp => !string.IsNullOrEmpty(ParentPath);

        /// <summary>Ruta del directorio padre, o null si estamos en la raíz.</summary>
        public string? ParentPath
        {
            get
            {
                if (string.IsNullOrEmpty(_currentPath)) return null;
                try { return Path.GetDirectoryName(_currentPath); }
                catch { return null; }
            }
        }

        /// <summary>Segmentos del breadcrumb derivados de CurrentPath.</summary>
        public ObservableCollection<BreadcrumbSegment> Breadcrumbs
        {
            get => _breadcrumbs;
            private set => SetProperty(ref _breadcrumbs, value);
        }

        /// <summary>Número de entradas en el historial de retroceso.</summary>
        public int BackHistoryCount => _backHistory.Count;

        /// <summary>Número de entradas en el historial de avance.</summary>
        public int ForwardHistoryCount => _forwardHistory.Count;

        // ─── Comandos ─────────────────────────────────────────────────────

        public ICommand GoBackCommand =>
            _goBackCommand ??= new RelayCommand(
                _ => GoBack(),
                _ => CanGoBack);

        public ICommand GoForwardCommand =>
            _goForwardCommand ??= new RelayCommand(
                _ => GoForward(),
                _ => CanGoForward);

        public ICommand NavigateToCommand =>
            _navigateToCommand ??= new RelayCommand<string>(path =>
            {
                if (!string.IsNullOrWhiteSpace(path))
                    NavigateTo(path);
            });

        public ICommand NavigateUpCommand =>
            _navigateUpCommand ??= new RelayCommand(
                _ => { if (ParentPath != null) NavigateTo(ParentPath); },
                _ => CanGoUp);

        public ICommand NavigateToBreadcrumbCommand =>
            _navigateToBreadcrumbCommand ??= new RelayCommand<string>(path =>
            {
                if (!string.IsNullOrWhiteSpace(path) && path != CurrentPath)
                    NavigateTo(path);
            });

        // ─── Métodos Públicos ─────────────────────────────────────────────

        /// <summary>
        /// Navega a la ruta indicada, guardando la ruta actual en el historial de retroceso
        /// y limpiando el de avance.
        /// </summary>
        public void NavigateTo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            if (!string.IsNullOrEmpty(_currentPath))
                _backHistory.Push(_currentPath);

            _forwardHistory.Clear();
            CurrentPath = path;
            FireNavigationChanged(path, NavigationTrigger.UserNavigation);
        }

        /// <summary>
        /// Retrocede al directorio anterior sin modificar el historial de retroceso externo.
        /// Llamado internamente desde GoBackCommand.
        /// </summary>
        public void GoBack()
        {
            if (_backHistory.Count == 0) return;

            if (!string.IsNullOrEmpty(_currentPath))
                _forwardHistory.Push(_currentPath);

            var previous = _backHistory.Pop();
            CurrentPath = previous;
            FireNavigationChanged(previous, NavigationTrigger.Back);
        }

        /// <summary>
        /// Avanza al directorio siguiente en el historial.
        /// </summary>
        public void GoForward()
        {
            if (_forwardHistory.Count == 0) return;

            if (!string.IsNullOrEmpty(_currentPath))
                _backHistory.Push(_currentPath);

            var next = _forwardHistory.Pop();
            CurrentPath = next;
            FireNavigationChanged(next, NavigationTrigger.Forward);
        }

        /// <summary>
        /// Establece la ruta inicial sin emitir entrada en el historial.
        /// Útil para el arranque de la aplicación.
        /// </summary>
        public void Initialize(string initialPath)
        {
            _backHistory.Clear();
            _forwardHistory.Clear();
            CurrentPath = initialPath;
            FireNavigationChanged(initialPath, NavigationTrigger.Initialization);
        }

        /// <summary>Limpia por completo el historial de navegación.</summary>
        public void ClearHistory()
        {
            _backHistory.Clear();
            _forwardHistory.Clear();
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
        }

        /// <summary>Devuelve una copia de las entradas del historial de retroceso (más reciente primero).</summary>
        public IReadOnlyList<string> GetBackHistory() => _backHistory.ToList().AsReadOnly();

        /// <summary>Devuelve una copia de las entradas del historial de avance (más reciente primero).</summary>
        public IReadOnlyList<string> GetForwardHistory() => _forwardHistory.ToList().AsReadOnly();

        // ─── Métodos Privados ─────────────────────────────────────────────

        private void RebuildBreadcrumbs(string path)
        {
            var segments = new ObservableCollection<BreadcrumbSegment>();
            if (string.IsNullOrEmpty(path))
            {
                Breadcrumbs = segments;
                return;
            }

            var parts = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                                   StringSplitOptions.RemoveEmptyEntries);

            string accumulated = "";
            foreach (var part in parts)
            {
                // Para rutas Windows como "C:" necesitamos restaurar la barra final
                accumulated = accumulated == ""
                    ? part + Path.DirectorySeparatorChar
                    : Path.Combine(accumulated, part);

                segments.Add(new BreadcrumbSegment
                {
                    DisplayName = part,
                    FullPath = accumulated,
                    IsLast = false   // se fija a continuación
                });
            }

            if (segments.Count > 0)
                segments[^1] = segments[^1] with { IsLast = true };

            Breadcrumbs = segments;
        }

        private void FireNavigationChanged(string path, NavigationTrigger trigger)
        {
            NavigationChanged?.Invoke(this, new NavigationChangedEventArgs(path, trigger));
            OnPropertyChanged(nameof(BackHistoryCount));
            OnPropertyChanged(nameof(ForwardHistoryCount));
        }
    }

    // ─── Tipos auxiliares ─────────────────────────────────────────────────────

    /// <summary>Un segmento del breadcrumb de navegación.</summary>
    public record BreadcrumbSegment
    {
        public string DisplayName { get; init; } = "";
        public string FullPath { get; init; } = "";
        public bool IsLast { get; init; }
    }

    /// <summary>Indica qué acción desencadenó el cambio de ruta.</summary>
    public enum NavigationTrigger
    {
        Initialization,
        UserNavigation,
        Back,
        Forward
    }

    /// <summary>Argumentos del evento NavigationChanged.</summary>
    public class NavigationChangedEventArgs : EventArgs
    {
        public string Path { get; }
        public NavigationTrigger Trigger { get; }

        public NavigationChangedEventArgs(string path, NavigationTrigger trigger)
        {
            Path = path;
            Trigger = trigger;
        }
    }
}
