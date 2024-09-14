namespace RegionKit.Modules.Iggy;
/// <summary>
/// Interface for DevUINodes that have tooltips.
/// </summary>
public interface IGiveAToolTip : IGeneralMouseOver
{
	/// <summary>
	/// Read when Iggy requests an element description. May return null if no custom tooltip should appear at the moment.
	/// </summary>
	/// <value></value>
	public ToolTip? ToolTip { get; }
}
