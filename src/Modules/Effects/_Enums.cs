namespace RegionKit.Modules.Effects;
///<inheritdoc/>
public static class _Enums
{
	public static RoomSettings.RoomEffect.Type PWMalfunction = new(nameof(PWMalfunction), true);
	public static RoomSettings.RoomEffect.Type FogOfWarSolid = new(nameof(FogOfWarSolid), false); // disabled
	public static RoomSettings.RoomEffect.Type FogOfWarDarkened = new(nameof(FogOfWarDarkened), false); //disabled
	public static RoomSettings.RoomEffect.Type ReplaceCorruptionColor = new(nameof(ReplaceCorruptionColor), true);
	public static RoomSettings.RoomEffect.Type ReplaceEffectColorA = new(nameof(ReplaceEffectColorA), true);
	public static RoomSettings.RoomEffect.Type ReplaceEffectColorB = new(nameof(ReplaceEffectColorB), true);
	public static RoomSettings.RoomEffect.Type HiveColorAlpha = new(nameof(HiveColorAlpha), true);
	public static RoomSettings.RoomEffect.Type MossWater = new(nameof(MossWater), true);
	public static RoomSettings.RoomEffect.Type MossWaterRGB = new(nameof(MossWaterRGB), true);
	public static RoomSettings.RoomEffect.Type IceWater = new(nameof(IceWater), false); // TODO: set to true when working on again
	public static RoomSettings.RoomEffect.Type MurkyWater = new(nameof(MurkyWater), true);
	public static RoomSettings.RoomEffect.Type ReflectiveWater = new(nameof(ReflectiveWater), true);
	public static RoomSettings.RoomEffect.Type RGBElectricDeath = new(nameof(RGBElectricDeath), true);
	public static RoomSettings.RoomEffect.Type Suffocation = new(nameof(Suffocation), true);
	public static RoomSettings.RoomEffect.Type RainSiren = new(nameof(RainSiren), true);
	public static RoomSettings.RoomEffect.Type HSLDisplaySnow = new(nameof(HSLDisplaySnow), true);
	public static RoomSettings.RoomEffect.Type DenseFog = new(nameof(DenseFog), true);
	public static RoomSettings.RoomEffect.Type DenseFogSoundVolume = new(nameof(DenseFogSoundVolume), true);
	public static RoomSettings.RoomEffect.Type PolePlantColor = new(nameof(PolePlantColor), true);
	public static RoomSettings.RoomEffect.Type FlatFog = new(nameof(FlatFog), true);
	public static RoomSettings.RoomEffect.Type RainPaletteFade = new(nameof(RainPaletteFade), true);
	public static RoomSettings.RoomEffect.Type Rumbles = new(nameof(Rumbles), true);
	public static RoomSettings.RoomEffect.Type ReduceNeuronGlow = new(nameof(ReduceNeuronGlow), true);
	public static RoomSettings.RoomEffect.Type CloudsShadowSpeed = new(nameof(CloudsShadowSpeed), true);

	public static SoundID FT_Fog_PreDeath = new(nameof(FT_Fog_PreDeath), true);

	/// <summary>
	/// Effect category
	/// </summary>
	public static DevInterface.RoomSettingsPage.DevEffectsCategories RegionKit_Gameplay = new("RegionKit-Gameplay", true);
	public static DevInterface.RoomSettingsPage.DevEffectsCategories RegionKit_Decoration = new("RegionKit-Decoration", true);
}
