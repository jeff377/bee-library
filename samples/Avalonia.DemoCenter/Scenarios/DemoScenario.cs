using Avalonia.Controls;

namespace Avalonia.DemoCenter.Scenarios
{
    /// <summary>
    /// Describes a single demo scenario: where it sits in the navigation tree
    /// (<see cref="Category"/> → <see cref="ControlName"/> → <see cref="ScenarioTitle"/>),
    /// a short description, and a factory that builds the live interactive view.
    /// </summary>
    /// <remarks>
    /// This is the Stage 1 lightweight shape. Stage 2 promotes scenarios to a formal
    /// <c>IDemoModule</c> abstraction with a registry and embedded-source (View Source)
    /// support; the factory delegate here maps onto that module's <c>BuildView</c>.
    /// </remarks>
    public sealed record DemoScenario(
        string Category,
        string ControlName,
        string ScenarioTitle,
        string Description,
        Func<Control> BuildView);
}
