using System.Globalization;

namespace Bee.Db.Sql
{
    /// <summary>
    /// Default parameter collector that generates parameter names as @p0, @p1, ... (or with a custom prefix).
    /// </summary>
    public sealed class DefaultParameterCollector : IParameterCollector
    {
        private readonly Dictionary<string, object> _parameters = [];
        private int _index;

        /// <summary>
        /// Gets the parameter prefix. SQL Server uses '@'; Oracle may use ':'.
        /// </summary>
        public string Prefix { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultParameterCollector"/>.
        /// </summary>
        /// <param name="prefix">The parameter prefix character (e.g., "@" for SQL Server).</param>
        public DefaultParameterCollector(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));
            }
            Prefix = prefix;
        }

        /// <inheritdoc/>
        /// <summary>
        /// Adds a parameter value and returns the generated parameter name (including the prefix).
        /// </summary>
        /// <param name="value">The parameter value to add.</param>
        /// <returns>The generated parameter name in the format "prefix + 'p' + index" (e.g., @p0, @p1).</returns>
        public string Add(object value)
        {
            var name = Prefix + "p" + _index.ToString(CultureInfo.InvariantCulture);
            _index++;
            _parameters[name] = value;
            return name;
        }

        /// <inheritdoc/>
        public IDictionary<string, object> GetAll()
        {
            return _parameters;
        }
    }
}
