using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Bee.Northwind.UI.Views;

/// <summary>
/// Platform-neutral root content hosting the navigation <see cref="ContentControl"/> bound to
/// <c>CurrentView</c> on <see cref="ViewModels.MainWindowViewModel"/>. The desktop head wraps it
/// in <see cref="MainWindow"/>; the browser head hosts it directly as the single view.
/// </summary>
public partial class MainView : UserControl
{
    private IInsetsManager? _insets;
    private TopLevel? _topLevel;

    /// <summary>Loads the XAML.</summary>
    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Single-view hosts (iOS / Android) report a non-zero safe area around the notch, status
        // bar and home indicator; inset the content by it so it stays clear of those system areas
        // in any orientation. Desktop / browser report an empty safe area (or no insets manager),
        // so this is a no-op there.
        _topLevel = TopLevel.GetTopLevel(this);
        _insets = _topLevel?.InsetsManager;
        if (_insets is not null)
        {
            _insets.SafeAreaChanged += OnSafeAreaChanged;
            Padding = _insets.SafeAreaPadding;
        }

        // Route the platform back request (Android hardware / gesture back; also the browser
        // back button) into the Forms shell so it unwinds record → tab before the platform
        // takes over (which on Android exits the app). Connection / Login have nothing to
        // unwind, so the request falls through unhandled and the platform handles it.
        if (_topLevel is not null)
        {
            _topLevel.BackRequested += OnBackRequested;
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_insets is not null)
        {
            _insets.SafeAreaChanged -= OnSafeAreaChanged;
            _insets = null;
        }

        if (_topLevel is not null)
        {
            _topLevel.BackRequested -= OnBackRequested;
            _topLevel = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
        => Padding = e.SafeAreaPadding;

    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        // The Forms shell is the only step with nested navigation to unwind. When present and it
        // consumes the back request, mark it handled so the platform does not also act on it.
        var forms = this.GetVisualDescendants().OfType<FormsView>().FirstOrDefault();
        if (forms is not null && forms.TryHandleBack())
        {
            e.Handled = true;
        }
    }
}
