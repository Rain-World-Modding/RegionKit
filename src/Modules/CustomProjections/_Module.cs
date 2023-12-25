namespace RegionKit.Modules.CustomProjections;


[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "CustomProjections")]
public static class _Module
{
	public const string OVERSEER_POM_CATEGORY = RK_POM_CATEGORY + "-CustomProjections";
	internal static void Setup()
	{
		RegisterManagedObject<ReliableIggyEntrance, ReliableEntranceData, ReliableEntranceRep>("ReliableIggyEntrance", OVERSEER_POM_CATEGORY);
	}

	internal static void Enable()
	{
		CustomProjections.Apply();
		ReliableIggyEntrance.Apply();
	}
	internal static void Disable()
	{
		CustomProjections.Undo();
		ReliableIggyEntrance.Undo();
	}
}
