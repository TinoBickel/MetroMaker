using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Material.Icons;

namespace MetroMaker.Converters;

public class IconKindToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MaterialIconKind kind)
        {
            var data = MaterialIconDataProvider.GetData(kind);
            return Geometry.Parse(data);
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
