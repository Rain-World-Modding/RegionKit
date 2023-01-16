namespace RegionKit.Modules.Misc;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Miscellanceous")]
internal static class _Module
{
	private static bool __appliedOnce;
	public static void Enable()
	{
		//todo: wrap first
		CloudAdjustment.Apply();
		ExtendedGates.Enable();
		SunBlockerFix.Apply();
		SuperstructureFusesFix.Patch();
	}
	public static void Disable()
	{
		ExtendedGates.Disable();
		SuperstructureFusesFix.Disable();
	}
}
