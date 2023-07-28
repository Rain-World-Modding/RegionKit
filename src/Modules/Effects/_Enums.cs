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
	public static RoomSettings.RoomEffect.Type FogOfWarSolid = new(nameof(FogOfWarSolid), false); // disabled
	public static RoomSettings.RoomEffect.Type FogOfWarDarkened = new(nameof(FogOfWarDarkened), false); //disabled
	/// <summary>
	/// Glowing Swimmer effect enum
	/// </summary>
	public static RoomSettings.RoomEffect.Type GlowingSwimmers = new(nameof(GlowingSwimmers), true);
	/// <summary>
	/// Colored Camo Beetle effect enum
	/// </summary>
	public static RoomSettings.RoomEffect.Type ColoredCamoBeetles = new(nameof(ColoredCamoBeetles), true);
	/// <summary>
	/// Mosquito Insect effet enum
	/// </summary>
	public static RoomSettings.RoomEffect.Type MosquitoInsects = new(nameof(MosquitoInsects), true);
	/// <summary>
	/// Glowing Swimmer insect enum
	/// </summary>
	public static CosmeticInsect.Type GlowingSwimmerInsect = new(nameof(GlowingSwimmerInsect), true);
	/// <summary>
	/// Colored Camo Beetle insect enum
	/// </summary>
	public static CosmeticInsect.Type ColoredCamoBeetle = new(nameof(ColoredCamoBeetle), true);
	/// <summary>
	/// Mosquito Insect enum
	/// </summary>
	public static CosmeticInsect.Type MosquitoInsect = new(nameof(MosquitoInsect), true);
	/// <summary>
	/// ReplaceEffectColorA
	/// </summary>
	public static RoomSettings.RoomEffect.Type ReplaceEffectColorA = new(nameof(ReplaceEffectColorA), true);
	/// <summary>
	/// ReplaceEffectColorB
	/// </summary>
	public static RoomSettings.RoomEffect.Type ReplaceEffectColorB = new(nameof(ReplaceEffectColorB), true);
	/// <summary>
	/// HiveColorAlpha
	/// </summary>
	public static RoomSettings.RoomEffect.Type HiveColorAlpha = new(nameof(HiveColorAlpha), true);
	/// <summary>
	/// MossWater
	/// </summary>
	public static RoomSettings.RoomEffect.Type MossWater = new(nameof(MossWater), true);
	/// <summary>
	/// Effect category
	/// </summary>
	public static DevInterface.RoomSettingsPage.DevEffectsCategories RegionKit = new(nameof(RegionKit), true);
}
