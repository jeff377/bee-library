namespace Bee.Definition
{
    /// <summary>
    /// Global events used for cross-project notifications.
    /// </summary>
    public static class GlobalEvents
    {
        /// <summary>
        /// Occurs when the database settings have changed.
        /// </summary>
        public static event EventHandler? DatabaseSettingsChanged;

        /// <summary>
        /// Raises the <see cref="DatabaseSettingsChanged"/> event.
        /// </summary>
        public static void RaiseDatabaseSettingsChanged()
        {
            DatabaseSettingsChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
