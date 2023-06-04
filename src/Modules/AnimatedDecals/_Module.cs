namespace RegionKit.Modules.AnimatedDecals
{
	/// <summary>
	/// Allows animated textures to be used instead of static images for decals.
	/// </summary>
	[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Animated Decals")]
	public static class _Module
	{
		/// <summary>
		/// Applies hooks.
		/// </summary>
		public static void Enable()
		{
			VideoManager.Enable();
			Decals.Enable();
		}

		/// <summary>
		/// Undoes hooks.
		/// </summary>
		public static void Disable()
		{
			VideoManager.Disable();
			Decals.Disable();
		}
	}
}
