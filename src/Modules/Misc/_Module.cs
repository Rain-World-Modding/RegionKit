namespace RegionKit.Modules.Misc;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Miscellanceous")]
internal static class _Module
{

	public static void Setup()
	{
	}
	public static void Enable()
	{

		//PaletteTextInput.Apply();
		SunBlockerFix.Apply();
		GhostEffectColorsFix.Apply();
		//CloudAdjustment.Apply();
		SuperstructureFusesHook.Apply();
		MoreFadePalettes.Apply();
		SlugcatRoomTemplates.Apply();
		RainSong.Enable();
		FadePaletteCombiner.Enable();
		DecalPreview.Enable();
		CustomSSSong.Enable();
		LightningFix.Apply();
		DecalSelectSearch.Apply();
		AntiPanelCollapse.Apply();

		On.RoomPalette.GetColor += RoomPalette_GetColor;
	}

	public static void Disable()
	{
		//PaletteTextInput.Undo();
		SunBlockerFix.Undo();
		GhostEffectColorsFix.Undo();
		//CloudAdjustment.Undo();
		SuperstructureFusesHook.Undo();
		MoreFadePalettes.Undo();
		SlugcatRoomTemplates.Undo();
		RainSong.Disable();
		FadePaletteCombiner.Disable();
		DecalPreview.Disable();
		CustomSSSong.Disable();
		LightningFix.Undo();
		DecalSelectSearch.Undo();
		AntiPanelCollapse.Undo();
	}

	private static Color RoomPalette_GetColor(On.RoomPalette.orig_GetColor orig, ref RoomPalette self, RoomPalette.ColorName colorName)
	{
		if (colorName == _Enums.EffectColor1)
		{
			return self.texture.GetPixel(30, 5);
		}
		else if (colorName == _Enums.EffectColor2)
		{
			return self.texture.GetPixel(30, 3);
		}
		else if (colorName == _Enums.White)
		{
			return Color.white;
		}
		return orig(ref self, colorName);
	}
}
