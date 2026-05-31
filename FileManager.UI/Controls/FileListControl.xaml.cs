using System.Windows.Controls;
using System.Windows.Input;
using FileManager.Core.Models;

namespace FileManager.UI.Controls
{
    public partial class FileListControl : UserControl
    {
        public FileListControl()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is FileSystemItem item)
            {
                if (item.IsDirectory)
                {
                    // Navegar a la carpeta
                    var command = (DataContext as dynamic)?.FileExplorerViewModel?.OpenFileCommand;
                    if (command != null)
                    {
                        command.Execute(item);
                    }
                }
            }
        }
    }
}
