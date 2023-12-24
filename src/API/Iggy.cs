using Impl = RegionKit.Modules.Iggy;

namespace RegionKit.API;

public static class Iggy
{
	public static void AddTooltip(DevInterface.DevUINode node, Func<Impl.ToolTip> toolTipProducer)
	{
		ThrowIfModNotInitialized();
		Impl._Module.__attachedToolTips.Add(node, toolTipProducer);
	}
}