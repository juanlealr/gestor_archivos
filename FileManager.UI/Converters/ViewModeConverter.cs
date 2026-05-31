using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileManager.UI.Converters
{
    public class ViewModeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int viewMode && parameter is string targetModeStr && int.TryParse(targetModeStr, out int targetMode))
            {
                return viewMode == targetMode ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
