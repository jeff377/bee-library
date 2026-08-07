namespace Bee.Business
{
    /// <summary>
    /// Interface for handlers that execute methods identified by a FuncID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// NOTE: this interface is deliberately empty. Dispatch resolves a FuncID to a method by name
    /// through reflection, so handlers declare arbitrarily named methods with the signature
    /// <c>(ExecFuncArgs, ExecFuncResult)</c>; a fixed member here could not express that and would
    /// only get in the way.
    /// </para>
    /// <para>
    /// It earns its place as a marker in two ways. It gives
    /// <see cref="ExecFuncHandlerExtensions.InvokeExecFunc(IExecFuncHandler, Bee.Definition.Security.ApiAccessRequirement, bool, ExecFuncArgs, ExecFuncResult)"/>
    /// a receiver type other than <c>object</c> — an extension method on <c>object</c> would appear on
    /// every type in the consumer's IntelliSense — and it is how rule BEE3003 recognises a handler in
    /// order to check that each dispatchable method declares
    /// <see cref="Attributes.ExecFuncAccessControlAttribute"/>.
    /// </para>
    /// <para>
    /// WARNING: implementing this interface guarantees nothing at compile time. A type carrying no
    /// dispatchable method at all still satisfies it. What keeps the surface safe is that dispatch is
    /// fail-closed for undeclared methods, with BEE3003 reporting the omission during the build.
    /// </para>
    /// </remarks>
    public interface IExecFuncHandler
    {
    }
}
