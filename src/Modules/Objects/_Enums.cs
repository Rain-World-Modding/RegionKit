namespace RegionKit.Modules.Objects;

///<inheritdoc/>
public class _Enums
{
	/// <summary>
	/// Colorable light rod
	/// </summary>
	public static PlacedObject.Type PWLightrod = new(nameof(PWLightrod), true);
	/// <summary>
	/// Kills creatures that enter the rect
	/// </summary>
	public static PlacedObject.Type ARKillRect = new(nameof(ARKillRect), true);
	/// <summary>
	/// Replaces pipe symbols
	/// </summary>
	public static PlacedObject.Type CustomEntranceSymbol = new(nameof(CustomEntranceSymbol), true);
	/// <summary>
	/// Makes wall slippery
	/// </summary>
	public static PlacedObject.Type NoWallSlideZone = new(nameof(NoWallSlideZone), true);
	/// <summary>
	/// Planet hologram
	/// </summary>
	public static PlacedObject.Type LittlePlanet = new(nameof(LittlePlanet), true);
	/// <summary>
	/// Persistent rainbow
	/// </summary>
	public static PlacedObject.Type RainbowNoFade = new(nameof(RainbowNoFade), true);
	/// <summary>
	/// Hologram circle
	/// </summary>
	/// <returns></returns>
	public static PlacedObject.Type ProjectedCircle = new(nameof(ProjectedCircle), true);
	/// <summary>
	/// Upside down waterfall
	/// </summary>
	public static PlacedObject.Type UpsideDownWaterFall = new(nameof(UpsideDownWaterFall), true);
	/// <summary>
	/// Colored light beam
	/// </summary>
	public static PlacedObject.Type ColoredLightBeam = new(nameof(ColoredLightBeam), true);
}
