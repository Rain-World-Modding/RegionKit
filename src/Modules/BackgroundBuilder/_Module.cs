namespace RegionKit.Modules.BackgroundBuilder;


[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "BackgroundBuilder")]
public static class _Module
{
	public const string BGPath = "Assets\\RegionKit\\Backgrounds";
	internal static void Enable()
	{
		Data.Apply();
		Init.Apply();
		BuilderPageHooks.Apply();
		ExceptionFixes.Apply();
		BackgroundUpdates.Apply();
		Registry.InitSceneMakerRegistry();
		Registry.InitElementFactoryRegistry();
	}
	internal static void Disable()
	{
		Data.Undo();
		Init.Undo();
		BuilderPageHooks.Undo();
		ExceptionFixes.Undo();
		BackgroundUpdates.Undo();
	}

}
