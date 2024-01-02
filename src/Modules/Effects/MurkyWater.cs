using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Modules.Effects
{
	internal class MurkyWater
	{
		static bool loaded = false;
		public static RoomSettings.RoomEffect.Type NoLitWater;
		public static RoomSettings.RoomEffect.Type DarkWater;

		// By ASlightlyOvergrownCactus, big thx to Xan for help with replacement shaders
		internal static void Apply()
		{
			On.Lantern.InitiateSprites += Lantern_InitiateSprites;
			On.Water.InitiateSprites += Water_InitiateSprites;
			On.Water.DrawSprites += Water_DrawSprites;
		}

		private static void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (rCam.room.roomSettings.GetEffect(_Enums.MurkyWater) != null)
			{
				float amount = rCam.room.roomSettings.GetEffectAmount(_Enums.MurkyWater);
				Shader.SetGlobalFloat("_Amount", amount);
			}
		}

		internal static void Undo()
		{
			On.Lantern.InitiateSprites += Lantern_InitiateSprites;
			On.Water.InitiateSprites += Water_InitiateSprites;
			
		}

		private static void Water_InitiateSprites(On.Water.orig_InitiateSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			if (rCam.room.roomSettings.GetEffect(_Enums.MurkyWater) != null)
				sLeaser.sprites[1].shader = self.room.game.rainWorld.Shaders["DarkWater"];
		}

		private static void Lantern_InitiateSprites(On.Lantern.orig_InitiateSprites orig, Lantern self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			if (rCam.room.roomSettings.GetEffect(_Enums.MurkyWater) != null)
				sLeaser.sprites[3].shader = self.room.game.rainWorld.Shaders["NoLitWater"];
		}

		public static void MurkyWaterLoadResources(RainWorld rw)
		{
			if (!loaded)
			{
				LogMessage("entered loading / loading status: " + loaded);
				loaded = true;
				if (MossWaterUnlit.mossBundle != null)
				{
					rw.Shaders["NoLitWater"] = FShader.CreateShader("NoLitWater", MossWaterUnlit.mossBundle.LoadAsset<Shader>("Assets/shaders 1.9.03/NoLitWater.shader"));
					rw.Shaders["DarkWater"] = FShader.CreateShader(("DarkWater"), MossWaterUnlit.mossBundle.LoadAsset<Shader>("Assets/shaders 1.9.03/DarkWater.shader"));
					NoLitWater = new(nameof(NoLitWater), true);
					DarkWater = new(nameof(DarkWater), true);
				}
				else
				{
					LogMessage("MurkyWater must be loaded after MossWaterUnlit!");
				}
			}
		}
	}

	public static class ShaderBuffers
	{

		/// <summary>
		/// The amount of bits needed to activate the stencil buffer.
		/// </summary>
		private const int DEPTH_AND_STENCIL_BUFFER_BITS = 24;

		private static bool _hasStencilBuffer = false;

		internal static void Initialize()
		{
			On.FScreen.ctor += OnConstructingFScreen;
			On.FScreen.ReinitRenderTexture += OnReinitializeRT;
			_hasStencilBuffer = true;
			if (Futile.screen != null)
			{
				RenderTexture rt = Futile.screen.renderTexture;
				if (rt.depth < DEPTH_AND_STENCIL_BUFFER_BITS)
				{
					// Use this check in case another mod happens to enable the 32 bit buffer for whatever reason.
					Debug.Log("setting the render texture depth thingy");
					rt.Release();
					rt.depth = DEPTH_AND_STENCIL_BUFFER_BITS;
				}
			}
		}

		internal static void Uninitialize()
		{
			On.FScreen.ctor -= OnConstructingFScreen;
			On.FScreen.ReinitRenderTexture -= OnReinitializeRT;
			// DO NOT set rt.depth = 0 here or you will brick any mods that (sensibly) expect their changes to the value to be kept.
			// Let RW wipe it on its own when it rebuilds the RT.
			_hasStencilBuffer = false;
		}

		private static void OnReinitializeRT(On.FScreen.orig_ReinitRenderTexture originalMethod, FScreen @this, int displayWidth)
		{
			originalMethod(@this, displayWidth);
			// Use this check in case another mod happens to enable the 32 bit buffer for whatever reason.
			@this.renderTexture.depth = (_hasStencilBuffer && @this.renderTexture.depth < DEPTH_AND_STENCIL_BUFFER_BITS) ? DEPTH_AND_STENCIL_BUFFER_BITS : @this.renderTexture.depth;
		}

		private static void OnConstructingFScreen(On.FScreen.orig_ctor originalCtor, FScreen @this, FutileParams futileParams)
		{
			originalCtor(@this, futileParams);
			// Use this check in case another mod happens to enable the 32 bit buffer for whatever reason.
			@this.renderTexture.depth = (_hasStencilBuffer && @this.renderTexture.depth < DEPTH_AND_STENCIL_BUFFER_BITS) ? DEPTH_AND_STENCIL_BUFFER_BITS : @this.renderTexture.depth;
		}

	}
}
