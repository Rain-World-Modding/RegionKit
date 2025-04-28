using DevInterface;
using EffExt;
using RegionKit.Modules.RoomSlideShow;

namespace RegionKit.Modules.Effects;

///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Effects")]
public static class _Module
{
	internal static void Setup()
	{
		PWMalfunction.Patch();


		EffectDefinitionBuilder builder = new EffectDefinitionBuilder("NonlethalWater");
		builder
			.SetUADFactory((room, data, firstTimeRealized) => new NonlethalWater(data))
			.SetCategory("RegionKit")
			.Register();

		RainWorld rainworld = CRW;
		MossWaterRGBBuilder.__RegisterBuilder();
		ReflectiveWaterBuilder.__RegisterBuilder();
		//IceWaterBuilder.__RegisterBuilder();
		RGBElectricDeathBuilder.__RegisterBuilder();
		RainSirenBuilder.__RegisterBuilder();
		SuffocationBuilder.__RegisterBuilder();
		HSLDisplaySnowBuilder.__RegisterBuilder();
		MossWaterUnlit.MossLoadResources(rainworld);
		MossWaterRGB.MossLoadResources(rainworld);
		MurkyWater.MurkyWaterLoadResources(rainworld);
		ReflectiveWater.ReflectiveLoadResources(rainworld);
		RGBElectricDeath.REDLoadResources(rainworld);
		HSLDisplaySnow.RDSLoadResources(rainworld);
		AlphaLevelShaderLoader.AlphaLevelLoad(rainworld);
		LegacyColoredSprite2.LegacyColoredSprite2Load(rainworld);
	}

	internal static void Enable()
	{
		ColorRoomEffect.Apply();
		ReplaceEffectColor.Apply();
		ReplaceCorruptionColors.Apply();
		HiveColorAlpha.Apply();
		RoomRainWithoutDeathRain.Apply();
		MossWaterUnlit.Apply();
		MossWaterRGB.Apply();
		ReflectiveWater.Apply();
		//IceWater.Apply();
		RGBElectricDeath.Apply();
		HSLDisplaySnow.Apply();
		MurkyWater.Apply();
		DenseFogHooks.Apply();
		SuffocationHooks.Apply();
		On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType += RoomSettingsPageDevEffectGetCategoryFromEffectType;
	}

	internal static void Disable()
	{
		ColorRoomEffect.Undo();
		ReplaceEffectColor.Undo();
		HiveColorAlpha.Undo();
		RoomRainWithoutDeathRain.Undo();
		MossWaterUnlit.Undo();
		MossWaterRGB.Undo();
		ReflectiveWater.Undo();
		//IceWater.Undo();
		RGBElectricDeath.Undo();
		HSLDisplaySnow.Undo();
		MurkyWater.Undo();
		DenseFogHooks.Undo();
		SuffocationHooks.Undo();
		On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType -= RoomSettingsPageDevEffectGetCategoryFromEffectType;
	}

	private static RoomSettingsPage.DevEffectsCategories RoomSettingsPageDevEffectGetCategoryFromEffectType(On.DevInterface.RoomSettingsPage.orig_DevEffectGetCategoryFromEffectType orig, RoomSettingsPage self, RoomSettings.RoomEffect.Type type)
	{
		RoomSettingsPage.DevEffectsCategories res = orig(self, type);
		if (type == _Enums.ReplaceCorruptionColor
			|| type == _Enums.ReplaceEffectColorA
			|| type == _Enums.ReplaceEffectColorB
			|| type == _Enums.FogOfWarSolid
			|| type == _Enums.FogOfWarDarkened

			|| type == _Enums.PWMalfunction
			|| type == _Enums.HiveColorAlpha
			|| type == _Enums.MossWater
			|| type == _Enums.MurkyWater
			|| type == _Enums.ReflectiveWater
			|| type == _Enums.DenseFog
			|| type == _Enums.DenseFogSoundVolume
			|| type == _Enums.RainSiren
			|| type == _Enums.Suffocation
			|| type == EchoExtender._Enums.EchoPresenceOverride)
			res = _Enums.RegionKit;
		return res;
	}
}
