namespace RegionKit.Modules.Iggy;

/// <summary>
/// Represents an iggy tooltip that appears on right clicking a devtools UI element.
/// </summary>
/// <param name="text">What Iggy should say.</param>
/// <param name="priority">Value that determines how important tooltip from a particular element is. Highest value element is chosen. Big container-like elements such as Panels have 5, buttons have 10, Iggy itself has 1.</param>
/// <param name="source">Node that gave the tooltip. Used to prevent duplicates when requesting a tooltip.</param>
/// <returns></returns>
public record ToolTip(string text, int priority, DevInterface.DevUINode source);
