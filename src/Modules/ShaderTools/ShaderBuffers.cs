using System;
using MonoMod.Cil;
using UnityEngine;

namespace RegionKit.Modules.ShaderTools {
	public static class ShaderBuffers {

		/// <summary>
		/// The amount of bits needed to activate the stencil buffer.
		/// </summary>
		private const int DEPTH_AND_STENCIL_BUFFER_BITS = 24;
		
		private static bool _hasStencilBuffer = false;

		internal static void Initialize() {
			IL.FScreen.ctor += ReplaceRTDepth;
			IL.FScreen.ReinitRenderTexture += ReplaceRTDepth;

			_hasStencilBuffer = true;
			if (Futile.screen != null)
			{
				Futile.instance.UpdateScreenWidth(Futile.screen.pixelWidth);
			}
		}

		internal static void Uninitialize() {
			IL.FScreen.ctor -= ReplaceRTDepth;
			IL.FScreen.ReinitRenderTexture -= ReplaceRTDepth;

			// DO NOT set rt.depth = 0 here or you will brick any mods that (sensibly) expect their changes to the value to be kept.
			// Let RW wipe it on its own when it rebuilds the RT.
			_hasStencilBuffer = false;
		}

		private static void ReplaceRTDepth(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.AfterLabel, x => x.MatchNewobj<RenderTexture>());
			c.EmitDelegate(ReplaceDepth);
		}

		private static int ReplaceDepth(int oldDepth)
		{
			// Use this check in case another mod happens to enable the 32 bit buffer for whatever reason.
			return (_hasStencilBuffer && oldDepth < DEPTH_AND_STENCIL_BUFFER_BITS) ? DEPTH_AND_STENCIL_BUFFER_BITS : oldDepth;
		}
	}
}
