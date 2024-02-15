using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Modules.Effects
{
	internal class ReflectiveWater
	{
		public static readonly object reflectiveSprite = new();
		static bool loaded = false;
		const int vertsPerColumn = 64;
		internal static void Apply()
		{
			On.Water.InitiateSprites += Water_InitiateSprites;
			On.Water.DrawSprites += Water_DrawSprites;
			On.Water.AddToContainer += Water_AddToContainer;
		}

		internal static void Undo()
		{
			On.Water.InitiateSprites -= Water_InitiateSprites;
			On.Water.DrawSprites -= Water_DrawSprites;
		}

		public static void ReflectiveLoadResources(RainWorld rw)
		{
			if (!loaded)
			{
				loaded = true;
				if (MossWaterUnlit.mossBundle != null)
				{
					rw.Shaders["ReflectiveWater"] = FShader.CreateShader("ReflectiveWater", MossWaterUnlit.mossBundle.LoadAsset<Shader>("Assets/shaders 1.9.03/ReflectiveWater.shader"));
				}
				else
				{
					LogWarning("ReflectiveWater must be loaded after MossWaterUnlit!");
				}
			}
		}

		private static void Water_AddToContainer(On.Water.orig_AddToContainer orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			orig(self, sLeaser, rCam, newContatiner);
			if (self.room.roomSettings.GetEffect(_Enums.ReflectiveWater) != null)
			{
				if (sLeaser.sprites.FirstOrDefault(x => x.data == reflectiveSprite) is TriangleMesh reflectiveMesh)
				{
					rCam.ReturnFContainer("Water").AddChild(reflectiveMesh);
					reflectiveMesh.MoveBehindOtherNode(sLeaser.sprites[1]);
				}
			}
		}

		private static void Water_InitiateSprites(On.Water.orig_InitiateSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			int index = 0;
			if (self.room.roomSettings.GetEffect(_Enums.ReflectiveWater) != null)
			{
				index = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);

				TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[self.pointsToRender * 2 * (vertsPerColumn - 1)];
				int triIndex = 0;
				for (int column = 0; column < self.pointsToRender; column++)
				{
					int firstVertex = column * vertsPerColumn;

					for (int row = 0; row < vertsPerColumn - 1; row++)
					{
						int i = firstVertex + row;
						tris[triIndex++] = new TriangleMesh.Triangle(i, i + 1, i + 1 + vertsPerColumn);
						tris[triIndex++] = new TriangleMesh.Triangle(i, i + 1 + vertsPerColumn, i + vertsPerColumn);
					}
				}
				sLeaser.sprites[index] = new TriangleMesh("Futile_White", tris, true)
				{
					data = reflectiveSprite,
					shader = self.room.game.rainWorld.Shaders["ReflectiveWater"]
				};

				self.AddToContainer(sLeaser, rCam, null);
			}
		}
		private static void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (self.room.roomSettings.GetEffect(_Enums.ReflectiveWater) != null)
			{
				if (sLeaser.sprites.FirstOrDefault(x => x.data == reflectiveSprite) is TriangleMesh reflectiveMesh)
				{
					Shader.SetGlobalFloat("_ReflectionLerp", self.room.roomSettings.GetEffectAmount(_Enums.ReflectiveWater));
					
					WaterTriangleMesh waterMesh = (WaterTriangleMesh)sLeaser.sprites[0];
					int offset = self.PreviousSurfacePoint(camPos.x - 30f);

					// Calculate vertex positions and UVs
					for (int column = 0; column <= self.pointsToRender; column++)
					{
						Vector2 waterFront = waterMesh.vertices[column * 2 + 0];
						Vector2 waterBack = waterMesh.vertices[column * 2 + 1];

						Vector3 crossWater = CrossWater(waterFront, waterBack, column, self.pointsToRender, waterMesh);
						crossWater = crossWater.normalized;

						for (int row = 0; row < vertsPerColumn; row++)
						{
							float u = column + offset;
							float v = row / (vertsPerColumn - 1f);
							reflectiveMesh.UVvertices[column * vertsPerColumn + row] = new Vector2(u, v);
							// Check vector to be slerped; Make sure the angle works with max water amplitude
							Vector3 surfaceNormal = Vector3.Slerp(crossWater.normalized, new Vector3(0.0f, 0.5f, -0.4f), 0.9f);
							reflectiveMesh.verticeColors[column * vertsPerColumn + row] = new Color(surfaceNormal.x, surfaceNormal.y, surfaceNormal.z, 1);
							reflectiveMesh.MoveVertice(column * vertsPerColumn + row, Vector2.Lerp(waterFront, waterBack, v));
						}
					}
				}
			}
		}

		private static Vector3 CrossWater(Vector2 waterFront, Vector2 waterBack, int column, int pointsToRender, WaterTriangleMesh mesh)
		{
			// First Vector3 is waterFront to waterBack
			Vector2 depthDiff = waterFront - waterBack;
			Vector3 waterDepth = new Vector3(depthDiff.x, depthDiff.y, 30f);

			// Second Vector3 is two waterFront vectors next to eachother
			Vector3 vertLength = Vector3.zero;
			if (column == pointsToRender)
			{
				Vector2 difference = waterFront - mesh.vertices[column * 2 - 2];
				vertLength.x = difference.x;
				vertLength.y = difference.y;
			}
			else
			{
				Vector2 difference = mesh.vertices[column * 2 + 2] - waterFront;
				vertLength.x = difference.x;
				vertLength.y = difference.y;
			}
			Vector3 cross = Vector3.Cross(vertLength, waterDepth);
			return new Vector3(-cross.x, -cross.y, cross.z);
		}
	}
}
