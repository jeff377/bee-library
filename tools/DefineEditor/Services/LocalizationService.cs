using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Bee.DefineEditor.Services;

/// <summary>
/// Singleton localization service. UI bindings go through this so language
/// changes propagate without rebuilding views.
/// </summary>
/// <remarks>
/// <para>
/// Backed by a <see cref="ResourceManager"/> over <c>Bee.DefineEditor.Resources.Strings</c>
/// (the neutral <c>Strings.resx</c> and per-culture <c>Strings.zh-TW.resx</c>).
/// English is the default — when <see cref="Culture"/> is set to any culture
/// whose entry is missing, ResourceManager falls back to the neutral value.
/// </para>
/// <para>
/// The indexer <c>this[string key]</c> is bound from XAML through
/// <see cref="Markup.LocExtension"/>. When <see cref="Culture"/> changes we
/// raise <c>PropertyChanged</c> with the special <c>"Item[]"</c> name so every
/// indexer binding re-fetches and the UI live-updates without app restart.
/// </para>
/// </remarks>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly LocalizationService _current = new();

    public static LocalizationService Current => _current;

    private readonly ResourceManager _resources;
    private CultureInfo _culture;

    private LocalizationService()
    {
        // Match the resource file's base name. The Bee.DefineEditor.csproj
        // embeds Resources/Strings.resx and Strings.zh-TW.resx; ResourceManager
        // wires them together by this name.
        _resources = new ResourceManager(
            "Bee.DefineEditor.Resources.Strings",
            typeof(LocalizationService).Assembly);
        _culture = CultureInfo.GetCultureInfo("en");
    }

    public CultureInfo Culture
    {
        get => _culture;
        set
        {
            if (value is null) return;
            if (string.Equals(_culture.Name, value.Name, System.StringComparison.OrdinalIgnoreCase))
                return;
            _culture = value;
            // Avalonia / WPF / WinUI all listen for "Item[]" to refresh every
            // indexer binding (equivalent to PropertyChangedEventArgs.IndexerName).
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            CultureChanged?.Invoke(this, value);
        }
    }

    /// <summary>
    /// Looks up <paramref name="key"/> for the current culture. Returns the
    /// neutral (English) value when no localized entry exists, and the key
    /// itself when the key is missing from every resx — so missing keys show
    /// up loudly in the UI instead of falling silent.
    /// </summary>
    public string this[string key]
    {
        get
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            return _resources.GetString(key, _culture) ?? key;
        }
    }

    /// <summary>
    /// Fired after <see cref="Culture"/> is set to a new value. Subscribers
    /// that hold cached strings (e.g. <c>StatusText</c>) can refresh manually
    /// — indexer-bound XAML refreshes automatically via PropertyChanged.
    /// </summary>
    public event System.EventHandler<CultureInfo>? CultureChanged;

    public event PropertyChangedEventHandler? PropertyChanged;
}
