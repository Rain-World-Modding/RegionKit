namespace RegionKit.Modules.TheMast;
///<inheritdoc/>
public class _Enums
{
	/// <summary>
	/// Shiny
	/// </summary>
	public static AbstractPhysicalObject.AbstractObjectType PearlChain = new(nameof(PearlChain), true);
	/// <summary>
	/// Placed object for pearl chains
	/// </summary>
	public static PlacedObject.Type PlacedPearlChain = new(nameof(PlacedPearlChain), true);
	/// <summary>
	/// Dialogue for pearl chains
	/// </summary>
	public static SLOracleBehaviorHasMark.MiscItemType MiscItemPearlChain = new(nameof(MiscItemPearlChain), true);
	/// <summary>
	/// Dialogue for torn pearl chain
	/// </summary>
	/// <returns></returns>
	public static SLOracleBehaviorHasMark.MiscItemType MiscItemSinglePearlChain = new(nameof(MiscItemSinglePearlChain), true);
	/// <summary>
	/// Wind system
	/// </summary>
	public static PlacedObject.Type PlacedWind = new(nameof(PlacedWind), true);
}
