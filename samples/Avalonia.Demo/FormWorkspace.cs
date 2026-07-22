using Avalonia.Controls;
using Bee.UI.Avalonia.Views;

namespace Avalonia.Demo
{
    /// <summary>
    /// Hosts the <see cref="ListView"/> browse surface for a <see cref="ProgId"/> and swaps in
    /// a <see cref="FormView"/> for the selected record, switching back to the list when the
    /// record is saved or dismissed. Mirrors the host-side list/record orchestration the
    /// Bee.Northwind app uses; exposed with a <see cref="ProgId"/> styled property so it can be
    /// dropped straight into XAML.
    /// </summary>
    public sealed class FormWorkspace : UserControl
    {
        /// <summary>Identifies the <see cref="ProgId"/> styled property.</summary>
        public static readonly StyledProperty<string> ProgIdProperty =
            AvaloniaProperty.Register<FormWorkspace, string>(nameof(ProgId), string.Empty);

        private readonly ContentControl _host = new();
        private ListView? _list;

        /// <summary>Initializes a new instance of <see cref="FormWorkspace"/>.</summary>
        public FormWorkspace()
        {
            Content = _host;
        }

        /// <summary>Gets or sets the form program id this workspace browses / edits.</summary>
        public string ProgId
        {
            get => GetValue(ProgIdProperty);
            set => SetValue(ProgIdProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (_list is null && !string.IsNullOrEmpty(ProgId))
                ShowList();
        }

        private void ShowList()
        {
            _list = new ListView { ProgId = ProgId };
            _list.ViewRequested += (_, id) => ShowRecord(record => record.ViewAsync(id));
            _list.EditRequested += (_, id) => ShowRecord(record => record.EditAsync(id));
            _list.AddRequested += (_, _) => ShowRecord(record => record.NewAsync());
            _host.Content = _list;
        }

        private void ShowRecord(Func<FormView, Task> start)
        {
            var record = new FormView { ProgId = ProgId };
            record.Saved += (_, _) => ReturnToList(reload: true);
            record.Closed += (_, _) => ReturnToList(reload: false);
            _host.Content = record;
            _ = start(record);
        }

        private void ReturnToList(bool reload)
        {
            _host.Content = _list;
            if (reload && _list is not null)
                _ = _list.ReloadAsync();
        }
    }
}
