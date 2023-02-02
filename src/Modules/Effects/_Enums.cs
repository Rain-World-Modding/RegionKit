namespace RegionKit.Modules.Effects;
///<inheritdoc/>
public static class _Enums
{
	/// <summary>
	/// Special kind of BrokenZeroG from Substratum
	/// </summary>
	/// <returns></returns>
	public static RoomSettings.RoomEffect.Type PWMalfunction = new(nameof(PWMalfunction), true);
	//public static RoomSettings.RoomEffect.Type FogOfWarSolid = new(nameof(FogOfWarSolid), true);
	//public static RoomSettings.RoomEffect.Type FogOfWarDarkened = new(nameof(FogOfWarDarkened), true);
	/// <summary>
	/// Effect that adjusts AboveCloudsView altitude for specific rooms.
	/// </summary>
	/// <returns></returns>
	public static RoomSettings.RoomEffect.Type CloudAdjustment = new(nameof(CloudAdjustment), true);

}
