namespace RegionKit.Modules.Misc;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Miscellanceous")]
internal static class _Module
{
	private static bool __appliedOnce;
	public static void Enable()
	{
		if (!__appliedOnce)
		{
			//CloudAdjustment.Apply();
			SunBlockerFix.Apply();
		}
		ExtendedGates.Enable();
		SuperstructureFusesFix.Patch();
		
	}
	public static void Disable()
	{
		ExtendedGates.Disable();
		SuperstructureFusesFix.Disable();
	}
}
