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
		PaletteEffectColorBuilder.__RegisterBuilder();
		MossWaterUnlit.MossLoadResources(rainworld);
		MossWaterRGB.MossLoadResources(rainworld);
		MurkyWater.MurkyWaterLoadResources(rainworld);
		ReflectiveWater.ReflectiveLoadResources(rainworld);
		RGBElectricDeath.REDLoadResources(rainworld);
		HSLDisplaySnow.RDSLoadResources(rainworld);
		AlphaLevelShaderLoader.AlphaLevelLoad(rainworld);
		LegacyColoredSprite2.LegacyColoredSprite2Load(rainworld);
		//LocustSwarmBuilder.__RegisterBuilder();
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
		PolePlantColor.Apply();
		FlatFog.Apply();

		On.DevInterface.AddEffectButton.Clicked += AddEffectButton_Clicked;
		On.DevInterface.EffectPanel.EffectPanelSlider.NubDragged += EffectPanelSlider_NubDragged;

		On.RoomSettings.RoomEffect.GetSliderCount += RoomEffect_GetSliderCount;
		On.RoomSettings.RoomEffect.GetSliderName += RoomEffect_GetSliderName;
		On.RoomSettings.RoomEffect.GetSliderDefault += RoomEffect_GetSliderDefault;
		On.DevInterface.EffectPanel.EffectPanelSlider.Refresh += EffectPanelSlider_Refresh;

		On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType += RoomSettingsPageDevEffectGetCategoryFromEffectType;

		LoadShaders();
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
		PolePlantColor.Undo();
		FlatFog.Undo();

		On.DevInterface.AddEffectButton.Clicked -= AddEffectButton_Clicked;
		On.DevInterface.EffectPanel.EffectPanelSlider.NubDragged -= EffectPanelSlider_NubDragged;

		On.RoomSettings.RoomEffect.GetSliderCount -= RoomEffect_GetSliderCount;
		On.RoomSettings.RoomEffect.GetSliderName -= RoomEffect_GetSliderName;
		On.RoomSettings.RoomEffect.GetSliderDefault -= RoomEffect_GetSliderDefault;
		On.DevInterface.EffectPanel.EffectPanelSlider.Refresh -= EffectPanelSlider_Refresh;

		On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType -= RoomSettingsPageDevEffectGetCategoryFromEffectType;
	}

	private static readonly HashSet<RoomSettings.RoomEffect.Type> TypesToRefreshPalette = new HashSet<RoomSettings.RoomEffect.Type>()
	{
		// Vanilla effects that require a palette refresh
		RoomSettings.RoomEffect.Type.SkyBloom,
		RoomSettings.RoomEffect.Type.SkyAndLightBloom,
		RoomSettings.RoomEffect.Type.LightBurn,
		RoomSettings.RoomEffect.Type.Lightning,
		RoomSettings.RoomEffect.Type.Fog,
		RoomSettings.RoomEffect.Type.Bloom,
		RoomSettings.RoomEffect.Type.VoidMelt,

		// Effects from RegionKit
		_Enums.FlatFog
	};

	private static void AddEffectButton_Clicked(On.DevInterface.AddEffectButton.orig_Clicked orig, AddEffectButton self)
	{
		orig(self);
		if (TypesToRefreshPalette.Contains(self.type))
		{
			// Refresh palette
			self.owner.room.game.cameras[0].ApplyPalette();
		}
	}

	private static void EffectPanelSlider_NubDragged(On.DevInterface.EffectPanel.EffectPanelSlider.orig_NubDragged orig, EffectPanel.EffectPanelSlider self, float nubPos)
	{
		float oldEffectAmount = self.effect.amount;
		orig(self, nubPos);
		if (!self.inheritButton && oldEffectAmount == 0f && nubPos > 0f && TypesToRefreshPalette.Contains(self.effect.type))
		{
			self.owner.room.game.cameras[0].ApplyPalette();
		}
	}

	private static int RoomEffect_GetSliderCount(On.RoomSettings.RoomEffect.orig_GetSliderCount orig, RoomSettings.RoomEffect.Type type)
	{
		if (type == _Enums.FlatFog)
		{
			return 4;
		}
		return orig(type);
	}

	private static string RoomEffect_GetSliderName(On.RoomSettings.RoomEffect.orig_GetSliderName orig, RoomSettings.RoomEffect.Type type, int index)
	{
		if (type == _Enums.FlatFog)
		{
			return index switch
			{
				0 => "Fade start depth",
				1 => "Fade end depth",
				2 => "Start intensity",
				3 => "End intensity",
				_ => "INVALID"
			};
		}
		return orig(type, index);
	}

	private static float RoomEffect_GetSliderDefault(On.RoomSettings.RoomEffect.orig_GetSliderDefault orig, RoomSettings.RoomEffect.Type type, int index)
	{
		if (type == _Enums.FlatFog)
		{
			return index switch
			{
				0 => 0f,
				1 => 1f,
				2 => 0f,
				3 => 0.5f,
				_ => 0f
			};
		}
		return orig(type, index);
	}

	private static void EffectPanelSlider_Refresh(On.DevInterface.EffectPanel.EffectPanelSlider.orig_Refresh orig, EffectPanel.EffectPanelSlider self)
	{
		orig(self);
		// can customize effect slider text here
	}

	private static RoomSettingsPage.DevEffectsCategories RoomSettingsPageDevEffectGetCategoryFromEffectType(On.DevInterface.RoomSettingsPage.orig_DevEffectGetCategoryFromEffectType orig, RoomSettingsPage self, RoomSettings.RoomEffect.Type type)
	{
		// Gameplay
		if (type == _Enums.FogOfWarSolid
			|| type == _Enums.FogOfWarDarkened
			|| type == _Enums.PWMalfunction
			|| type == _Enums.DenseFog
			|| type == _Enums.DenseFogSoundVolume
			|| type == _Enums.RainSiren
			|| type == _Enums.Suffocation)
		{
			return _Enums.RegionKit_Gameplay;
		}

		// Decoration
		if (type == _Enums.ReplaceCorruptionColor
			|| type == _Enums.ReplaceEffectColorA
			|| type == _Enums.ReplaceEffectColorB
			|| type == _Enums.PolePlantColor
			|| type == _Enums.HiveColorAlpha
			|| type == _Enums.MossWater
			|| type == _Enums.MurkyWater
			|| type == _Enums.ReflectiveWater
			|| type == EchoExtender._Enums.EchoPresenceOverride
			|| type == _Enums.FlatFog)
		{
			return _Enums.RegionKit_Decoration;
		}

		return orig(self, type);
	}

	private static void LoadShaders()
	{
		AssetBundle bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/rkeffects"));
		Custom.rainWorld.Shaders["RKFlatFog"] = FShader.CreateShader("RKFlatFog", bundle.LoadAsset<Shader>("Assets/Shaders/RKFlatFog.shader"));
	}
}
