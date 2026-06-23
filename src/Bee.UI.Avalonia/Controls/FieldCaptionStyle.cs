using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Shared caption-colour convention for field state, applied uniformly to master form
    /// field captions and detail grid headers: a read-only field's caption is brown, a
    /// required field's caption is blue. Read-only wins when a field is both — a read-only
    /// field cannot be filled in, so the required cue would be moot.
    /// </summary>
    internal static class FieldCaptionStyle
    {
        // Tunable cues. Read-only: sienna brown; required: blue. Both chosen to read on
        // the light and dark theme variants.
        private static readonly ImmutableSolidColorBrush s_readOnlyBrush =
            new(Color.FromRgb(0xA0, 0x52, 0x2D));

        private static readonly ImmutableSolidColorBrush s_requiredBrush =
            new(Color.FromRgb(0x25, 0x63, 0xEB));

        /// <summary>
        /// Returns the caption foreground for the given field state, or <c>null</c> to use
        /// the theme default. Read-only takes precedence over required.
        /// </summary>
        /// <param name="readOnly">Whether the field is read-only.</param>
        /// <param name="required">Whether the field is required (mandatory input).</param>
        public static IBrush? GetCaptionForeground(bool readOnly, bool required)
        {
            if (readOnly) return s_readOnlyBrush;
            if (required) return s_requiredBrush;
            return null;
        }
    }
}
