using DevInterface;
using EffExt;
using RegionKit.Modules.RoomSlideShow;
using RegionKit.Modules.ShaderTools;

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
		HSLDisplaySnowBuilder.__RegisterBuilder();
		MossWaterUnlit.MossLoadResources(rainworld);
		MossWaterRGB.MossLoadResources(rainworld);
		MurkyWater.MurkyWaterLoadResources(rainworld);
		ReflectiveWater.ReflectiveLoadResources(rainworld);
		RGBElectricDeath.REDLoadResources(rainworld);
		HSLDisplaySnow.RDSLoadResources(rainworld);
		AlphaLevelShaderLoader.AlphaLevelLoad(rainworld);
	}

	internal static void Enable()
	{
		GlowingSwimmersCI.Apply();
		ColoredCamoBeetlesCI.Apply();
		MosquitoInsectsCI.Apply();
		ButterfliesCI.Apply();
		ZippersCI.Apply();
		
		ColorRoomEffect.Apply();
		ReplaceEffectColor.Apply();
		HiveColorAlpha.Apply();
		RoomRainWithoutDeathRain.Apply();
		MossWaterUnlit.Apply();
		MossWaterRGB.Apply();



		ReflectiveWater.Apply();
		IceWater.Apply();
		RGBElectricDeath.Apply();
		HSLDisplaySnow.Apply();
		MurkyWater.Apply();
		On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType += RoomSettingsPageDevEffectGetCategoryFromEffectType;
	}

	internal static void Disable()
	{
		GlowingSwimmersCI.Undo();
		ColoredCamoBeetlesCI.Undo();
		MosquitoInsectsCI.Undo();
		ButterfliesCI.Undo();
		ZippersCI.Undo();

		ColorRoomEffect.Undo();
		ReplaceEffectColor.Undo();
		HiveColorAlpha.Undo();
		RoomRainWithoutDeathRain.Undo();
		MossWaterUnlit.Undo();
		MossWaterRGB.Undo();
		ReflectiveWater.Undo();
		IceWater.Undo();
		RGBElectricDeath.Undo();
		HSLDisplaySnow.Undo();
		MurkyWater.Undo();
		On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType -= RoomSettingsPageDevEffectGetCategoryFromEffectType;
	}

	private static RoomSettingsPage.DevEffectsCategories RoomSettingsPageDevEffectGetCategoryFromEffectType(On.DevInterface.RoomSettingsPage.orig_DevEffectGetCategoryFromEffectType orig, RoomSettingsPage self, RoomSettings.RoomEffect.Type type)
	{
		RoomSettingsPage.DevEffectsCategories res = orig(self, type);
		if (type == _Enums.ReplaceEffectColorA ||
			type == _Enums.ReplaceEffectColorB ||
			type == _Enums.FogOfWarSolid ||
			type == _Enums.FogOfWarDarkened ||
			type == _Enums.GlowingSwimmers ||
			type == _Enums.ColoredCamoBeetles ||
			type == _Enums.MosquitoInsects ||
			type == _Enums.ButterfliesA ||
			type == _Enums.ButterfliesB ||
			type == _Enums.Zippers ||
			type == _Enums.PWMalfunction ||
			type == _Enums.HiveColorAlpha ||
			type == _Enums.MossWater ||
			type == _Enums.MurkyWater ||
			type == _Enums.ReflectiveWater ||
			type == EchoExtender._Enums.EchoPresenceOverride)
			res = _Enums.RegionKit;
		return res;
	}
}
