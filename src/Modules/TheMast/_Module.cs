namespace RegionKit.Modules.TheMast;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "The Mast")]
internal static class _Module
{
	private static bool __appliedOnce = false;
	public static void Enable()
	{
		if (!__appliedOnce)
		{
			ArenaBackgrounds.Apply();
			BetterClouds.Apply();
			DeerFix.Apply();
			ElectricGates.Apply();
			PearlChains.Apply();
			RainThreatFix.Apply();
			SkyDandelionBgFix.Apply();
			WindSystem.Apply();
			WormGrassFix.Apply();
		}
		__appliedOnce = true;
	}
	public static void Disable()
	{

	}
}
