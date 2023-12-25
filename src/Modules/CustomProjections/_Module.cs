namespace RegionKit.Modules.CustomProjections;


[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "CustomProjections")]
public static class _Module
{
	internal static void Setup()
	{
	}

	internal static void Enable()
	{
		CustomProjections.Apply();
	}
	internal static void Disable()
	{
	}
}
