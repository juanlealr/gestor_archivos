using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FileManager.UI.Controls;
using FileManager.ViewModels;

namespace FileManager.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private BreadcrumbBar? _breadcrumbBar;
    private TreeViewControl? _treeViewControl;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Obtener referencias a los controles
        _breadcrumbBar = this.FindName("BreadcrumbBar") as BreadcrumbBar;
        
        // Encontrar el TreeViewControl y FileListControl en la jerarquía visual
        _treeViewControl = FindVisualChild<TreeViewControl>(this);

        // Suscribirse a cambios en CurrentPath para actualizar el breadcrumb
        if (DataContext is MainViewModel mainViewModel && mainViewModel.FileExplorerViewModel != null)
        {
            var fileExplorerVm = mainViewModel.FileExplorerViewModel;
            
            // Inicializar breadcrumb con ruta actual
            if (!string.IsNullOrEmpty(fileExplorerVm.CurrentPath) && _breadcrumbBar != null)
            {
                _breadcrumbBar.UpdateBreadcrumb(fileExplorerVm.CurrentPath);
            }

            // Suscribirse a cambios de propiedad
            fileExplorerVm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(FileExplorerViewModel.CurrentPath) && _breadcrumbBar != null)
                {
                    _breadcrumbBar.UpdateBreadcrumb(fileExplorerVm.CurrentPath);
                }
            };

            // Inicializar árbol de carpetas
            if (_treeViewControl != null)
            {
                _treeViewControl.BuildTree();
            }

            // Cargar unidades al abrir
            _ = fileExplorerVm.LoadDrivesAsync();
        }
    }

    /// <summary>
    /// Encuentra un hijo visual de un tipo específico en el árbol visual.
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}