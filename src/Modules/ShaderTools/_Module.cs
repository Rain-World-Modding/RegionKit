using System;

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
			ShaderBuffers.Uninitialize();
		}
	}
}
