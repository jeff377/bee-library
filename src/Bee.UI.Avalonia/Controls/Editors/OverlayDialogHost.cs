using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Presents a panel as a modal overlay on the current top level's
    /// <see cref="OverlayLayer"/>. This is the single-view fallback for hosts that cannot
    /// open a native <see cref="Window"/> — notably the browser (WASM) head, where a
    /// <c>Window.ShowDialog</c> throws. Desktop heads keep using the window path; only the
    /// browser branch of <see cref="LookupDialog"/> / <see cref="RowEditDialog"/> calls this.
    /// </summary>
    internal static class OverlayDialogHost
    {
        /// <summary>
        /// Adds <paramref name="content"/> as a centered modal card over a dimmed backdrop,
        /// then awaits <paramref name="completion"/> before removing it.
        /// </summary>
        /// <param name="host">A visual inside the target top level (resolves the overlay layer).</param>
        /// <param name="content">The panel to host (e.g. <see cref="LookupPanel"/> / <see cref="RowEditPanel"/>).</param>
        /// <param name="title">Optional header text shown above the panel.</param>
        /// <param name="completion">A task that completes when the panel commits or cancels.</param>
        public static async Task ShowAsync(Visual host, Control content, string? title, Task completion)
        {
            ArgumentNullException.ThrowIfNull(host);
            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(completion);

            var overlay = OverlayLayer.GetOverlayLayer(host)
                ?? throw new InvalidOperationException("No OverlayLayer is available for the current top level.");

            var variant = (host as StyledElement)?.ActualThemeVariant ?? ThemeVariant.Default;
            var card = BuildCard(content, title, variant);
            card.HorizontalAlignment = HorizontalAlignment.Center;
            card.VerticalAlignment = VerticalAlignment.Center;

            // The backdrop's non-null brush both dims the page and captures pointer input so
            // the dimmed content stays inert (a null background would not hit-test). It does not
            // dismiss on click — only the panel's OK / Cancel close the dialog, so an in-progress
            // row edit cannot be lost by a stray click outside the card.
            var backdrop = new Border
            {
                Background = new SolidColorBrush(Colors.Black, 0.45),
                Child = card,
            };

            // OverlayLayer derives from Canvas, so children are sized to their content rather
            // than stretched; size the backdrop to the layer explicitly and track resizes.
            void Resize()
            {
                backdrop.Width = overlay.Bounds.Width;
                backdrop.Height = overlay.Bounds.Height;
                card.MaxHeight = overlay.Bounds.Height * 0.9;
            }
            void OnOverlayChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Property == Visual.BoundsProperty) Resize();
            }

            Resize();
            overlay.PropertyChanged += OnOverlayChanged;
            overlay.Children.Add(backdrop);
            try
            {
                await completion.ConfigureAwait(true);
            }
            finally
            {
                overlay.PropertyChanged -= OnOverlayChanged;
                overlay.Children.Remove(backdrop);
            }
        }

        private static Border BuildCard(Control content, string? title, ThemeVariant variant)
        {
            var dark = variant == ThemeVariant.Dark;
            var surface = dark ? Color.FromRgb(0x2A, 0x2B, 0x33) : Color.FromRgb(0xFF, 0xFF, 0xFF);
            var stroke = dark ? Color.FromRgb(0x3C, 0x3D, 0x47) : Color.FromRgb(0xD0, 0xD0, 0xD8);

            var inner = new DockPanel { LastChildFill = true };
            if (!string.IsNullOrEmpty(title))
            {
                var header = new TextBlock
                {
                    Text = title,
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(16, 12, 16, 0),
                };
                DockPanel.SetDock(header, Dock.Top);
                inner.Children.Add(header);
            }
            inner.Children.Add(content);

            return new Border
            {
                Background = new SolidColorBrush(surface),
                BorderBrush = new SolidColorBrush(stroke),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                MaxWidth = 560,
                ClipToBounds = true,
                Child = inner,
            };
        }
    }
}
