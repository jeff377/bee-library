using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Bee.DefineEditor.Converters;

/// <summary>
/// Maps a string resource key (e.g. <c>"DefFormSchema"</c>) to the
/// <see cref="StreamGeometry"/> registered under that key in the
/// application-level resource dictionary. Used by the solution tree and the
/// tab header to render <see cref="Avalonia.Controls.PathIcon"/>s driven by a
/// plain string property on the VM.
/// </summary>
public sealed class IconKeyToGeometryConverter : IValueConverter
{
    public static IconKeyToGeometryConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key || string.IsNullOrEmpty(key)) return null;
        var app = Application.Current;
        if (app is null) return null;
        return app.TryGetResource(key, app.ActualThemeVariant, out var resource)
            ? resource as Geometry
            : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
