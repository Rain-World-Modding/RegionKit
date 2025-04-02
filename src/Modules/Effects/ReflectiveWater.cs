using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EffExt;

namespace RegionKit.Modules.Effects
{
	internal static class ReflectiveWaterBuilder
	{
		internal static void __RegisterBuilder()
		{
			try
			{
				EffectDefinitionBuilder builder = new EffectDefinitionBuilder("ReflectiveWater");
				builder
					.AddFloatField("LerpAngle", 0, 100, 1, 90)
					.AddFloatField("Alpha", 0, 255, 1, 1)
					.SetUADFactory((room, data, firstTimeRealized) => new ReflectiveWaterUAD(data))
					.SetCategory("RegionKit")
					.Register();
			}
			catch (Exception ex)
			{
				LogWarning($"Error on eff ReflectiveWater init {ex}");
			}
		}
	}

	internal class ReflectiveWaterUAD : UpdatableAndDeletable
	{
		public EffectExtraData EffectData { get; }
		public float height;
		public float lerpAngle;
		public ReflectiveWater reflectiveWater;


		public ReflectiveWaterUAD(EffectExtraData effectData)
		{
			EffectData = effectData;
			height = 1;
			reflectiveWater = new ReflectiveWater();

		}

		public override void Update(bool eu)
		{
			height = EffectData.GetFloat("Alpha") / 255f;
			lerpAngle = EffectData.GetFloat("LerpAngle") / 100f;

			if (reflectiveWater != null && room.BeingViewed)
			{
				reflectiveWater.SetValues(room, height, lerpAngle);
			}
		}
	}

	internal class ReflectiveWater
	{
		public static readonly object reflectiveSprite = new();
		static bool loaded = false;
		const int vertsPerColumn = 64;
		private static float angleLerp = 0.5f;
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
			On.Water.AddToContainer -= Water_AddToContainer;
		}

		public void SetValues(Room room, float alpha, float angle)
		{
			float width = room.roomSettings.GetEffectAmount(_Enums.ReflectiveWater);
			Shader.SetGlobalFloat("_ReflectionLerp", width);
			Shader.SetGlobalFloat("_AlphaReflective", alpha);
			angleLerp = angle;
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
					rCam.ReturnFContainer("GrabShaders").AddChild(reflectiveMesh);
					reflectiveMesh.MoveBehindOtherNode(sLeaser.sprites[1]);
				}
			}
		}

		private static void Water_InitiateSprites(On.Water.orig_InitiateSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			int index;
			if (self.room.roomSettings.GetEffect(_Enums.ReflectiveWater) != null)
			{
				index = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + self.surfaces.Length);

				for (int i = 0; i < self.surfaces.Length; i++)
				{
					int pointsToRender = i == 0 ? self.pointsToRender : (Mathf.Min(self.surfaces[i].points.Length, self.pointsToRender) - 1);
					TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[pointsToRender * 2 * (vertsPerColumn - 1)];
					int triIndex = 0;
					for (int column = 0; column < pointsToRender; column++)
					{
						int firstVertex = column * vertsPerColumn;

						for (int row = 0; row < vertsPerColumn - 1; row++)
						{
							int j = firstVertex + row;
							tris[triIndex++] = new TriangleMesh.Triangle(j, j + 1, j + 1 + vertsPerColumn);
							tris[triIndex++] = new TriangleMesh.Triangle(j, j + 1 + vertsPerColumn, j + vertsPerColumn);
						}
					}
					sLeaser.sprites[index + i] = new TriangleMesh("Futile_White", tris, true)
					{
						data = reflectiveSprite,
						shader = self.room.game.rainWorld.Shaders["ReflectiveWater"]
					};
				}

				self.AddToContainer(sLeaser, rCam, null);
			}
		}

		private static void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (self.room.roomSettings.GetEffect(_Enums.ReflectiveWater) != null)
			{
				int startIndex = -1;
				for (int i = 0; i < sLeaser.sprites.Length; i++)
				{
					if (sLeaser.sprites[i].data == reflectiveSprite)
					{
						startIndex = i; break;
					}
				}


				if (startIndex > -1)
				{
					for (int i = 0; i < self.surfaces.Length; i++)
					{
						if (sLeaser.sprites[startIndex + i] is TriangleMesh reflectiveMesh)
						{
							if (i == 0)
							{
								WaterTriangleMesh waterMesh = (WaterTriangleMesh)sLeaser.sprites[0];
								int offset = self.surfaces[0].PreviousPoint(camPos.x - 30f);

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
										Vector3 surfaceNormal = Vector3.Slerp(crossWater.normalized, new Vector3(0.0f, 0.5f, -0.4f), angleLerp);
										reflectiveMesh.verticeColors[column * vertsPerColumn + row] = new Color(surfaceNormal.x, surfaceNormal.y, surfaceNormal.z, 1);
										reflectiveMesh.MoveVertice(column * vertsPerColumn + row, Vector2.Lerp(waterFront, waterBack, v));
									}
								}
							}
							else
							{
								TriangleMesh waterMesh = (TriangleMesh)sLeaser.sprites[i * 2];
								int offset = self.surfaces[i].PreviousPoint(camPos.x - 30f);

								// Calculate vertex positions and UVs
								int pointsToRender = Mathf.Min(self.surfaces[i].points.Length, self.pointsToRender) - 1;
								for (int column = 0; column <= pointsToRender; column++)
								{
									Vector2 waterFront = waterMesh.vertices[column * 2 + 0];
									Vector2 waterBack = waterMesh.vertices[column * 2 + 1];

									Vector3 crossWater = CrossWater(waterFront, waterBack, column, pointsToRender, waterMesh);
									crossWater = crossWater.normalized;

									for (int row = 0; row < vertsPerColumn; row++)
									{
										float u = column + offset;
										float v = row / (vertsPerColumn - 1f);
										reflectiveMesh.UVvertices[column * vertsPerColumn + row] = new Vector2(u, v);
										// Check vector to be slerped; Make sure the angle works with max water amplitude
										Vector3 surfaceNormal = Vector3.Slerp(crossWater.normalized, new Vector3(0.0f, 0.5f, -0.4f), angleLerp);
										reflectiveMesh.verticeColors[column * vertsPerColumn + row] = new Color(surfaceNormal.x, surfaceNormal.y, surfaceNormal.z, 1);
										reflectiveMesh.MoveVertice(column * vertsPerColumn + row, Vector2.Lerp(waterFront, waterBack, v));
									}
								}
							}
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

		private static Vector3 CrossWater(Vector2 waterFront, Vector2 waterBack, int column, int pointsToRender, TriangleMesh mesh)
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
