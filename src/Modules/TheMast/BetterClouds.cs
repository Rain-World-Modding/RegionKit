using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast
{
	internal static class BetterClouds
	{
		const float heightThreshold = 5000f;

		public static void Apply()
		{
			On.AboveCloudsView.ctor += AboveCloudsView_ctor;
			On.AboveCloudsView.Update += AboveCloudsView_Update;
		}

		private static FieldInfo __BackgroundScene_elementsAddedToRoom = typeof(BackgroundScene).GetField("elementsAddedToRoom", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		private static void AboveCloudsView_Update(On.AboveCloudsView.orig_Update orig, AboveCloudsView self, bool eu)
		{
			// Temporarily change element depth to place clouds behind other objects
			const float depthOffset = 10000f;
			bool addedToRoom = (bool)__BackgroundScene_elementsAddedToRoom.GetValue(self);
			if (!addedToRoom)
			{
				for (int i = 0; i < self.elements.Count; i++)
				{
					if (self.elements[i] is AboveCloudsView.DistantCloud dc && dc.depth > self.distantCloudsEndDepth)
						dc.depth += depthOffset;
				}
			}

			orig(self, eu);

			// Revert depth changes
			if (!addedToRoom)
			{
				for (int i = 0; i < self.elements.Count; i++)
				{
					if (self.elements[i] is AboveCloudsView.DistantCloud dc && dc.depth > self.distantCloudsEndDepth)
						dc.depth -= depthOffset;
				}
			}
		}

		// Double the number of clouds if the room is above map height 5000
		private static FieldInfo __DistantCloud_distantCloudDepth = typeof(AboveCloudsView.DistantCloud).GetField("distantCloudDepth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		private static void AboveCloudsView_ctor(On.AboveCloudsView.orig_ctor orig, AboveCloudsView self, Room room, RoomSettings.RoomEffect effect)
		{
			orig(self, room, effect);
			if (room.world?.region?.name != "TM") return;

			// Move a can downwards to stop it from appearing over the clouds
			{
				float amount = Mathf.InverseLerp(3000f, 6000f, room.abstractRoom.mapPos.y);
				for (int i = 0; i < self.elements.Count; i++)
				{
					if (self.elements[i] is AboveCloudsView.DistantBuilding db)
					{
						if (db.assetName == "AtC_Structure2")
						{
							db.pos.y -= amount * db.depth * 10f;
							break;
						}
					}
				}
			}

			// Add extra cans and spires to fill the right side of the screen
			{
				float depth = 600f;
				self.AddElement(new AboveCloudsView.DistantBuilding(self, "AtC_Structure3", self.PosFromDrawPosAtNeutralCamPos(new Vector2(500f, -21f), depth), depth, -200f));
				depth = 150f;
				self.AddElement(new AboveCloudsView.DistantBuilding(self, "AtC_Spire4", self.PosFromDrawPosAtNeutralCamPos(new Vector2(344f, -20f), depth), depth, 80f));
				depth = 330f;
				self.AddElement(new AboveCloudsView.DistantBuilding(self, "AtC_Spire5", self.PosFromDrawPosAtNeutralCamPos(new Vector2(670f, -24f), depth), depth, -100f));
			}

			if (room.abstractRoom.mapPos.y < 5000f) return;

			// Double the amount of clouds
			int cloudCount = self.clouds.Count;
			for (int j = 0; j < cloudCount; j++)
			{
				float depth = Mathf.Clamp01((j + 0.5f) / (cloudCount - 1f));
				self.AddElement(new AboveCloudsView.DistantCloud(self, new Vector2(0f, -40f * self.cloudsEndDepth * (1f - depth)), depth, j + cloudCount));
			}

			// Add some clouds to cover the gap at the back of the scene
			int addedCloudCount = 10;
			for (int i = addedCloudCount - 1; i >= 0; i--)
			{
				float t = i / (addedCloudCount - 1f);
				float depth = (cloudCount + i * 0.5f) / (cloudCount - 1f);
				AboveCloudsView.DistantCloud dc = new AboveCloudsView.DistantCloud(self, new Vector2(0f, t * 8000f), depth, i + cloudCount * 2);
				self.AddElement(dc);
				dc.depth = LerpUnclamped(self.cloudsEndDepth, self.distantCloudsEndDepth, Mathf.Pow(depth, 1.5f));
			}

			// Stretch clouds vertically
			for (int i = 0; i < self.clouds.Count; i++)
			{
				if (self.clouds[i] is AboveCloudsView.DistantCloud dc)
				{
					float t = self.clouds[i].index / (cloudCount * 2 + addedCloudCount - 1f);
					__DistantCloud_distantCloudDepth.SetValue(dc, Mathf.Clamp((float)__DistantCloud_distantCloudDepth.GetValue(dc) - 0.3f, 0f, 1f));
				}
			}
		}

		private static float LerpUnclamped(float a, float b, float t)
		{
			return a * (1f - t) + b * t;
		}
	}
}
