namespace RegionKit.Modules.ExtendedGates
{
	[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "ExtendedGates")]
	internal static class _Module
	{
		internal static void Setup()
		{
			_Enums.Register();
			ExtendedGates.InitExLocks();
		}

		internal static void Enable()
		{
			ExtendedGates.Enable();
		}

		internal static void Disable()
		{
			ExtendedGates.Disable();
		}
	}
}
