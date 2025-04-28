namespace RegionKit.Modules.Objects;

///<inheritdoc/>
public class _Enums
{
	/// <summary>
	/// LB Climbable wire
	/// </summary>
	public static PlacedObject.Type ClimbableWire = new(nameof(ClimbableWire), true);
	/// <summary>
	/// LB Climbable pole
	/// </summary>
	public static PlacedObject.Type ClimbablePole = new(nameof(ClimbablePole), true);
	/// <summary>
	/// Colorable light rod
	/// </summary>
	public static PlacedObject.Type PWLightrod = new(nameof(PWLightrod), true);
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
	/// <summary>
	/// LB's spinning fan light
	/// </summary>
	public static PlacedObject.Type FanLight = new(nameof(FanLight), true);
	/// <summary>
	/// Prevents batflies from lurking in an area (by pushing them away and somewhat telling them not to pathfind over there)
	/// </summary>
	public static PlacedObject.Type NoBatflyLurkZone = new(nameof(NoBatflyLurkZone), true);
	/// <summary>
	/// Light source that changes strength depending on distance from player
	/// </summary>
	public static PlacedObject.Type PCPlayerSensitiveLightSource = new(nameof(PCPlayerSensitiveLightSource), true);
	/// <summary>
	/// Throwable spike tip object created by breaking a spike
	/// </summary>
	public static AbstractPhysicalObject.AbstractObjectType SpikeTip = new(nameof(SpikeTip), true);
	/// <summary>
	/// WaterFall with custom depth
	/// </summary>
	public static PlacedObject.Type WaterFallDepth = new(nameof(WaterFallDepth), true);
	/// <summary>
	/// Prevents dropwigs from perching in a location
	/// </summary>
	public static PlacedObject.Type NoDropwigPerchZone = new(nameof(NoDropwigPerchZone), true);
}
