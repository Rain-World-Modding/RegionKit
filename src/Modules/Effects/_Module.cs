namespace RegionKit.Modules.Effects;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Effects")]
internal static class _Module
{
	private static bool _appliedOnce = false;
	public static void Enable()
	{
		if (!_appliedOnce)
		{
			ColoredRoomEffect.Apply();
			FogOfWar.Patch();
			GlowingSwimmersCI.Apply();
			ReplaceEffectColor.Apply();
		}
		_appliedOnce = true;
	}
	public static void Disable()
	{

	}

}
