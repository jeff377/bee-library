using System.Data;
using Bee.Definition.Filters;
using Bee.Definition.Paging;
using Bee.Definition.Sorting;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;

namespace Bee.Api.Core.UnitTests.Form
{
    /// <summary>
    /// In-memory stub of <see cref="IDataFormRepository"/> shared by the new
    /// CRUD JsonRpc round-trip tests. Each method records the input it was
    /// called with and returns a caller-configured payload, so tests can
    /// assert what the BO ultimately passed down to the repository.
    /// </summary>
    internal sealed class StubCrudDataFormRepository : IDataFormRepository
    {
        public DataSet? GetNewDataResult { get; set; }
        public DataSet? GetDataResult { get; set; }
        public (DataSet? Refreshed, Dictionary<string, int> AffectedRows) SaveResult { get; set; }
            = (null, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
        public int DeleteResult { get; set; }

        public bool GetNewDataCalled { get; private set; }
        public Guid LastRowId { get; private set; }
        public DataSet? LastSavedDataSet { get; private set; }

        public DataFormListResult GetList(
            string selectFields,
            FilterNode? filter,
            SortFieldCollection? sortFields,
            PagingOptions? paging = null)
            => throw new NotSupportedException();

        public DataSet GetNewData()
        {
            GetNewDataCalled = true;
            return GetNewDataResult ?? new DataSet();
        }

        public DataSet? GetData(Guid rowId, FilterNode? scopeFilter = null)
        {
            LastRowId = rowId;
            return GetDataResult;
        }

        public (DataSet? Refreshed, Dictionary<string, int> AffectedRows) Save(DataSet dataSet)
        {
            LastSavedDataSet = dataSet;
            return SaveResult;
        }

        public int Delete(Guid rowId)
        {
            LastRowId = rowId;
            return DeleteResult;
        }
    }

    /// <summary>
    /// Wraps a single <see cref="IDataFormRepository"/> instance behind the
    /// factory contract, so tests can inject the same recording stub for any
    /// requested <c>progId</c>.
    /// </summary>
    internal sealed class StubCrudFormRepositoryFactory : IFormRepositoryFactory
    {
        private readonly IDataFormRepository _data;

        public StubCrudFormRepositoryFactory(IDataFormRepository data)
        {
            _data = data;
        }

        public IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken) => _data;

        public IReportFormRepository CreateReportFormRepository(string progId)
            => throw new NotSupportedException();
    }
}
