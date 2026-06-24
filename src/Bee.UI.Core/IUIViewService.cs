namespace Bee.UI.Core
{
    /// <summary>
    /// View services provided by the host UI framework (WinForms / WPF / Avalonia, etc.).
    /// </summary>
    public interface IUIViewService
    {
        /// <summary>
        /// Shows the API connection settings dialog.
        /// </summary>
        /// <returns>True when the user has completed the connection setup; otherwise, false.</returns>
        Task<bool> ShowApiConnectAsync();
    }
}
