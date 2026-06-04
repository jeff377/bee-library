namespace Bee.Definition.Forms
{
    /// <summary>
    /// The record-scope role a form field plays in permission filtering. It marks which
    /// column a named scope strategy resolves to, keeping the strategy pure semantics —
    /// the strategy never names a column directly.
    /// </summary>
    public enum ScopeRole
    {
        /// <summary>The field plays no scope role (the default).</summary>
        None,

        /// <summary>The owner column, resolved by the <c>Own</c> scope strategy.</summary>
        Owner,

        /// <summary>The department column, resolved by the <c>Dept</c> / <c>DeptAndSub</c> strategies.</summary>
        Dept
    }
}
