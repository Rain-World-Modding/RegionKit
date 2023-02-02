namespace RegionKit.Modules.Effects;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Effects")]
internal static class _Module
{
	private static bool __appliedOnce = false;
	public static void Enable()
	{
		if (!__appliedOnce)
		{
			GlowingSwimmersCI.Apply();
			ReplaceEffectColor.Apply();
			PWMalfunction.Patch();
		}
		__appliedOnce = true;
		ColoredRoomEffect.Apply();
	}
	public static void Disable()
	{
		ColoredRoomEffect.Undo();
	}

	

}
