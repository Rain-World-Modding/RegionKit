using System.Runtime.CompilerServices;
using EffExt;

namespace RegionKit.Modules.Effects
{
	internal static class RKWaterSurfaces
	{
		private static readonly ConditionalWeakTable<Water, ReflectiveWater> reflectiveWaterCWT = new();
		private static readonly ConditionalWeakTable<Water, MossWaterUnlit> mossWaterUnlitCWT = new();
		private static readonly ConditionalWeakTable<Water, MossWaterRGB> mossWaterRgbCWT = new();
		private static bool RunCWTs = false;
		internal static void __RegisterBuilders()
		{
			// Reflective water
			try
			{
				EffectDefinitionBuilder builder = new EffectDefinitionBuilder("ReflectiveWater");
				builder
					.AddFloatField("LerpAngle", 0, 100, 1, 90)
					.AddFloatField("Alpha", 0, 255, 1, 1)
					.SetCategory(_Enums.RegionKit_Decoration.value)
					.Register();
			}
			catch (Exception ex)
			{
				LogWarning($"Error on eff ReflectiveWater init {ex}");
			}

			// Moss water RGB
			try
			{
				EffectDefinitionBuilder builder = new EffectDefinitionBuilder("MossWaterRGB");
				builder
					.AddFloatField("Blue", 0, 255, 1, 51)
					.AddFloatField("Green", 0, 255, 1, 77)
					.AddFloatField("Red", 0, 255, 1, 25)
					.AddFloatField("Height", 0, 1, 0.01f, 1)
					.SetCategory(_Enums.RegionKit_Decoration.value)
					.Register();
			}
			catch (Exception ex)
			{
				LogWarning($"Error on eff MossWaterRGB init {ex}");
			}
		}

		internal static void Apply()
		{
			On.Water.Update += Water_Update;
			On.Water.InitiateSprites += Water_InitiateSprites;
			On.Water.AddToContainer += Water_AddToContainer;
			On.Water.DrawSprites += Water_DrawSprites;
		}

		internal static void Undo()
		{
			On.Water.InitiateSprites -= Water_InitiateSprites;
			On.Water.AddToContainer -= Water_AddToContainer;
			On.Water.DrawSprites -= Water_DrawSprites;
		}

		private static void Water_Update(On.Water.orig_Update orig, Water self)
		{
			orig(self);
			if (self.room == null || !self.room.BeingViewed) return;

			// Reflective water
			if (self.room.roomSettings.GetEffect(_Enums.ReflectiveWater) is { } reflectiveWaterEffect && reflectiveWaterEffect.TryGetExtraData(out EffectExtraData? reflectiveWaterData) && reflectiveWaterData != null)
			{
				if (!reflectiveWaterCWT.TryGetValue(self, out ReflectiveWater instance))
				{
					RunCWTs = true;
					instance = new ReflectiveWater();
					reflectiveWaterCWT.Add(self, instance);
					LogInfo("Init reflective water");
				}

				float height = reflectiveWaterData.GetFloat("Alpha") / 255f;
				float lerpAngle = reflectiveWaterData.GetFloat("LerpAngle") / 100f;
				instance.SetValues(self.room, height, lerpAngle);
			}

			// Moss unlit
			if (self.room.roomSettings.GetEffect(_Enums.MossWater) is { } mossWaterUnlitEffect)
			{
				if (!mossWaterUnlitCWT.TryGetValue(self, out MossWaterUnlit instance))
				{
					RunCWTs = true;
					instance = new MossWaterUnlit();
					mossWaterUnlitCWT.Add(self, instance);
					LogInfo("Init moss water");
				}
			}

			// Moss RGB
			if (self.room.roomSettings.GetEffect(_Enums.MossWaterRGB) is { } mossWaterRgbEffect && mossWaterRgbEffect.TryGetExtraData(out EffectExtraData? mossWaterRgbData) && mossWaterRgbData != null)
			{
				if (!mossWaterRgbCWT.TryGetValue(self, out MossWaterRGB instance))
				{
					RunCWTs = true;
					instance = new MossWaterRGB();
					mossWaterRgbCWT.Add(self, instance);
					LogInfo("Init moss water rgb");
				}

				Color color = new Color(
					mossWaterRgbData.GetFloat("Red") / 255f,
					mossWaterRgbData.GetFloat("Green") / 255f,
					mossWaterRgbData.GetFloat("Blue") / 255f,
					1f);
				float height = mossWaterRgbData.GetFloat("Height");

				instance.SetValues(color, self.room, height);
			}
		}

