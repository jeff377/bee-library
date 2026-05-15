namespace Bee.Business.Form
{
    /// <summary>
    /// Interface for form-level business logic objects.
    /// </summary>
    public interface IFormBusinessObject : IBusinessObject
    {
        /// <summary>
        /// Retrieves list-view rows by executing the FormSchema-driven SELECT statement
        /// for the underlying program identifier.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        GetListResult GetList(GetListArgs args);
    }
}
