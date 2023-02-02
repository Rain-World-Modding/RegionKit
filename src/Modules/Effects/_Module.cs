namespace RegionKit.Modules.Effects;

///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Effects")]
public static class _Module
{
	private static bool __appliedOnce = false;
	internal static void Enable()
	{
		if (!__appliedOnce)
		{
			GlowingSwimmersCI.Apply();
			PWMalfunction.Patch();
		}
		__appliedOnce = true;
		ColorRoomEffect.Apply();
		ReplaceEffectColor.Apply();
	}
	internal static void Disable()
	{
		ColorRoomEffect.Undo();
		ReplaceEffectColor.Undo();
	}

	

}
