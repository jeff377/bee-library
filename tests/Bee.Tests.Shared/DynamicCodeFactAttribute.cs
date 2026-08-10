using System.Runtime.CompilerServices;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Marks a test that exercises a capability only available where the runtime can generate
    /// code. It is skipped when <see cref="RuntimeFeature.IsDynamicCodeSupported"/> is false.
    /// </summary>
    /// <remarks>
    /// The AOT gate (<c>dotnet test … -p:DynamicCodeSupport=false</c>) reproduces what .NET for
    /// iOS sets on every build, and is expected to stay at zero failures. A handful of behaviours
    /// genuinely cannot work there — serializing a type the framework has no registered formatter
    /// for, for one, which is what the application-configurable
    /// <c>SysInfo.AllowedTypeNamespaces</c> escape hatch relies on. Marking those tests keeps the
    /// gate meaningful instead of carrying a list of "expected" failures.
    /// <para>
    /// Do not reach for this to make an inconvenient failure go away: if the framework's own wire
    /// types need dynamic code, that is the bug, not the test.
    /// </para>
    /// </remarks>
    public class DynamicCodeFactAttribute : FactAttribute
    {
        public DynamicCodeFactAttribute()
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                Skip = "Skipped where dynamic code is unavailable (the mobile AOT gate) – covers a desktop-only capability";
        }
    }
}
