namespace RegionKit.Modules.ConcealedGarden;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "The Mast")]
internal static class _Module
{
	private static bool _ranOnce = false;
	public static void Enable()
	{
		if (!_ranOnce)
		{
			CGDrySpot.Register();
			CGFourthLayerFix.Apply();
			CGGateCustomization.Register();
		}
		_ranOnce = true;
	}
	public static void Disable()
	{

	}
}
