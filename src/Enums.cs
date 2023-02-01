namespace RegionKit;
/// <summary>
/// Contains extended enum entries for the mod.
/// </summary>
public static class Enums
{
	/// <summary>
	/// SandStorm effect from Arid Barrens.
	/// </summary>
	/// <returns></returns>
	public static RoomSettings.RoomEffect.Type SandStorm = new(nameof(SandStorm), true);
	/// <summary>
	/// SandPuffs effect from AridBarrens
	/// </summary>
	/// <returns></returns>
	public static RoomSettings.RoomEffect.Type SandPuffs = new(nameof(SandPuffs), true);
	/// <summary>
	/// EchoExtender's variant of GhostSpot
	/// </summary>
	/// <returns></returns>
	public static PlacedObject.Type EEGhostSpot = new("EEGhostSpot", true);
}
