namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Extension methods for <see cref="FormEditModes"/>.
    /// </summary>
    public static class FormEditModesExtensions
    {
        /// <summary>
        /// Determines whether editing is allowed in the specified form mode.
        /// View mode is never editable regardless of the flags.
        /// </summary>
        /// <param name="modes">The allowed edit modes.</param>
        /// <param name="formMode">The single-record form mode.</param>
        /// <returns>True if editing is allowed in the mode; otherwise, false.</returns>
        public static bool Allows(this FormEditModes modes, SingleFormMode formMode)
            => formMode switch
            {
                SingleFormMode.Add => modes.HasFlag(FormEditModes.Add),
                SingleFormMode.Edit => modes.HasFlag(FormEditModes.Edit),
                _ => false,
            };
    }
}
