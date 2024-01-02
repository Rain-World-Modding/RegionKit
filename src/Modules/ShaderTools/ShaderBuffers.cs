using System;
using UnityEngine;

namespace RegionKit.Modules.ShaderTools {
	public static class ShaderBuffers {

		/// <summary>
		/// The amount of bits needed to activate the stencil buffer.
		/// </summary>
		private const int DEPTH_AND_STENCIL_BUFFER_BITS = 24;
		
		private static bool _hasStencilBuffer = false;

		internal static void Initialize() {
			On.FScreen.ctor += OnConstructingFScreen;
			On.FScreen.ReinitRenderTexture += OnReinitializeRT;
			_hasStencilBuffer = true;
			if (Futile.screen != null) {
				RenderTexture rt = Futile.screen.renderTexture;
				if (rt.depth < DEPTH_AND_STENCIL_BUFFER_BITS) {
					// Use this check in case another mod happens to enable the 32 bit buffer for whatever reason.
					rt.Release();
					rt.depth = DEPTH_AND_STENCIL_BUFFER_BITS;
				}
			}
		}

		internal static void Uninitialize() {
			On.FScreen.ctor -= OnConstructingFScreen;
			On.FScreen.ReinitRenderTexture -= OnReinitializeRT;
			// DO NOT set rt.depth = 0 here or you will brick any mods that (sensibly) expect their changes to the value to be kept.
			// Let RW wipe it on its own when it rebuilds the RT.
			_hasStencilBuffer = false;
		}
		
		private static void OnReinitializeRT(On.FScreen.orig_ReinitRenderTexture originalMethod, FScreen @this, int displayWidth) {
			originalMethod(@this, displayWidth);
			@this.renderTexture.Release();
			// Use this check in case another mod happens to enable the 32 bit buffer for whatever reason.
			int newDepth = (_hasStencilBuffer && @this.renderTexture.depth < DEPTH_AND_STENCIL_BUFFER_BITS) ? DEPTH_AND_STENCIL_BUFFER_BITS : @this.renderTexture.depth;
			if (@this.renderTexture.depth != newDepth) {
				@this.renderTexture.Release();
				@this.renderTexture.depth = newDepth;
			}			
		}

		private static void OnConstructingFScreen(On.FScreen.orig_ctor originalCtor, FScreen @this, FutileParams futileParams) {
			originalCtor(@this, futileParams);
			// Use this check in case another mod happens to enable the 32 bit buffer for whatever reason.
			int newDepth = (_hasStencilBuffer && @this.renderTexture.depth < DEPTH_AND_STENCIL_BUFFER_BITS) ? DEPTH_AND_STENCIL_BUFFER_BITS : @this.renderTexture.depth;
			if (@this.renderTexture.depth != newDepth)
			{
				@this.renderTexture.Release();
				@this.renderTexture.depth = newDepth;
			}
		}

	}
}
