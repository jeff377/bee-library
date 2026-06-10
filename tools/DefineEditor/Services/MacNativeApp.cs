using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Bee.DefineEditor.Services;

/// <summary>
/// Thin Objective-C bridge for the three standard macOS application-menu
/// actions (Hide / Hide Others / Show All). Avalonia's NativeMenu has no
/// built-in roles for these once
/// <c>MacOSPlatformOptions.DisableDefaultApplicationMenuItems</c> is set, so
/// we send the NSApplication selectors ourselves. Callers must guard with
/// <see cref="OperatingSystem.IsMacOS"/>.
/// </summary>
/// <remarks>
/// Uses classic <c>DllImport</c> rather than <c>LibraryImport</c> — the
/// source-generated marshaller emits unsafe code, which would force
/// <c>AllowUnsafeBlocks</c> on for the whole project just for these three
/// selectors.
/// </remarks>
[SupportedOSPlatform("macos")]
internal static class MacNativeApp
{
    private const string LibObjC = "/usr/lib/libobjc.dylib";

    public static void Hide() => SendToSharedApp("hide:");

    public static void HideOthers() => SendToSharedApp("hideOtherApplications:");

    public static void ShowAll() => SendToSharedApp("unhideAllApplications:");

    private static void SendToSharedApp(string selector)
    {
        var nsApp = SendMessage(GetClass("NSApplication"), RegisterSelector("sharedApplication"));
        if (nsApp == IntPtr.Zero) return;
        SendMessage(nsApp, RegisterSelector(selector), IntPtr.Zero);
    }

    [DllImport(LibObjC, EntryPoint = "objc_getClass")]
    private static extern IntPtr GetClass(string name);

    [DllImport(LibObjC, EntryPoint = "sel_registerName")]
    private static extern IntPtr RegisterSelector(string name);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendMessage(IntPtr receiver, IntPtr selector);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendMessage(IntPtr receiver, IntPtr selector, IntPtr arg);
}
