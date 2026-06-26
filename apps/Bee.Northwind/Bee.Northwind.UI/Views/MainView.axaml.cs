using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Markup.Xaml;

namespace Bee.Northwind.UI.Views;

/// <summary>
/// Platform-neutral root content hosting the navigation <see cref="ContentControl"/> bound to
/// <c>CurrentView</c> on <see cref="ViewModels.MainWindowViewModel"/>. The desktop head wraps it
/// in <see cref="MainWindow"/>; the browser head hosts it directly as the single view.
/// </summary>
public partial class MainView : UserControl
{
    private IInsetsManager? _insets;

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
        _insets = TopLevel.GetTopLevel(this)?.InsetsManager;
        if (_insets is not null)
        {
            _insets.SafeAreaChanged += OnSafeAreaChanged;
            Padding = _insets.SafeAreaPadding;
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

        base.OnDetachedFromVisualTree(e);
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
        => Padding = e.SafeAreaPadding;
}
