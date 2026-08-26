namespace Bee.Definition.Logging
{
    /// <summary>
    /// One company's audit-rule snapshot: every row of its <c>st_audit_rule</c> table, indexed by
    /// program id. Cached per company, so asking whether a form has a rule costs a dictionary
    /// lookup rather than a database round-trip.
    /// </summary>
    /// <remarks>
    /// The whole table is snapshotted rather than each form cached separately because "this form
    /// has no rule" is the common answer — every form left on <see cref="AuditRuleMode.Inherit"/>
    /// gives it. Keying by program id would turn each of those into a miss, a query and a negative
    /// entry; holding the table answers them all from memory.
    /// <para>
    /// WARNING: this is a cache-shared instance. It must not be mutated after construction — every
    /// session in the company receives the same reference.
    /// </para>
    /// </remarks>
    public sealed class CompanyAuditRules
    {
        private readonly Dictionary<string, AuditRule> _byProgId;

        /// <summary>
        /// Initializes a new <see cref="CompanyAuditRules"/>.
        /// </summary>
        /// <param name="companyId">The company business id.</param>
        /// <param name="rules">Every rule row read from the company's <c>st_audit_rule</c> table.</param>
        /// <remarks>
        /// Program ids are compared with <see cref="StringComparer.Ordinal"/>: they are identifiers,
        /// not display text. A duplicate program id keeps the first row — the table's unique index
        /// on <c>sys_id</c> makes that unreachable in practice, and throwing here would take the
        /// whole company's auditing down over one bad row.
        /// </remarks>
        public CompanyAuditRules(string companyId, IReadOnlyList<AuditRule> rules)
        {
            ArgumentNullException.ThrowIfNull(rules);

            CompanyId = companyId ?? string.Empty;
            _byProgId = new Dictionary<string, AuditRule>(rules.Count, StringComparer.Ordinal);
            foreach (var rule in rules)
            {
                _byProgId.TryAdd(rule.ProgId, rule);
            }
        }

        /// <summary>Gets the company business id this snapshot belongs to.</summary>
        public string CompanyId { get; }

        /// <summary>Gets the number of rules in the snapshot.</summary>
        public int Count => _byProgId.Count;

        /// <summary>
        /// Gets the rule declared for the specified program id, or <c>null</c> when the form has
        /// none — which means every axis is <see cref="AuditRuleMode.Inherit"/>.
        /// </summary>
        /// <param name="progId">The form's program id.</param>
        public AuditRule? Find(string progId)
        {
            if (string.IsNullOrEmpty(progId)) { return null; }
            return _byProgId.TryGetValue(progId, out var rule) ? rule : null;
        }
    }
}
