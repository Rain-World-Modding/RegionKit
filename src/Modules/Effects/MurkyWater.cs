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

				}
				else
				{
					LogMessage("MurkyWater must be loaded after MossWaterUnlit!");
				}
			}
		}
	}
}
