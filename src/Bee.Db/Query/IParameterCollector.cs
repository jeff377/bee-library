namespace Bee.Db.Query
{
    /// <summary>
    /// Defines the interface for a parameter collector used by query builders to generate named parameters.
    /// </summary>
    public interface IParameterCollector
    {
        /// <summary>Adds a parameter value and returns the generated parameter name (including the prefix).</summary>
        string Add(object value);

        /// <summary>Returns a dictionary of all collected parameters.</summary>
        IDictionary<string, object> GetAll();
    }
}
