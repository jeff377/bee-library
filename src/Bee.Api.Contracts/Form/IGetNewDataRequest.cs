namespace Bee.Api.Contracts.Form
{
    /// <summary>
    /// Contract interface for the GetNewData request.
    /// </summary>
    /// <remarks>
    /// Step 1 of the "new + save" flow. The server returns a blank
    /// <c>DataSet</c> skeleton seeded with FormSchema defaults, a server-issued
    /// <c>sys_rowid</c>, and empty detail tables. The target
    /// <c>FormSchema</c> is identified by the <c>ProgId</c> embedded in the
    /// JSON-RPC method name and is not carried on the request, so the
    /// contract currently exposes no properties.
    /// </remarks>
    public interface IGetNewDataRequest
    {
    }
}
