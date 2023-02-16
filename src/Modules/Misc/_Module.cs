namespace RegionKit.Modules.Misc;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Miscellanceous")]
internal static class _Module
{
	private static bool __appliedOnce;
	public static void Enable()
	{
		if (!__appliedOnce)
		{
			_Enums.Register();
			//CustomArenaDivisions.Patch
			//CustomArenaDivisions.
		}
		__appliedOnce = true;
		//PaletteTextInput.Apply();
		SunBlockerFix.Apply();
		CloudAdjustment.Apply();
		ExtendedGates.Enable();
<<<<<<< Updated upstream
		SuperstructureFusesHook.Apply();
=======
		SuperstructureFusesFix.Patch();
>>>>>>> Stashed changes
		
	}
	public static void Disable()
	{
		//PaletteTextInput.Undo();
		SunBlockerFix.Undo();
		CloudAdjustment.Undo();
		ExtendedGates.Disable();
<<<<<<< Updated upstream
		SuperstructureFusesHook.Undo();
=======
		SuperstructureFusesFix.Disable();
>>>>>>> Stashed changes
	}
}
