using Bee.Base;
using Bee.Definition.Layouts;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Field editor for <see cref="ControlType.TimeEdit"/>: a fixed-width time-of-day input that
    /// normalises what the user types to <c>"HH:mm"</c> when the value is committed, so
    /// <c>"8:30"</c> is stored as <c>"08:30"</c>.
    /// </summary>
    /// <remarks>
    /// Display format equals storage format, so this editor does no culture-aware formatting at all
    /// — it is a masked text input, not a clock or spinner (ADR-033). Normalising on commit is what
    /// upholds the fixed-width ordering guarantee: a <c>char(5)</c> column only sorts and
    /// range-scans chronologically while every stored value is zero-padded.
    /// <para>
    /// Input that does not parse as a time keeps the last committed value rather than clearing the
    /// field, matching <see cref="NumericEdit"/> — a stray keystroke should never silently destroy
    /// data. Clearing the box is still an explicit way to unset the field.
    /// </para>
    /// </remarks>
    public class TimeEdit : TextEdit
    {
        // The committed value in storage form. Text may briefly hold whatever the user is typing;
        // write-backs always use this.
        private string _value = string.Empty;

        /// <summary>
        /// Initializes a new instance of <see cref="TimeEdit"/>.
        /// </summary>
        public TimeEdit()
        {
            MaxLength = ValueUtilities.TimeOnlyLength;
            // Subscribed after the base TextEdit commit handler (registered in its constructor), so
            // this runs once the bound value is written and can show the normalised form.
            LostFocus += (_, _) => Text = _value;
        }

        /// <inheritdoc />
        protected override void RefreshFromSource()
        {
            _value = ValueUtilities.CTimeString(Binder.GetValue());
            Text = _value;
        }

        /// <inheritdoc />
        protected override string? GetWriteBackValue()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                // An emptied box is an explicit "unset"; the empty string is how an unfilled time of
                // day is stored, because midnight is a legal value and cannot stand in for it.
                _value = string.Empty;
            }
            else
            {
                string normalized = ValueUtilities.CTimeString(Text);
                if (StringUtilities.IsNotEmpty(normalized)) { _value = normalized; }
            }
            return _value;
        }
    }
}
