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
	/// ReplaceCorruptionColor
	/// </summary>
	public static RoomSettings.RoomEffect.Type ReplaceCorruptionColor = new(nameof(ReplaceCorruptionColor), true);
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
	public static RoomSettings.RoomEffect.Type IceWater = new(nameof(IceWater), false); // TODO: set to true when working on again
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
	public static RoomSettings.RoomEffect.Type Suffocation = new(nameof(Suffocation), true);
	/// <summary>
	/// Effect category
	/// </summary>
	public static RoomSettings.RoomEffect.Type RainSiren = new(nameof(RainSiren), true);
	/// <summary>
	/// Effect category
	/// </summary>
	public static RoomSettings.RoomEffect.Type HSLDisplaySnow = new(nameof(HSLDisplaySnow), true);
	/// <summary>
	/// FT dense fog
	/// </summary>
	public static RoomSettings.RoomEffect.Type DenseFog = new(nameof(DenseFog), true);
	/// <summary>
	/// FT dense fog volume changer
	/// </summary>
	public static RoomSettings.RoomEffect.Type DenseFogSoundVolume = new(nameof(DenseFogSoundVolume), true);
	/// <summary>
	/// FT dense fog sound
	/// </summary>
	public static SoundID FT_Fog_PreDeath = new(nameof(FT_Fog_PreDeath), true);
	/// <summary>
	/// Pole plant color effect by LB/M4rbleL1ne
	/// </summary>
	public static RoomSettings.RoomEffect.Type PolePlantColor = new(nameof(PolePlantColor), true);



	/// <summary>
	/// Effect category
	/// </summary>
	public static DevInterface.RoomSettingsPage.DevEffectsCategories RegionKit = new(nameof(RegionKit), true);
}
