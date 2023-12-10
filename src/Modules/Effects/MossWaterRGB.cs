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
				LogMessage("Loading builder");
				EffectDefinitionBuilder builder = new EffectDefinitionBuilder("MossWaterRGB");
				builder
					.AddFloatField("Red", 0, 255, 1, 25)
					.AddFloatField("Green", 0, 255, 1, 77)
					.AddFloatField("Blue", 0, 255, 1, 51)
					.SetUADFactory((room, data, firstTimeRealized) => new MossWaterUAD(data))
					.SetCategory("POMEffectsExamples")
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
		public MossWaterRGB mossWaterRGB;

		public MossWaterUAD(EffectExtraData effectData)
		{
			EffectData = effectData;
			color = Color.green;
			mossWaterRGB = new MossWaterRGB();
		}

		public override void Update(bool eu)
		{
			color.r = EffectData.GetFloat("Red") / 255f;
			color.g = EffectData.GetFloat("Green") / 255f;
			color.b = EffectData.GetFloat("Blue") / 255f;
			if (mossWaterRGB != null )
			{
				mossWaterRGB.SetColor(color);
			}
		}

		public Color GetColor()
		{
			return color;
		}
	}
		internal class MossWaterRGB : UpdatableAndDeletable
	{
		public static readonly object mossRGBSprite = new();
		static bool loaded = false;
		const int vertsPerColumn = 64;

		// The constructor is never ran!
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

		public void SetColor(Color color)
		{
			Shader.SetGlobalColor("_InputColorMoss", color);
		}

		public static void MossLoadResources(RainWorld rw)
		{
			if (!loaded)
			{
				LogMessage("entered loading / loading status: " + loaded);
				loaded = true;
				if (MossWaterUnlit.mossBundle != null)
				{
					rw.Shaders["MossWaterRGB"] = FShader.CreateShader("MossWaterRGB", MossWaterUnlit.mossBundle.LoadAsset<Shader>("Assets/shaders 1.9.03/MossWaterRGB.shader"));
					Shader.SetGlobalColor("_InputColorMoss", Color.green);
				}
				else
				{
					LogMessage("MossWaterRGB must be loaded after MossWaterUnlit!");
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
			int index = 0;
			if (self.room.roomSettings.GetEffect(_Enums.MossWaterRGB) != null)
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
				LogMessage("got here");
				sLeaser.sprites[index] = new TriangleMesh("Futile_White", tris, true)
				{
					data = mossRGBSprite,
					shader = self.room.game.rainWorld.Shaders["MossWaterRGB"]
				};

				self.AddToContainer(sLeaser, rCam, null);
			}
		}

		private static void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (self.room.roomSettings.GetEffect(_Enums.MossWaterRGB) != null)
			{
				if (sLeaser.sprites.FirstOrDefault(x => x.data == mossRGBSprite) is TriangleMesh mossMesh)
				{
					WaterTriangleMesh waterMesh = (WaterTriangleMesh)sLeaser.sprites[0];
					int offset = self.PreviousSurfacePoint(camPos.x - 30f);
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
			}
		}
	}
}
