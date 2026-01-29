namespace RegionKit.Modules.Misc;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Miscellanceous")]
internal static class _Module
{

	public static void Setup()
	{
		_Enums.Register();
		ExtendedGates.InitExLocks();
	}
	public static void Enable()
	{

		//PaletteTextInput.Apply();
		SunBlockerFix.Apply();
		GhostEffectColorsFix.Apply();
		//CloudAdjustment.Apply();
		ExtendedGates.Enable();
		SuperstructureFusesHook.Apply();
		MoreFadePalettes.Apply();
		SlugcatRoomTemplates.Apply();
		RainSong.Enable();
		FadePaletteCombiner.Enable();
		DecalPreview.Enable();
		RegionConfigs.Apply();

		On.RoomPalette.GetColor += RoomPalette_GetColor;
	}

	public static void Disable()
	{
		//PaletteTextInput.Undo();
		SunBlockerFix.Undo();
		GhostEffectColorsFix.Undo();
		//CloudAdjustment.Undo();
		ExtendedGates.Disable();
		SuperstructureFusesHook.Undo();
		MoreFadePalettes.Undo();
		SlugcatRoomTemplates.Undo();
		RainSong.Disable();
		FadePaletteCombiner.Disable();
		DecalPreview.Disable();
		RegionConfigs.Undo();
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
