using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Bee.DefineEditor.ViewModels;

namespace Bee.DefineEditor.Behaviors;

/// <summary>
/// Attached behavior that marks the hosting document dirty whenever the user
/// edits a control inside the property panel. The editor panes two-way bind
/// straight into Bee.Definition POCOs, which carry no
/// <c>INotifyPropertyChanged</c> — so the view-model never hears about plain
/// property edits. Attaching <c>DirtyTracking.IsEnabled="True"</c> to the
/// panel's container listens for the bubbled user-input events (TextBox text,
/// ComboBox selection, CheckBox toggle) and flips
/// <see cref="DocumentViewModelBase.IsDirty"/> on the container's DataContext.
/// </summary>
public static class DirtyTracking
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(DirtyTracking));

    /// <summary>
    /// True while the container's content is being swapped (tree-node selection
    /// change re-materialises the DataTemplate, which fires the very same
    /// TextChanged / SelectionChanged events during initial binding). Edits
    /// observed in that window are not user input and must not mark dirty.
    /// </summary>
    private static readonly AttachedProperty<bool> s_isSuppressedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsSuppressed", typeof(DirtyTracking));

    static DirtyTracking()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.GetNewValue<bool>())
        {
            // handledEventsToo so edits inside composite controls (e.g. the
            // TextBox embedded in a ComboBox) still reach us if an inner
            // handler marked the event handled.
            control.AddHandler(TextBox.TextChangedEvent, OnUserEdit, RoutingStrategies.Bubble, handledEventsToo: true);
            control.AddHandler(SelectingItemsControl.SelectionChangedEvent, OnUserEdit, RoutingStrategies.Bubble, handledEventsToo: true);
            control.AddHandler(ToggleButton.IsCheckedChangedEvent, OnUserEdit, RoutingStrategies.Bubble, handledEventsToo: true);
            control.PropertyChanged += OnHostPropertyChanged;

            // The very first content assignment may have happened before this
            // behavior attached; start suppressed and release once the initial
            // bindings have settled.
            Suppress(control);
        }
        else
        {
            control.RemoveHandler(TextBox.TextChangedEvent, OnUserEdit);
            control.RemoveHandler(SelectingItemsControl.SelectionChangedEvent, OnUserEdit);
            control.RemoveHandler(ToggleButton.IsCheckedChangedEvent, OnUserEdit);
            control.PropertyChanged -= OnHostPropertyChanged;
        }
    }

    private static void OnHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentControl.ContentProperty && sender is Control control)
            Suppress(control);
    }

    /// <summary>
    /// Opens a suppression window covering the content swap. Background
    /// priority runs after the pending layout pass, which is when the new
    /// DataTemplate materialises and its bindings push initial values into
    /// the editor controls.
    /// </summary>
    private static void Suppress(Control control)
    {
        control.SetValue(s_isSuppressedProperty, true);
        Dispatcher.UIThread.Post(
            () => control.SetValue(s_isSuppressedProperty, false),
            DispatcherPriority.Background);
    }

    private static void OnUserEdit(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.GetValue(s_isSuppressedProperty)) return;
        if (control.DataContext is DocumentViewModelBase doc)
            doc.IsDirty = true;
    }
}
