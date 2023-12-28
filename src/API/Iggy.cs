using Impl = RegionKit.Modules.Iggy;

namespace RegionKit.API;

/// <summary>
/// Functions related to dev-iggy (devtools clippy in a hardhat)
/// </summary>
public static class Iggy
{
	/// <summary>
	/// Gives a DevUINode a tooltip. Uses a callback to allow different text based on element state.
	/// <para/>
	/// If you are making a new UI node class, you should implement <see cref="global::RegionKit.Modules.Iggy.IGiveAToolTip"/> instead.
	/// </summary>
	public static void AddTooltip(DevInterface.DevUINode node, Func<Impl.ToolTip> toolTipProducer)
	{
		ThrowIfModNotInitialized();
		Impl._Module.__attachedToolTips.Add(node, toolTipProducer);
	}
}