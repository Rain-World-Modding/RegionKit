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

	public static RoomSettings.RoomEffect.Type FogOfWarSolid = new(nameof(FogOfWarSolid), false); // disabled
	public static RoomSettings.RoomEffect.Type FogOfWarDarkened = new(nameof(FogOfWarDarkened), false); //disabled
	/// <summary>
	/// Glowing Swimmer effect enum
	/// </summary>
	public static RoomSettings.RoomEffect.Type GlowingSwimmers = new(nameof(GlowingSwimmers), true);
	/// <summary>
	/// Glowing Swimmer insect enum
	/// </summary>
	public static CosmeticInsect.Type GlowingSwimmerInsect = new(nameof(GlowingSwimmerInsect), true);
	/// <summary>
	/// ReplaceEffectColorA
	/// </summary>
	public static RoomSettings.RoomEffect.Type ReplaceEffectColorA = new(nameof(ReplaceEffectColorA), true);
	/// <summary>
	/// ReplaceEffectColorB
	/// </summary>
	public static RoomSettings.RoomEffect.Type ReplaceEffectColorB = new(nameof(ReplaceEffectColorB), true);
}
