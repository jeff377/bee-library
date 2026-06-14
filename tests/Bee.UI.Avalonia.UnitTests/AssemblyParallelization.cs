// Run this assembly's tests serially.
//
// Almost every test here constructs Avalonia controls, and control construction walks
// Avalonia's internal `AvaloniaPropertyDictionaryPool` (via SetInheritanceParent when a
// control parents its children). That pool's Get/Return over a shared Stack is not
// thread-safe: two test threads constructing controls at the same moment intermittently
// hit `Stack empty` (a TOCTOU between the count check and Pop). This is an Avalonia-internal
// race, not Bee static state, so the fixture-scoped DI approach used elsewhere cannot remove
// it; AvaloniaRegistryWarmup only covers the one-time *registry* population, not the ongoing
// pool access. Disabling collection parallelization for this assembly serializes control
// construction and removes the flake. The assembly runs in well under a second, so the cost
// is negligible.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
