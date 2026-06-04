namespace Bee.Definition.Settings
{
    /// <summary>
    /// The named record-scope strategy applied to a (model, action) pair.
    /// </summary>
    /// <remarks>
    /// Strategies are pure semantics: the concrete column is supplied by the consuming
    /// FormSchema via <c>FormField.ScopeRole</c>, not named here. Parameterised strategies
    /// (owner-field, node lists, custom predicates) are deferred — this phase ships the four
    /// parameter-free strategies plus <see cref="Inherit"/>.
    /// </remarks>
    public enum ScopeStrategy
    {
        /// <summary>
        /// No scope is defined on this action; it inherits the model's Read scope. This is
        /// the default for egress actions (Print / Export), which omit the scope entirely.
        /// </summary>
        Inherit,

        /// <summary>Full range — no record-level restriction.</summary>
        All,

        /// <summary>Only records owned by the current user (the field marked ScopeRole=Owner).</summary>
        Own,

        /// <summary>Records of the current user's department (the field marked ScopeRole=Dept).</summary>
        Dept,

        /// <summary>Records of the current user's department and its sub-departments.</summary>
        DeptAndSub
    }
}
