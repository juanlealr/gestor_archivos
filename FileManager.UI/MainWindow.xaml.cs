using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileManager.Core.Models;
using FileManager.ViewModels;

namespace FileManager.UI
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as MainViewModel;
            if (_viewModel == null)
                return;

            _viewModel.NavigationViewModel.NavigationChanged += NavigationViewModel_NavigationChanged;
            _viewModel.SelectionViewModel.SetSourceItems(_viewModel.FileExplorerViewModel.Items);

            DetailsView.SelectionChanged += OnListSelectionChanged;
            ListViewMode.SelectionChanged += OnListSelectionChanged;

            DetailsView.MouseDoubleClick += OnItemDoubleClick;
            ListViewMode.MouseDoubleClick += OnItemDoubleClick;
        }

        private async void NavigationViewModel_NavigationChanged(object? sender, NavigationChangedEventArgs e)
        {
            if (_viewModel == null)
                return;

            await _viewModel.FileExplorerViewModel.NavigateToAsync(e.Path);
            _viewModel.SelectionViewModel.ClearOnDirectoryChange();
            _viewModel.SelectionViewModel.SetSourceItems(_viewModel.FileExplorerViewModel.Items);
        }

        private void OnListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;

            if (sender is ListView listView)
            {
                var selectedItems = listView.SelectedItems.OfType<FileSystemItem>().ToList();
                _viewModel.SelectionViewModel.SetSelectedItems(selectedItems);
            }
            else if (sender is ListBox listBox)
            {
                var selectedItems = listBox.SelectedItems.OfType<FileSystemItem>().ToList();
                _viewModel.SelectionViewModel.SetSelectedItems(selectedItems);
            }
        }

        private void OnItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel == null) return;
            
            FileSystemItem? item = null;
            
            if (sender is ListView listView && listView.SelectedItem is FileSystemItem lvItem)
            {
                item = lvItem;
            }
            else if (sender is ListBox listBox && listBox.SelectedItem is FileSystemItem lbItem)
            {
                item = lbItem;
            }

            if (item != null)
            {
                if (item.IsDirectory)
                {
                    _viewModel.NavigationViewModel.NavigateTo(item.FullPath);
                }
                else
                {
                    _viewModel.FileExplorerViewModel.OpenFileCommand.Execute(item);
                }
            }
        }

        private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenPropertiesWindowAsync(_viewModel?.FileExplorerViewModel.SelectedItem?.FullPath);
        }

        private async void PropertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.CommandParameter is FileSystemItem fileSystemItem)
            {
                await OpenPropertiesWindowAsync(fileSystemItem.FullPath);
            }
        }

        private async Task OpenPropertiesWindowAsync(string? path)
        {
            if (_viewModel == null || string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(this, "Seleccione un elemento para ver sus propiedades.", "Propiedades", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _viewModel.PropertiesViewModel.Clear();
            await _viewModel.PropertiesViewModel.LoadAsync(path);

            var window = new PropertiesWindow(_viewModel.PropertiesViewModel)
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void MoveToMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "Mover a... aún no está implementado en este menú.", "Mover a...", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BatchCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "Copiar por lotes aún no está disponible desde el menú contextual.", "Copiar elementos", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
