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
	}
}
