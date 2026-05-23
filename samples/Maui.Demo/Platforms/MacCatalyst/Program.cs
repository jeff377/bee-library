using ObjCRuntime;
using UIKit;

namespace Maui.Demo;

/// <summary>
/// Mac Catalyst platform <c>Main</c>. Required by the MAUI SDK when
/// <c>OutputType=Exe</c>; bootstraps UIKit with the typed <see cref="AppDelegate"/>.
/// </summary>
public static class Program
{
    /// <summary>The Mac Catalyst process entry point.</summary>
    /// <param name="args">Process command-line arguments.</param>
    public static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
