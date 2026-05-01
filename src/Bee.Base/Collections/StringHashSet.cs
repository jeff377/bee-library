namespace Bee.Base.Collections
{
    /// <summary>
    /// A case-insensitive string collection that does not allow duplicate entries.
    /// </summary>
    public class StringHashSet : HashSet<string>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="StringHashSet"/>.
        /// </summary>
        public StringHashSet() : base(StringComparer.InvariantCultureIgnoreCase)
        { }

        #endregion

        /// <summary>
        /// Splits the given string by the specified delimiter and adds each token as a member.
        /// </summary>
        /// <param name="s">The string to split and add.</param>
        /// <param name="delimiter">The delimiter character or string.</param>
        public void Add(string s, string delimiter)
        {
            string[] oValues;

            if (StringUtilities.IsEmpty(s)) { return; }

            oValues = StringUtilities.Split(s, delimiter);
            foreach (string value in oValues)
                this.Add(value);
        }
    }
}
