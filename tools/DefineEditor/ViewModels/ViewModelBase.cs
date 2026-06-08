using System.Globalization;
using Bee.DefineEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bee.DefineEditor.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Shorthand for looking up a localized string. Every view-model derives
    /// from this base, so they all write <c>L("Status_Saved", filename)</c>
    /// instead of repeating the namespace-qualified service indexer.
    /// </summary>
    protected static string L(string key) => LocalizationService.Current[key];

    /// <summary>
    /// Same as <see cref="L(string)"/> but with positional args formatted via
    /// <c>string.Format(InvariantCulture, …)</c> so number formatting stays
    /// stable across locales.
    /// </summary>
    protected static string L(string key, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, LocalizationService.Current[key], args);
}
