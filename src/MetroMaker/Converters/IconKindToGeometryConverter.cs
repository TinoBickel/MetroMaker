using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MetroMaker.Models;

namespace MetroMaker.Converters;

public class IconEntryToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IconEntry entry && !string.IsNullOrEmpty(entry.PathData))
        {
            try
            {
                return Geometry.Parse(entry.PathData);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
