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
	/// Mosquito Insect effect enum
	/// </summary>
	public static RoomSettings.RoomEffect.Type MosquitoInsects = new(nameof(MosquitoInsects), true);
	/// Butterfly A effect enum
	/// </summary>
	public static RoomSettings.RoomEffect.Type ButterfliesA = new(nameof(ButterfliesA), true);
	/// <summary>
	/// Butterfly B effect enum
	/// </summary>
	public static RoomSettings.RoomEffect.Type ButterfliesB = new(nameof(ButterfliesB), true);
	/// <summary>
	/// Zipper effect enum
	/// </summary>
	public static RoomSettings.RoomEffect.Type Zippers = new(nameof(Zippers), true);
	
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
	/// Butterfly A enum
	/// </summary>
	public static CosmeticInsect.Type ButterflyA = new(nameof(ButterflyA), true);
	/// <summary>
	/// Butterfly B enum
	/// </summary>
	public static CosmeticInsect.Type ButterflyB = new(nameof(ButterflyB), true);
	/// <summary>
	/// Zipper enum
	/// </summary>
	public static CosmeticInsect.Type Zipper = new(nameof(Zipper), true);
	/// <summary>
	/// Circuit fly enum
	/// </summary>
	public static CosmeticInsect.Type CircuitFly = new(nameof(CircuitFly), true);

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
	public static RoomSettings.RoomEffect.Type MossWaterRGB = new(nameof(MossWaterRGB), true);
	/// <summary>
	/// Effect category
	/// </summary>
	public static RoomSettings.RoomEffect.Type IceWater = new(nameof(IceWater), true);
	/// <summary>
	/// Effect category
	/// </summary>
	public static RoomSettings.RoomEffect.Type MurkyWater = new(nameof(MurkyWater), true);
	/// <summary>
	/// Effect category
	/// </summary>
	public static RoomSettings.RoomEffect.Type ReflectiveWater = new(nameof(ReflectiveWater), true);

	/// <summary>
	/// Effect category
	/// </summary>
	public static RoomSettings.RoomEffect.Type RGBElectricDeath = new(nameof(RGBElectricDeath), true);
	/// <summary>
	/// Effect category
	/// </summary>
	public static RoomSettings.RoomEffect.Type HSLDisplaySnow = new(nameof(HSLDisplaySnow), true);
	/// <summary>
	/// Effect category
	/// </summary>
	public static DevInterface.RoomSettingsPage.DevEffectsCategories RegionKit = new(nameof(RegionKit), true);
}
