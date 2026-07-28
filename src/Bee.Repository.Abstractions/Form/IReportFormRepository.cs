namespace Bee.Repository.Abstractions.Form
{
    /// <summary>
    /// Repository contract for report forms.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately empty.</b> Reporting and batch work follow the AnyCode track: the business
    /// object writes its own SQL against <c>IDbAccessFactory</c> rather than going through a
    /// FormSchema-driven CRUD repository, so there is no operation every report form shares. The
    /// type exists as the binding target for <c>IFormRepositoryFactory.CreateReportFormRepository</c>
    /// — an application can return its own implementation carrying whatever members its reports
    /// need, and the framework never calls into it.
    /// <para>
    /// If a genuinely shared operation appears, declare it here. If none has appeared by the time
    /// this note looks old, remove the interface together with the factory method.
    /// </para>
    /// </remarks>
    public interface IReportFormRepository
    {
    }
}
