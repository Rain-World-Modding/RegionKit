namespace RegionKit.Modules.IndividualPlacedObjectViewer;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "IndividualPlacedObjectViewer")]
internal static class _Module
{
	public static void Enable()
	{
		IndividualPlacedObjectViewer.Enable();
	}

	public static void Disable()
	{
		IndividualPlacedObjectViewer.Disable();
	}
}
