namespace RegionKit.Modules.ConcealedGarden;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "The Mast")]
internal static class _Module
{
	private static bool __appliedOnce = false;
	public static void Enable()
	{
		if (!__appliedOnce)
		{
			CGDrySpot.Register();
			CGFourthLayerFix.Apply();
			CGGateCustomization.Register();
		}
		__appliedOnce = true;
	}
	public static void Disable()
	{

	}
}
