namespace RegionKit.Modules.Effects;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Effects")]
internal static class _Module
{
	private static bool __appliedOnce = false;
	public static void Enable()
	{
		
		if (!__appliedOnce)
		{
			ColoredRoomEffect.Apply();
			//FogOfWar.Patch();
			GlowingSwimmersCI.Apply();
			ReplaceEffectColor.Apply();
			PWMalfunction.Patch();
		}
		__appliedOnce = true;
	}
	public static void Disable()
	{

	}

	

}
