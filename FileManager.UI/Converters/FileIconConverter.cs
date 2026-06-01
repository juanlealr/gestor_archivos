using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using FileManager.Core.Helpers;
using FileManager.Core.Models;

namespace FileManager.UI.Converters
{
    public class FileIconConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> IconGlyphs = new(StringComparer.OrdinalIgnoreCase)
        {
            { "folder", "\uE8B7" },
            { "folder_zip", "\uE8E5" },
            { "image", "\uE8B9" },
            { "picture_as_pdf", "\uE8A5" },
            { "description", "\uE8A5" },
            { "table_chart", "\uE9D2" },
            { "slideshow", "\uE8C8" },
            { "article", "\uE8A5" },
            { "code", "\uE943" },
            { "html", "\uE776" },
            { "css", "\uE774" },
            { "data_object", "\uED37" },
            { "music_note", "\uE189" },
            { "movie", "\uE8D7" },
            { "terminal", "\uE756" },
            { "link", "\uE71B" },
            { "insert_drive_file", "\uE8A5" },
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is FileSystemItem item)
            {
                var iconName = IconHelper.GetIcon(item);
                return IconGlyphs.TryGetValue(iconName, out var glyph) ? glyph : "\uE8A5";
            }

            return "\uE8A5";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
