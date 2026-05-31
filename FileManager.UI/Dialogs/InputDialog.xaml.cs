using System.Windows;

namespace FileManager.UI.Dialogs
{
    public partial class InputDialog : Window
    {
        public string InputValue { get; private set; } = string.Empty;

        public InputDialog(string title, string defaultValue = "")
        {
            InitializeComponent();
            TitleTextBlock.Text = title;
            InputTextBox.Text = defaultValue;
            InputTextBox.SelectAll();
            InputTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputValue = InputTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public static string? Show(string title, string defaultValue = "")
        {
            var dialog = new InputDialog(title, defaultValue);
            if (dialog.ShowDialog() == true)
            {
                return dialog.InputValue;
            }
            return null;
        }
    }
}
