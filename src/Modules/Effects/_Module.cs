using DevInterface;
using EffExt;
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
	}
	internal static void Enable()
	{
		GlowingSwimmersCI.Apply();
		ColoredCamoBeetlesCI.Apply();
		MosquitoInsectsCI.Apply();
		ColorRoomEffect.Apply();
		ReplaceEffectColor.Apply();
		HiveColorAlpha.Apply();
		RoomRainWithoutDeathRain.Apply();
		MossWaterUnlit.Apply();
		MossWaterRGB.Apply();

		RainWorld rainworld = CRW;
		MossWaterRGBBuilder.__RegisterBuilder();
		ReflectiveWaterBuilder.__RegisterBuilder();
		IceWaterBuilder.__RegisterBuilder();
		MossWaterUnlit.MossLoadResources(rainworld);
		MossWaterRGB.MossLoadResources(rainworld);
		MurkyWater.MurkyWaterLoadResources(rainworld);
		ReflectiveWater.ReflectiveLoadResources(rainworld);


		ReflectiveWater.Apply();
		IceWater.Apply();
		MurkyWater.Apply();
		On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType += RoomSettingsPageDevEffectGetCategoryFromEffectType;
	}

	internal static void Disable()
	{
		GlowingSwimmersCI.Undo();
		ColoredCamoBeetlesCI.Undo();
		MosquitoInsectsCI.Undo();
		ColorRoomEffect.Undo();
		ReplaceEffectColor.Undo();
		HiveColorAlpha.Undo();
		RoomRainWithoutDeathRain.Undo();
		MossWaterUnlit.Undo();
		MossWaterRGB.Undo();
		ReflectiveWater.Undo();
		IceWater.Undo();
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
