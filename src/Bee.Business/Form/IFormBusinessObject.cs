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

        /// <summary>
        /// Retrieves lookup candidate rows for picker windows that reference this
        /// form; the projection is server-resolved from <c>FormSchema.LookupFields</c>.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        GetLookupResult GetLookup(GetLookupArgs args);

        /// <summary>
        /// Returns a blank <c>DataSet</c> skeleton seeded with FormSchema
        /// defaults and a server-issued <c>sys_rowid</c>; step 1 of the
        /// new-and-save flow.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        GetNewDataResult GetNewData(GetNewDataArgs args);

        /// <summary>
        /// Loads a single master row (and its details) by <c>RowId</c>;
        /// step 1 of the load-and-save flow.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        GetDataResult GetData(GetDataArgs args);

        /// <summary>
        /// Persists a <c>DataSet</c> by dispatching INSERT / UPDATE / DELETE
        /// based on each row's <c>RowState</c>; step 2 of both the new-and-save
        /// and load-and-save flows.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        SaveResult Save(SaveArgs args);

        /// <summary>
        /// Deletes a single master row directly by <c>RowId</c> without first
        /// loading the full <c>DataSet</c>.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        DeleteResult Delete(DeleteArgs args);
    }
}
