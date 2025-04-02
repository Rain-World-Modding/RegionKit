using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EffExt;
using RegionKit.Modules.Objects;

namespace RegionKit.Modules.Effects
{
	// By ASlightlyOvergrownCactus
	// Called before MossWaterRGB's Load Resources
	internal static class MossWaterRGBBuilder
	{
		internal static void __RegisterBuilder()
		{
			try
			{
				EffectDefinitionBuilder builder = new EffectDefinitionBuilder("MossWaterRGB");
				builder
					.AddFloatField("Blue", 0, 255, 1, 51)
					.AddFloatField("Green", 0, 255, 1, 77)
					.AddFloatField("Red", 0, 255, 1, 25)
					.AddFloatField("Height", 0, 1, 0.01f, 1)
					.SetUADFactory((room, data, firstTimeRealized) => new MossWaterUAD(data))
					.SetCategory("RegionKit")
					.Register();
			}
			catch (Exception ex)
			{
				LogWarning($"Error on eff MossWaterRGB init {ex}");
			}
		}
	}

	internal class MossWaterUAD : UpdatableAndDeletable
	{
		public EffectExtraData EffectData { get; }
		public Color color;
		public float height;
		public MossWaterRGB mossWaterRGB;
		

		public MossWaterUAD(EffectExtraData effectData)
		{
			EffectData = effectData;
			color = Color.green;
			height = 1;
			mossWaterRGB = new MossWaterRGB();
			
		}

		public override void Update(bool eu)
		{
			color.r = EffectData.GetFloat("Red") / 255f;
			color.g = EffectData.GetFloat("Green") / 255f;
			color.b = EffectData.GetFloat("Blue") / 255f;
			height = EffectData.GetFloat("Height");


			if (mossWaterRGB != null && room.BeingViewed)
			{
				mossWaterRGB.SetValues(color, room, height);
			}
		}
	}
		internal class MossWaterRGB : UpdatableAndDeletable
	{
		public static readonly object mossRGBSprite = new();
		static bool loaded = false;
		const int vertsPerColumn = 128;

		public MossWaterRGB() 
		{

		}
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

		public void SetValues(Color color, Room room, float height)
		{
			Shader.SetGlobalColor("_InputColorMoss", color);
			float width = room.roomSettings.GetEffectAmount(_Enums.MossWaterRGB);
			if (width > 0)
			{
				width *= Mathf.Lerp(0.05f, 5f, width);
			}
			Shader.SetGlobalFloat("_InputWidthMoss", width);
			Shader.SetGlobalFloat("_InputHeightMoss", height);
		}

		public static void MossLoadResources(RainWorld rw)
		{
			if (!loaded)
			{
				loaded = true;
				if (MossWaterUnlit.mossBundle != null)
				{
					rw.Shaders["MossWaterRGB"] = FShader.CreateShader("MossWaterRGB", MossWaterUnlit.mossBundle.LoadAsset<Shader>("Assets/shaders 1.9.03/MossWaterRGB.shader"));
					Shader.SetGlobalColor("_InputColorMoss", Color.green);
				}
				else
				{
					LogWarning("MossWaterRGB must be loaded after MossWaterUnlit!");
				}
			}
		}

		private static void Water_AddToContainer(On.Water.orig_AddToContainer orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			orig(self, sLeaser, rCam, newContatiner);
			if (self.room.roomSettings.GetEffect(_Enums.MossWaterRGB) != null)
			{
				if (sLeaser.sprites.FirstOrDefault(x => x.data == mossRGBSprite) is TriangleMesh mossMesh)
				{
					rCam.ReturnFContainer("Water").AddChild(mossMesh);
					mossMesh.MoveBehindOtherNode(sLeaser.sprites[1]);
				}
			}
		}

		private static void Water_InitiateSprites(On.Water.orig_InitiateSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			int index;
			if (self.room.roomSettings.GetEffect(_Enums.MossWaterRGB) != null)
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
						data = mossRGBSprite,
						shader = self.room.game.rainWorld.Shaders["MossWaterRGB"]
					};
				}

				self.AddToContainer(sLeaser, rCam, null);
			}
		}

		private static void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (self.room.roomSettings.GetEffect(_Enums.MossWaterRGB) != null)
			{
				int startIndex = -1;
				for (int i = 0; i < sLeaser.sprites.Length; i++)
				{
					if (sLeaser.sprites[i].data == mossRGBSprite)
					{
						startIndex = i; break;
					}
				}

				if (startIndex > -1)
				{
					for (int i = 0; i < self.surfaces.Length; i++)
					{
						if (sLeaser.sprites[startIndex + i] is TriangleMesh mossMesh)
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

									for (int row = 0; row < vertsPerColumn; row++)
									{
										float u = column + offset;
										float v = row / (vertsPerColumn - 1f);
										mossMesh.UVvertices[column * vertsPerColumn + row] = new Vector2(u, v);
										mossMesh.MoveVertice(column * vertsPerColumn + row, Vector2.Lerp(waterFront, waterBack, v));
									}
								}
							}
							else
							{
								TriangleMesh waterMesh = (TriangleMesh)sLeaser.sprites[i * 2];
								int offset = self.surfaces[i].PreviousPoint(camPos.x - 30f);
								int pointsToRender = Mathf.Min(self.surfaces[i].points.Length, self.pointsToRender) - 1;
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
}