		private static void Water_InitiateSprites(On.Water.orig_InitiateSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			if (RunCWTs)
			{
				if (reflectiveWaterCWT.TryGetValue(self, out ReflectiveWater reflectiveWater))
				{
					reflectiveWater.Reset();
				}

				if (mossWaterUnlitCWT.TryGetValue(self, out MossWaterUnlit mossWaterUnlit))
				{
					mossWaterUnlit.Reset();
				}

				if (mossWaterRgbCWT.TryGetValue(self, out MossWaterRGB mossWaterRGB))
				{
					mossWaterRGB.Reset();
				}
			}

			LogInfo("Water init!");
			sLeaser.RemoveAllSpritesFromContainer();
			orig(self, sLeaser, rCam);

			if (RunCWTs)
			{
				if (reflectiveWaterCWT.TryGetValue(self, out ReflectiveWater reflectiveWater))
				{
					reflectiveWater.InitiateSprites(self, sLeaser, rCam);
				}

				if (mossWaterUnlitCWT.TryGetValue(self, out MossWaterUnlit mossWaterUnlit))
				{
					mossWaterUnlit.InitiateSprites(self, sLeaser, rCam);
				}

				if (mossWaterRgbCWT.TryGetValue(self, out MossWaterRGB mossWaterRGB))
				{
					mossWaterRGB.InitiateSprites(self, sLeaser, rCam);
				}
			}
		}

		private static void Water_AddToContainer(On.Water.orig_AddToContainer orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			sLeaser.RemoveAllSpritesFromContainer(); // make sure to do this!!
			orig(self, sLeaser, rCam, newContatiner);
			if (!RunCWTs) return;

			if (reflectiveWaterCWT.TryGetValue(self, out ReflectiveWater reflectiveWater))
			{
				reflectiveWater.AddToContainer(self, sLeaser, rCam, newContatiner);
			}

			if (mossWaterUnlitCWT.TryGetValue(self, out MossWaterUnlit mossWaterUnlit))
			{
				mossWaterUnlit.AddToContainer(self, sLeaser, rCam, newContatiner);
			}

			if (mossWaterRgbCWT.TryGetValue(self, out MossWaterRGB mossWaterRGB))
			{
				mossWaterRGB.AddToContainer(self, sLeaser, rCam, newContatiner);
			}
		}

		private static void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (!RunCWTs) return;

			if (reflectiveWaterCWT.TryGetValue(self, out ReflectiveWater reflectiveWater))
			{
				reflectiveWater.DrawSprites(self, sLeaser, rCam, timeStacker, camPos);
			}

			if (mossWaterUnlitCWT.TryGetValue(self, out MossWaterUnlit mossWaterUnlit))
			{
				mossWaterUnlit.DrawSprites(self, sLeaser, rCam, timeStacker, camPos);
			}

			if (mossWaterRgbCWT.TryGetValue(self, out MossWaterRGB mossWaterRGB))
			{
				mossWaterRGB.DrawSprites(self, sLeaser, rCam, timeStacker, camPos);
			}
		}

		internal class ReflectiveWater
		{
			// By ASlightlyOvergrownCactus
			const int vertsPerColumn = 64;
			private float angleLerp = 0.5f;
			private int startSprite;
			private bool hasInitSprites = false;

			public void SetValues(Room room, float alpha, float angle)
			{
				float width = room.roomSettings.GetEffectAmount(_Enums.ReflectiveWater);
				Shader.SetGlobalFloat("_ReflectionLerp", width);
				Shader.SetGlobalFloat("_AlphaReflective", alpha);
				angleLerp = angle;
			}

			public void Reset()
			{
				hasInitSprites = false;
			}

			public void InitiateSprites(Water water, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				hasInitSprites = true;
				startSprite = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + water.surfaces.Length);

				for (int i = 0; i < water.surfaces.Length; i++)
				{
					int pointsToRender = i == 0 ? water.pointsToRender : (Mathf.Min(water.surfaces[i].points.Length, water.pointsToRender) - 1);
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
					sLeaser.sprites[startSprite + i] = new TriangleMesh("Futile_White", tris, true)
					{
						shader = water.room.game.rainWorld.Shaders["ReflectiveWater"],
					};
				}

				water.AddToContainer(sLeaser, rCam, null);
			}

