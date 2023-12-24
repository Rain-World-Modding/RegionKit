namespace RegionKit.Modules.Iggy;

public interface IGiveAToolTip : IGeneralMouseOver
{
	public int ToolTipPriority { get; }
	public string ToolTip { get; }
}
