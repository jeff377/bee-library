using Avalonia;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;

namespace Bee.UI.Avalonia.Views
{
    /// <summary>
    /// Responsive-layout half of <see cref="FormView"/> (compact viewport detection, layout
    /// re-evaluation and error surface). Split out for file size only; behaviour is unchanged.
    /// </summary>
    public partial class FormView
    {
        // ---- Responsive layout (compact = phone-sized viewport) ----

        /// <summary>
        /// Reacts to the inputs of the responsive layout decision. <see cref="Visual.Bounds"/> is
        /// a direct property whose change notifications are not delivered to static class handlers,
        /// so the width reaction is wired here (the same pattern OverlayDialogHost uses) rather than
        /// in the static constructor.
        /// </summary>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == BoundsProperty || change.Property == CompactWidthThresholdProperty)
            {
                ApplyResponsiveState();
            }
            else if (change.Property == DetailEditModeProperty && !_isCompact)
            {
                // The preferred detail mode only takes effect on wide layouts; a compact layout
                // forces EditForm regardless, so only a wide form needs to re-render here.
                RebuildIfReady();
            }
        }

        /// <summary>
        /// Pure decision: a positive width below <paramref name="compactWidthThreshold"/> is a
        /// compact (phone-sized) viewport. A non-positive width means "not measured yet", so the
        /// layout stays in its wide form until the first layout pass.
        /// </summary>
        internal static bool IsCompactWidth(double viewportWidth, double compactWidthThreshold)
            => viewportWidth > 0 && viewportWidth < compactWidthThreshold;

        /// <summary>
        /// Gets the viewport width used for the compact-layout decision. Defaults to the
        /// control's own <see cref="Visual.Bounds"/> width; overridden in tests to drive the
        /// responsive switch without a real layout pass.
        /// </summary>
        protected virtual double GetViewportWidth() => Bounds.Width;

        /// <summary>
        /// Recomputes the compact state from the current viewport width and rebuilds the form only
        /// when the <see cref="CompactWidthThreshold"/> boundary is crossed, so the frequent
        /// within-band <see cref="Visual.Bounds"/> ticks during layout cost a single comparison.
        /// </summary>
        protected void ApplyResponsiveState()
        {
            var compact = IsCompactWidth(GetViewportWidth(), CompactWidthThreshold);
            if (compact == _isCompact) return;
            _isCompact = compact;
            RebuildIfReady();
        }

        private void RebuildIfReady()
        {
            if (_formLayout is not null && _dataObject is not null)
                Rebuild();
        }

        // Master fields collapse to a single column on a compact viewport; otherwise the layout's
        // own column count applies.
        private int EffectiveColumnCount()
            => _isCompact ? 1 : NormalizeColumnCount(_formLayout?.ColumnCount);

        // Detail grids edit in a form on a compact viewport; otherwise the preferred mode applies.
        private GridEditMode EffectiveDetailEditMode()
            => _isCompact ? GridEditMode.EditForm : DetailEditMode;

        private IEnumerable<LayoutSection> EnumerateSections()
            => _formLayout?.Sections ?? Enumerable.Empty<LayoutSection>();

        private IEnumerable<LayoutGrid> EnumerateDetails()
            => _formLayout?.Details ?? Enumerable.Empty<LayoutGrid>();

        private static IEnumerable<LayoutField> EnumerateFields(LayoutSection section)
            => section.Fields?.Where(f => f.Visible) ?? Enumerable.Empty<LayoutField>();

        private static int NormalizeColumnCount(int? columnCount)
        {
            var n = columnCount ?? 1;
            return n < 1 ? 1 : n;
        }

        private static (int rowSpan, int columnSpan) NormalizeSpans(LayoutField field)
        {
            var rowSpan = field.RowSpan < 1 ? 1 : field.RowSpan;
            var colSpan = field.ColumnSpan < 1 ? 1 : field.ColumnSpan;
            return (rowSpan, colSpan);
        }

        private void ReportError(Exception ex)
        {
            _errorLabel.Text = ex.Message;
            _errorLabel.IsVisible = true;
            ErrorOccurred?.Invoke(this, ex);
        }

        private void ClearError()
        {
            _errorLabel.Text = string.Empty;
            _errorLabel.IsVisible = false;
        }
    }
}
