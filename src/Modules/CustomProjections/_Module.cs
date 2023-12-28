namespace RegionKit.Modules.CustomProjections;


[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "CustomProjections")]
public static class _Module
{
	public const string OVERSEER_POM_CATEGORY = RK_POM_CATEGORY + "-CustomProjections";
	internal static void Setup()
	{
		RegisterManagedObject<ReliableIggyEntrance, ReliableEntranceData, ReliableEntranceRep>("ReliableIggyEntrance", OVERSEER_POM_CATEGORY);
		RegisterManagedObject<CustomDoorPointer, DoorPointerData, DoorPointerRep>("ReliableDoorPointer", OVERSEER_POM_CATEGORY);
		LoadShaders();
	}

	internal static void Enable()
	{
		CustomProjections.Apply();
		ReliableIggyEntrance.Apply();
		CustomDoorPointer.Apply();
		OverseerProperties.Apply();
		PointerHooks.Apply();
		OverseerRecolor.Apply();
	}
	internal static void Disable()
	{
		CustomProjections.Undo();
		ReliableIggyEntrance.Undo();
		CustomDoorPointer.Undo();
		OverseerProperties.Undo();
		PointerHooks.Undo();
		OverseerRecolor.Undo();
	}

	public static void LoadShaders()
	{
		rainWorld.Shaders["HKHoloGrid"] = FShader.CreateShader("HKHoloGrid", AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/hkhologrid")).LoadAsset<Shader>("Assets/HKHoloGrid.shader"));
	}
}
