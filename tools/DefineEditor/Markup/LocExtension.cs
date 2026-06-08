using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Bee.DefineEditor.Services;

namespace Bee.DefineEditor.Markup;

/// <summary>
/// XAML markup extension that binds a property to a localized string. Usage:
/// <code>
/// &lt;TextBlock Text="{loc:Loc Menu_File}"/&gt;
/// </code>
/// The binding tracks <see cref="LocalizationService.Current"/>'s indexer so
/// the bound property refreshes automatically when the user switches language.
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocExtension() { }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // Avalonia's CompiledBinding only handles plain property paths, so we
        // use ReflectionBinding (the classic source-string binding) to hit the
        // indexer. Performance is irrelevant for a few hundred UI strings.
        return new Binding($"[{Key}]")
        {
            Source = LocalizationService.Current,
            Mode = BindingMode.OneWay,
        };
    }
}
