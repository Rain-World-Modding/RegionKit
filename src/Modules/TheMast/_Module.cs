namespace RegionKit.Modules.TheMast;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "The Mast")]
internal static class _Module
{
	private static bool _ranOnce = false;
	public static void Enable()
	{
		if (!_ranOnce)
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
		_ranOnce = true;
	}
	public static void Disable()
	{

	}
}
