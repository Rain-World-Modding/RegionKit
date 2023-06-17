namespace RegionKit.Modules.TheMast;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "The Mast")]
internal static class _Module
{
	public static void Setup()
	{
		PearlChains.Apply();
		WindSystem.Apply();
		WormGrassFix.Apply();
	}
	public static void Enable()
	{
		//ArenaBackgrounds.Apply(); //assigned with AboveCloudsView slider now
		//BetterClouds.Apply(); //replaced by BackgroundBuilder
		RainThreatFix.Apply();
		SkyDandelionBgFix.Apply();
	}
	public static void Disable()
	{
		//ArenaBackgrounds.Undo();
		//BetterClouds.Undo();
		RainThreatFix.Undo();
		SkyDandelionBgFix.Undo();
	}
}