			public void AddToContainer(Water water, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
			{
				if (!hasInitSprites) return;
				for (int i = 0; i < water.surfaces.Length; i++)
				{
					if (sLeaser.sprites[startSprite + i] is TriangleMesh mesh)
					{
						mesh.RemoveFromContainer();
						rCam.ReturnFContainer("GrabShaders").AddChild(mesh);
						//mesh.MoveBehindOtherNode(sLeaser.sprites[2 * i + 1]);
					}
				}
			}

			public void DrawSprites(Water water, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
			{
				if (!hasInitSprites)
				{
					water.InitiateSprites(sLeaser, rCam);
				}

				for (int i = 0; i < water.surfaces.Length; i++)
				{
					if (sLeaser.sprites[startSprite + i] is TriangleMesh reflectiveMesh)
					{
						if (i == 0)
						{
							WaterTriangleMesh waterMesh = (WaterTriangleMesh)sLeaser.sprites[0];
							int offset = water.surfaces[0].PreviousPoint(camPos.x - 30f);

							// Calculate vertex positions and UVs
							for (int column = 0; column <= water.pointsToRender; column++)
							{
								Vector2 waterFront = waterMesh.vertices[column * 2 + 0];
								Vector2 waterBack = waterMesh.vertices[column * 2 + 1];

								Vector3 crossWater = CrossWater(waterFront, waterBack, column, water.pointsToRender, waterMesh);
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
							int offset = water.surfaces[i].PreviousPoint(camPos.x - 30f);

							// Calculate vertex positions and UVs
							int pointsToRender = Mathf.Min(water.surfaces[i].points.Length, water.pointsToRender) - 1;
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
									reflectiveMesh.verticeColors[column * vertsPerColumn + row] = new Color(surfaceNormal.x, surfaceNormal.y, surfaceNormal.z, Mathf.InverseLerp(water.surfaces.Length - 1, 0, i));
									reflectiveMesh.MoveVertice(column * vertsPerColumn + row, Vector2.Lerp(waterFront, waterBack, v));
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

		internal class MossWaterUnlit
		{
			// By ASlightlyOvergrownCactus
			const int vertsPerColumn = 64;
			private int startSprite;
			private bool hasInitSprites = false;

			public void Reset()
			{
				hasInitSprites = false;
			}

			public void InitiateSprites(Water water, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				hasInitSprites = true;
				startSprite = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + water.surfaces.Length);

				for (int i = 0; i < water.surfaces.Length; i++)
				{
					int pointsToRender = i == 0 ? water.pointsToRender : (Mathf.Min(water.surfaces[i].points.Length, water.pointsToRender) - 1);
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
					sLeaser.sprites[startSprite + i] = new TriangleMesh("Futile_White", tris, true)
					{
						shader = water.room.game.rainWorld.Shaders["MossWater"],
					};
				}

				water.AddToContainer(sLeaser, rCam, null);
			}


			public void AddToContainer(Water water, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
			{
				if (!hasInitSprites) return;
				for (int i = 0; i < water.surfaces.Length; i++)
				{
					if (sLeaser.sprites[startSprite + i] is TriangleMesh mesh)
					{
						//rCam.ReturnFContainer("Water").AddChild(mesh); // this is already done by Water.AddToContainer
						mesh.MoveBehindOtherNode(sLeaser.sprites[2 * i + 1]);
					}
				}
			}

			public void DrawSprites(Water water, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
			{
				if (!hasInitSprites)
				{
					water.InitiateSprites(sLeaser, rCam);
				}

				for (int i = 0; i < water.surfaces.Length; i++)
				{
					if (sLeaser.sprites[startSprite + i] is TriangleMesh mossMesh)
					{
						if (i == 0)
						{
							WaterTriangleMesh waterMesh = (WaterTriangleMesh)sLeaser.sprites[0];
							int offset = water.surfaces[0].PreviousPoint(camPos.x - 30f);
							// Calculate vertex positions and UVs
							for (int column = 0; column <= water.pointsToRender; column++)
							{
								Vector2 waterFront = waterMesh.vertices[column * 2 + 0];
								Vector2 waterBack = waterMesh.vertices[column * 2 + 1];

								for (int row = 0; row < vertsPerColumn; row++)
								{
									float u = column + offset;
									float v = row / (vertsPerColumn - 1f);
									mossMesh.UVvertices[column * vertsPerColumn + row] = new Vector2(u, v);
									mossMesh.verticeColors[column * vertsPerColumn + row] = new Color(0f, 0f, 0f, 1f);
									mossMesh.MoveVertice(column * vertsPerColumn + row, Vector2.Lerp(waterFront, waterBack, v));
								}
							}
						}
						else
						{
							TriangleMesh waterMesh = (TriangleMesh)sLeaser.sprites[i * 2];
							int offset = water.surfaces[i].PreviousPoint(camPos.x - 30f);
							int pointsToRender = Mathf.Min(water.surfaces[i].points.Length, water.pointsToRender) - 1;
							// Calculate vertex positions and UVs
							for (int column = 0; column <= pointsToRender; column++)
							{
								Vector2 waterFront = waterMesh.vertices[column * 2 + 0];
								Vector2 waterBack = waterMesh.vertices[column * 2 + 1];

								for (int row = 0; row < vertsPerColumn; row++)
								{
									float u = column + offset;
									float v = row / (vertsPerColumn - 1f);
									mossMesh.UVvertices[column * vertsPerColumn + row] = new Vector2(u, v);
									mossMesh.verticeColors[column * vertsPerColumn + row] = new Color(0f, 0f, 0f, Mathf.InverseLerp(water.surfaces.Length - 1, 0, i));
									mossMesh.MoveVertice(column * vertsPerColumn + row, Vector2.Lerp(waterFront, waterBack, v));
								}
							}
						}
					}
				}
			}

		}

		internal class MossWaterRGB
		{
			// By ASlightlyOvergrownCactus
			const int vertsPerColumn = 128;
			private int startSprite;
			private bool hasInitSprites = false;
			private Color mossColor = Color.green;

			public void SetValues(Color color, Room room, float height)
			{
				//Shader.SetGlobalColor("_InputColorMoss", color);
				mossColor = color;
				float width = room.roomSettings.GetEffectAmount(_Enums.MossWaterRGB);
				if (width > 0)
				{
					width *= Mathf.Lerp(0.05f, 5f, width);
				}
				Shader.SetGlobalFloat("_InputWidthMoss", width);
				Shader.SetGlobalFloat("_InputHeightMoss", height);
			}

			public void Reset()
			{
				hasInitSprites = false;
			}

			public void InitiateSprites(Water water, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				hasInitSprites = true;
				startSprite = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + water.surfaces.Length);

				for (int i = 0; i < water.surfaces.Length; i++)
				{
					int pointsToRender = i == 0 ? water.pointsToRender : (Mathf.Min(water.surfaces[i].points.Length, water.pointsToRender) - 1);
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
					sLeaser.sprites[startSprite + i] = new TriangleMesh("Futile_White", tris, true)
					{
						shader = water.room.game.rainWorld.Shaders["MossWaterRGB"],
						alpha = i == 0 ? 1 : Mathf.InverseLerp(water.surfaces.Length - 1, 0, i)
					};
				}

				water.AddToContainer(sLeaser, rCam, null);
			}


			public void AddToContainer(Water water, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
			{
				if (!hasInitSprites) return;
				for (int i = 0; i < water.surfaces.Length; i++)
				{
					if (sLeaser.sprites[startSprite + i] is TriangleMesh mesh)
					{
						//rCam.ReturnFContainer("Water").AddChild(mesh); // this is already done by Water.AddToContainer
						mesh.MoveBehindOtherNode(sLeaser.sprites[2 * i + 1]);
					}
				}
			}

			public void DrawSprites(Water water, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
			{
				if (!hasInitSprites)
				{
					water.InitiateSprites(sLeaser, rCam);
				}

				for (int i = 0; i < water.surfaces.Length; i++)
				{
					if (sLeaser.sprites[startSprite + i] is TriangleMesh mossMesh)
					{
						if (i == 0)
						{
							WaterTriangleMesh waterMesh = (WaterTriangleMesh)sLeaser.sprites[0];
							int offset = water.surfaces[0].PreviousPoint(camPos.x - 30f);
							// Calculate vertex positions and UVs
							for (int column = 0; column <= water.pointsToRender; column++)
							{
								Vector2 waterFront = waterMesh.vertices[column * 2 + 0];
								Vector2 waterBack = waterMesh.vertices[column * 2 + 1];

								for (int row = 0; row < vertsPerColumn; row++)
								{
									float u = column + offset;
									float v = row / (vertsPerColumn - 1f);
									mossMesh.UVvertices[column * vertsPerColumn + row] = new Vector2(u, v);
									mossMesh.verticeColors[column * vertsPerColumn + row] = mossColor with { a = 1f };
									mossMesh.MoveVertice(column * vertsPerColumn + row, Vector2.Lerp(waterFront, waterBack, v));
								}
							}
						}
						else
						{
							TriangleMesh waterMesh = (TriangleMesh)sLeaser.sprites[i * 2];
							int offset = water.surfaces[i].PreviousPoint(camPos.x - 30f);
							int pointsToRender = Mathf.Min(water.surfaces[i].points.Length, water.pointsToRender) - 1;
							// Calculate vertex positions and UVs
							for (int column = 0; column <= pointsToRender; column++)
							{
								Vector2 waterFront = waterMesh.vertices[column * 2 + 0];
								Vector2 waterBack = waterMesh.vertices[column * 2 + 1];

								for (int row = 0; row < vertsPerColumn; row++)
								{
									float u = column + offset;
									float v = row / (vertsPerColumn - 1f);
									mossMesh.UVvertices[column * vertsPerColumn + row] = new Vector2(u, v);
									mossMesh.verticeColors[column * vertsPerColumn + row] = mossColor with { a = Mathf.InverseLerp(water.surfaces.Length - 1, 0, i) };
									mossMesh.MoveVertice(column * vertsPerColumn + row, Vector2.Lerp(waterFront, waterBack, v));
								}
							}
						}
					}
				}
			}

		}
	}
}
