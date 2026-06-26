using Avalonia.Controls;
using Bee.UI.Avalonia.Views;

namespace Bee.Northwind.UI.Controls;

/// <summary>
/// One form's workspace: hosts the <see cref="ListView"/> browse surface and swaps in a
/// <see cref="FormView"/> for the selected record, switching back to the list when the record
/// is saved or dismissed. This is the host-side orchestration of the ERP list/record flow —
/// the framework ships <see cref="ListView"/> and <see cref="FormView"/> as independent
/// controls and leaves the switching to the host.
/// </summary>
public sealed class FormWorkspace : UserControl
{
    private readonly string _progId;
    private readonly ListView _list;
    private readonly ContentControl _host;

    /// <summary>
    /// Initializes a new workspace for <paramref name="progId"/>, starting on the list.
    /// </summary>
    public FormWorkspace(string progId)
    {
        _progId = progId;

        _list = new ListView { ProgId = progId };
        _list.ViewRequested += (_, id) => ShowRecord(record => record.ViewAsync(id));
        _list.EditRequested += (_, id) => ShowRecord(record => record.EditAsync(id));
        _list.AddRequested += (_, _) => ShowRecord(record => record.NewAsync());

        _host = new ContentControl { Content = _list };
        Content = _host;
    }

    /// <summary>
    /// Attempts to unwind one navigation level in response to a platform back request (the
    /// Android hardware / gesture back button, routed via <c>TopLevel.BackRequested</c>). If a
    /// record is open it returns to the list — mirroring the on-screen Back button — and reports
    /// that the request was consumed.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a record was open and the workspace returned to the list;
    /// <see langword="false"/> if the list was already showing, leaving the caller to decide the
    /// next back action (close the tab, exit the app, …).
    /// </returns>
    public bool TryGoBack()
    {
        if (_host.Content is FormView)
        {
            ReturnToList(reload: false);
            return true;
        }

        return false;
    }

    private void ShowRecord(Func<FormView, Task> start)
    {
        var record = new FormView { ProgId = _progId };
        record.Saved += (_, _) => ReturnToList(reload: true);
        record.Closed += (_, _) => ReturnToList(reload: false);

        _host.Content = record;
        _ = start(record);
    }

    private void ReturnToList(bool reload)
    {
        _host.Content = _list;
        if (reload)
            _ = _list.ReloadAsync();
    }
}
