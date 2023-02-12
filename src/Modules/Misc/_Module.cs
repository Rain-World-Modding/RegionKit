namespace RegionKit.Modules.Misc;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Miscellanceous")]
internal static class _Module
{
	private static bool __appliedOnce;
	public static void Enable()
	{
		if (!__appliedOnce)
		{
			SunBlockerFix.Apply();
			_Enums.Register();
			//CustomArenaDivisions.Patch
			//CustomArenaDivisions.
		}
		__appliedOnce = true;
		//PaletteTextInput.Apply();
		CloudAdjustment.Apply();
		ArenaFixes.ApplyHooks();
		ExtendedGates.Enable();
		SuperstructureFusesFix.Patch();
		PaletteTextInput.Apply();
		SettingsPathDisplay.Apply();
		
	}
	public static void Disable()
	{
		//PaletteTextInput.Undo();
		CloudAdjustment.Undo();
		ArenaFixes.UndoHooks();
		ExtendedGates.Disable();
		SuperstructureFusesFix.Disable();
		PaletteTextInput.Undo();
		SettingsPathDisplay.Undo();
	}
}
