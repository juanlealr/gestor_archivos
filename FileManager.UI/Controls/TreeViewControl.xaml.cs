using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FileManager.ViewModels;

namespace FileManager.UI.Controls
{
    public class TreeNode
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }
        public ObservableCollection<TreeNode> Children { get; set; } = new();
        public bool IsLoaded { get; set; } = false;
    }

    public partial class TreeViewControl : UserControl
    {
        public ObservableCollection<TreeNode> TreeNodes { get; }

        public TreeViewControl()
        {
            TreeNodes = new ObservableCollection<TreeNode>();
            InitializeComponent();
            DataContext = this;
            Loaded += TreeViewControl_Loaded;
        }

        private void TreeViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Cargar solo las unidades al inicio (rápido)
            BuildTree();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNode node && Parent is UIElement parentElement)
            {
                // Buscar el MainViewModel en el árbol de visualización
                var window = Window.GetWindow(this);
                if (window?.DataContext is MainViewModel mainViewModel && mainViewModel.FileExplorerViewModel != null)
                {
                    // Usar NavigateToPathCommand para cambiar de carpeta
                    if (mainViewModel.FileExplorerViewModel.NavigateToPathCommand is RelayCommand<string> cmd)
                    {
                        cmd.Execute(node.FullPath);
                    }
                }
            }
        }

        private void TreeView_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.Source is TreeViewItem item && item.DataContext is TreeNode node)
            {
                // Cargar hijos bajo demanda cuando se expande el nodo
                if (!node.IsLoaded)
                {
                    LoadChildrenAsync(node);
                }
            }
        }

        public void BuildTree()
        {
            TreeNodes.Clear();

            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    try
                    {
                        var driveNode = new TreeNode
                        {
                            Name = drive.Name.TrimEnd('\\'),
                            FullPath = drive.Name,
                            IsLoaded = false
                        };

                        TreeNodes.Add(driveNode);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void LoadChildrenAsync(TreeNode parentNode)
        {
            Task.Run(() =>
            {
                try
                {
                    var children = new List<TreeNode>();
                    
                    try
                    {
                        var subDirectories = Directory.GetDirectories(parentNode.FullPath);
                        
                        foreach (var subDir in subDirectories)
                        {
                            try
                            {
                                var dirInfo = new DirectoryInfo(subDir);
                                var childNode = new TreeNode
                                {
                                    Name = dirInfo.Name,
                                    FullPath = dirInfo.FullName,
                                    IsLoaded = false
                                };

                                // Verificar si tiene subdirectorios
                                try
                                {
                                    if (Directory.GetDirectories(subDir).Length > 0)
                                    {
                                        // Agregar un nodo dummy solo si hay subdirectorios
                                        childNode.Children.Add(new TreeNode { Name = "...", FullPath = "" });
                                    }
                                }
                                catch { }

                                children.Add(childNode);
                            }
                            catch { }
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException) { }

                    // Actualizar en el UI thread
                    Dispatcher.Invoke(() =>
                    {
                        parentNode.Children.Clear();
                        foreach (var child in children)
                        {
                            parentNode.Children.Add(child);
                        }
                        parentNode.IsLoaded = true;
                    });
                }
                catch { }
            });
        }
    }
}
