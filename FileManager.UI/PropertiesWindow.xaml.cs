using System;
using System.Windows;
using FileManager.ViewModels;

namespace FileManager.UI
{
    public partial class PropertiesWindow : Window
    {
        public PropertiesWindow(PropertiesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is PropertiesViewModel viewModel)
            {
                viewModel.Clear();
            }

            base.OnClosed(e);
        }
    }
}
