namespace Bee.Tests.Shared
{
    /// <summary>
    /// Marks a parameterized test to run only in local environments.
    /// When the CI environment variable is set to "true" (e.g., GitHub Actions),
    /// the test is automatically skipped.
    /// </summary>
    public class LocalOnlyTheoryAttribute : TheoryAttribute
    {
        public LocalOnlyTheoryAttribute()
        {
            if (Environment.GetEnvironmentVariable("CI") == "true")
                Skip = "Skipped on CI – requires local infrastructure (e.g. running API server)";
        }
    }
}
