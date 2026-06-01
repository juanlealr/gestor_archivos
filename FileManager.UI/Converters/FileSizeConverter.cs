using System;
using System.Globalization;
using System.Windows.Data;
using FileManager.Core.Helpers;

namespace FileManager.UI.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long longValue)
                return FileSizeHelper.Format(longValue);

            if (value is int intValue)
                return FileSizeHelper.Format(intValue);

            if (value is double doubleValue)
                return FileSizeHelper.Format((long)doubleValue);

            return "0 B";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
