using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Shared caption convention for field state, applied uniformly to master form field
    /// captions and detail grid headers. A required field's caption is blue. A read-only field
    /// is marked by parenthesising its caption (<see cref="FormatCaption"/>) — a theme-independent
    /// cue that reads on both light and dark variants, unlike a colour — while master form fields
    /// additionally show the editor's read-only underline. Read-only wins when a field is both:
    /// a read-only field cannot be filled in, so the required colour is suppressed.
    /// </summary>
    internal static class FieldCaptionStyle
    {
        // Required: blue, chosen to read on the light and dark theme variants.
        private static readonly ImmutableSolidColorBrush s_requiredBrush =
            new(Color.FromRgb(0x25, 0x63, 0xEB));

        /// <summary>
        /// Returns the caption foreground for the given field state, or <c>null</c> to use the
        /// theme default. Required fields are blue; read-only fields use the theme default (their
        /// cue is the parenthesised caption from <see cref="FormatCaption"/>, plus the editor's
        /// read-only underline on master fields). Read-only suppresses the required colour.
        /// </summary>
        /// <param name="readOnly">Whether the field is read-only.</param>
        /// <param name="required">Whether the field is required (mandatory input).</param>
        public static IBrush? GetCaptionForeground(bool readOnly, bool required)
            => (required && !readOnly) ? s_requiredBrush : null;

        /// <summary>
        /// Marks a read-only field's caption by wrapping it in parentheses (e.g. <c>Amount</c> →
        /// <c>(Amount)</c>); returns the caption unchanged when the field is editable or empty.
        /// </summary>
        /// <param name="caption">The field caption.</param>
        /// <param name="readOnly">Whether the field is read-only.</param>
        public static string FormatCaption(string caption, bool readOnly)
            => readOnly && !string.IsNullOrEmpty(caption) ? $"({caption})" : caption;
    }
}
