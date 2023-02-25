namespace RegionKit.Modules.Effects;

///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Effects")]
public static class _Module
{
	private static bool __appliedOnce = false;

	internal static void Enable()
	{
		if (!__appliedOnce)
			PWMalfunction.Patch();
		__appliedOnce = true;
		GlowingSwimmersCI.Apply();
		ColoredCamoBeetlesCI.Apply();
		ColorRoomEffect.Apply();
		ReplaceEffectColor.Apply();
	}
	internal static void Disable()
	{
		GlowingSwimmersCI.Undo();
		ColoredCamoBeetlesCI.Undo();
		ColorRoomEffect.Undo();
		ReplaceEffectColor.Undo();
	}
}
