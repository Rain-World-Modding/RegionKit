namespace RegionKit.Modules.TheMast;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "The Mast")]
internal static class _Module
{
	private static bool __appliedOnce = false;
	public static void Enable()
	{
		if (!__appliedOnce)
		{
			//DeerFix.Apply(); //deer can be allowed in rooms with room attractiveness now
			//ElectricGates.Apply();
			PearlChains.Apply();
			WindSystem.Apply();
			WormGrassFix.Apply();
		}
		__appliedOnce = true;
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
