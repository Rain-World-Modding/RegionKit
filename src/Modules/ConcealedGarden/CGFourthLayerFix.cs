using System;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;

internal class CGFourthLayerFix
{
	internal static void Apply()
	{
		On.PersistentData.ctor += PersistentData_ctor;
		On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_Int;
	}

	static private void RoomCamera_MoveCamera_Room_Int(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera self, Room newRoom, int camPos)
	{
		orig(self, newRoom, camPos);

		if (self.bkgwww == null)
		{
			string text = WorldLoader.FindRoomFile(newRoom.abstractRoom.name, true, $"{camPos + 1}_bkg.png");
			if (!System.IO.File.Exists(text)) return;
			Uri uri = new Uri(text);
			if (uri.IsFile && System.IO.File.Exists(uri.LocalPath))
			{
				LogMessage("RoomCamera_MoveCamera loading bkg img from: " + text);
				#pragma warning disable CS0618 //WWW is obsolete
				self.bkgwww = new WWW(text);
				#pragma warning restore CS0618 //WWW is obsolete
			}
			//LogMessage("RoomCamera_MoveCamera_1 would load from:" + text);
			//LogMessage("RoomCamera_MoveCamera_1 would load :" + System.System.IO.File.Exists(text));
			//LogMessage("RoomCamera_MoveCamera_1 bkgwww real " + (self.bkgwww != null));
		}
	}

	static private void PersistentData_ctor(On.PersistentData.orig_ctor orig, PersistentData self, RainWorld rainWorld)
	{
		orig(self, rainWorld);
		int ntex = self.cameraTextures.GetLength(0);
		self.cameraTextures = new Texture2D[ntex, 2];
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 2; j++)
			{
				self.cameraTextures[i, j] = new Texture2D(1400, 800, TextureFormat.ARGB32, false);
				self.cameraTextures[i, j].anisoLevel = 0;
				self.cameraTextures[i, j].filterMode = FilterMode.Point;
				self.cameraTextures[i, j].wrapMode = TextureWrapMode.Clamp;
				// This part originally loaded the same texture into both atlases
				// In the normal game, this had no effect, but if it remained, the background
				// Would always be a copy of the foreground
				if (j == 0)
				{
					Futile.atlasManager.UnloadAtlas("LevelTexture" + ((i != 0) ? i.ToString() : string.Empty));
					Futile.atlasManager.LoadAtlasFromTexture("LevelTexture" + ((i != 0) ? i.ToString() : string.Empty), self.cameraTextures[i, j], false);
				}

				else
				{
					Futile.atlasManager.UnloadAtlas("BackgroundTexture" + ((i != 0) ? i.ToString() : string.Empty));
					Futile.atlasManager.LoadAtlasFromTexture("BackgroundTexture" + ((i != 0) ? i.ToString() : string.Empty), self.cameraTextures[i, j], false);
				}
			}
		}
	}
}
