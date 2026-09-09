using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace TokenUsageMonitor.Converters;

/// <summary>
/// Converts an overlay opacity value (1.0 = opaque) into the dark background
/// brush of the floating bar, scaling only the alpha channel so the text stays
/// fully readable while the background becomes transparent.
/// </summary>
public sealed class OpacityToBackgroundConverter : IValueConverter
{
    private static readonly Color BaseColor = Color.FromArgb(0xE6, 0x25, 0x25, 0x25);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double opacity = value is double o ? Math.Clamp(o, 0.0, 1.0) : 1.0;
        byte alpha = (byte)Math.Round(0xE6 * opacity);
        return new SolidColorBrush(Color.FromArgb(alpha, BaseColor.R, BaseColor.G, BaseColor.B));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}