using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Modules.Effects
{
	internal class TestEffect
	{
		static bool loaded = false;
		private static AssetBundle testBundle;
		internal static void Apply()
		{
			On.Spear.InitiateSprites += Spear_InitiateSprites;
			On.Spear.DrawSprites += Spear_DrawSprites;
			On.Spear.ApplyPalette += Spear_ApplyPalette;
		}


		internal static void Undo()
		{
			On.Spear.InitiateSprites -= Spear_InitiateSprites;
			On.Spear.DrawSprites -= Spear_DrawSprites;
			On.Spear.ApplyPalette -= Spear_ApplyPalette;
		}

		public static void LoadResources(RainWorld rw)
		{
			if (!loaded)
			{
				loaded = true;
				testBundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/test"));
				rw.Shaders["TestShader"] = FShader.CreateShader("TestShader", testBundle.LoadAsset<Shader>("Assets/shaders 1.9.03/TestShader.shader"));
				Shader.SetGlobalColor("_TestColor", Color.green);
			}
		}


		private static void Spear_ApplyPalette(On.Spear.orig_ApplyPalette orig, Spear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);
		}

		private static void Spear_DrawSprites(On.Spear.orig_DrawSprites orig, Spear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (self.room.roomSettings.GetEffect(_Enums.TestEffect) != null)
			{
				sLeaser.sprites[sLeaser.sprites.Length - 1].SetPosition(Vector2.Lerp(self.bodyChunks[0].lastPos, self.bodyChunks[0].pos, timeStacker) - camPos);
				sLeaser.sprites[sLeaser.sprites.Length - 1].isVisible = true;
			}
		}

		private static void Spear_InitiateSprites(On.Spear.orig_InitiateSprites orig, Spear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			if (self.room.roomSettings.GetEffect(_Enums.TestEffect) != null)
			{
				int index = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
				sLeaser.sprites[index] = new FSprite("Circle20");
				sLeaser.sprites[index].shader = self.room.game.rainWorld.Shaders["TestShader"];
			}
		}
	}
}
