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
		//CloudAdjustment.Apply();
		ExtendedGates.Enable();
		SuperstructureFusesHook.Apply();
	}
	public static void Disable()
	{
		//PaletteTextInput.Undo();
		SunBlockerFix.Undo();
		//CloudAdjustment.Undo();
		ExtendedGates.Disable();
		SuperstructureFusesHook.Undo();
	}
}
