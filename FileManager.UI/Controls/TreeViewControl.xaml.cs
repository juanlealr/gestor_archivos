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
    }

    public partial class TreeViewControl : UserControl
    {
        public ObservableCollection<TreeNode> TreeNodes { get; }

        public TreeViewControl()
        {
            TreeNodes = new ObservableCollection<TreeNode>();
            InitializeComponent();
            DataContext = this;
            Loaded += (s, e) => BuildTree(new List<string>());
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

        public void BuildTree(List<string> folderPaths)
        {
            TreeNodes.Clear();

            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    var driveNode = new TreeNode
                    {
                        Name = drive.Name.TrimEnd('\\'),
                        FullPath = drive.Name
                    };

                    PopulateFolder(driveNode, drive.Name);
                    TreeNodes.Add(driveNode);
                }
            }
            catch { }
        }

        private void PopulateFolder(TreeNode parentNode, string folderPath)
        {
            try
            {
                var subDirectories = Directory.GetDirectories(folderPath);
                foreach (var subDir in subDirectories)
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(subDir);
                        var childNode = new TreeNode
                        {
                            Name = dirInfo.Name,
                            FullPath = dirInfo.FullName
                        };

                        // Solo expandir si existen subdirectorios
                        if (Directory.GetDirectories(subDir).Length > 0)
                        {
                            PopulateFolder(childNode, subDir);
                        }

                        parentNode.Children.Add(childNode);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
