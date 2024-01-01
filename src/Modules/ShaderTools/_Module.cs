namespace RegionKit.Modules.ShaderTools {
	[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Shader Tools")]
	public static class _Module {
		/// <summary>
		/// Applies hooks.
		/// </summary>
		public static void Enable() {
			ShaderBuffers.Initialize();
		}

		/// <summary>
		/// Undoes hooks.
		/// </summary>
		public static void Disable() {
			// This probably should *not* be implemented.
			// Editing the RT's bits has no adverse effects:
			// 1. Depth doesn't work because *Unity* only renders the game's RT, and nothing else.
			// 2. The stencil buffer is never set by any native shaders, meaning it is all 0 anyway by default.
			
			// And in exchange, disabling the buffer might brick compatibility with other mods that 
			// enable it on their own, by unexpectedly disabling it when they might be trying to use it.
		}
	}
}
