namespace Bee.Definition.Language
{
    /// <summary>
    /// The key format <see cref="ILanguageService"/> uses: <c>namespace.subKey</c>.
    /// </summary>
    /// <remarks>
    /// This lives here rather than in each service because the split **is** the contract, not an
    /// implementation detail: the server's <see cref="LanguageService"/> and the client's snapshot
    /// service must agree on where the boundary falls, and they held verbatim copies of the same
    /// method. A convention with two implementations is a convention that can change on one side
    /// only, and a resource lookup that silently misses is not a loud failure.
    /// </remarks>
    public static class LanguageKey
    {
        /// <summary>
        /// Splits a full key on the first <c>.</c> into its namespace and sub-key.
        /// </summary>
        /// <param name="fullKey">The full key, e.g. <c>Order.Title</c>.</param>
        /// <returns>The namespace and sub-key.</returns>
        /// <remarks>
        /// A key with no <c>.</c> is a namespace with an empty sub-key. Splitting on the **first**
        /// dot rather than the last means a sub-key may itself contain dots, which is what lets a
        /// namespace hold structured names.
        /// </remarks>
        public static (string Namespace, string SubKey) Split(string fullKey)
        {
            ArgumentNullException.ThrowIfNull(fullKey);

            int dot = fullKey.IndexOf('.', StringComparison.Ordinal);
            return dot < 0
                ? (fullKey, string.Empty)
                : (fullKey.Substring(0, dot), fullKey.Substring(dot + 1));
        }
    }
}
