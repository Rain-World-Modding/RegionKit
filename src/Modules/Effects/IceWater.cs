using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EffExt;
using RegionKit.Modules.RoomSlideShow;
using SharpVoronoiLib;

namespace RegionKit.Modules.Effects
{
	// By ASlightlyOvergrownCactus

	internal static class IceWaterBuilder
	{ 
		internal static void __RegisterBuilder()
		{
			try
			{
				EffectDefinitionBuilder builder = new EffectDefinitionBuilder("IceWater");
				builder
					.AddFloatField("Blue", 0, 255, 1, 255)
					.AddFloatField("Green", 0, 255, 1, 255)
					.AddFloatField("Red", 0, 255, 1, 255)
					.AddFloatField("Height", 0, 5, 0.1f, 1)
					.AddFloatField("CellCount", 0f, 1000f, 1f, 400f)
					.AddIntField("GenType", 1, 2, 1)
					.SetUADFactory((room, data, firstTimeRealized) => new IceWaterUAD(data))
					.SetCategory("RegionKit")
					.Register();
			}
			catch (Exception ex)
			{
				LogfixWarning($"Error on eff IceWater init {ex}");
			}
		}


	}

	 internal class IceWaterUAD : UpdatableAndDeletable
	{
		public EffectExtraData EffectData { get; }
		public UnityEngine.Color color;
		public float height;
		public IceWater iceWater;
		public int seed;
		public float cellCount;


		public IceWaterUAD(EffectExtraData effectData)
		{
			EffectData = effectData;
			color = UnityEngine.Color.white;
			height = 1;
			iceWater = new IceWater();
			seed = 1;
		}

		public override void Update(bool eu)
		{
			color.r = EffectData.GetFloat("Red") / 255f;
			color.g = EffectData.GetFloat("Green") / 255f;
			color.b = EffectData.GetFloat("Blue") / 255f;
			color.a = 1.0f;
			height = EffectData.GetFloat("Height");
			seed = EffectData.GetInt("GenType");
			cellCount = EffectData.GetFloat("CellCount");


			if (iceWater != null && room.BeingViewed)
			{
				iceWater.SetValues(color, room, height, seed, cellCount);
			}
		}
	}


	internal class IceWater : CosmeticSprite
	{
		public static readonly object iceWaterSprite = new();
		static List<VoronoiSite> poisPoints = new List<VoronoiSite>();
		private static VoronoiPlane plane;
		private static List<VoronoiEdge> voronoiEdges = new List<VoronoiEdge>();
		private static float height = 0f;
		private static float cellAmount = 0f;
		private static int genType = 1;
		private static UnityEngine.Color color;
		private static float oldAmount = 0f;
		private static int oldGen = 1;
		public IceWater()
		{

		}

		internal static void Apply()
		{
			On.Room.Loaded += Room_Loaded;
		}



		internal static void Undo()
		{
			On.Room.Loaded -= Room_Loaded;
		}

		private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
		{
			orig(self);

			for (int i = 0; i < self.roomSettings.effects.Count; i++)
			{
				if (self.roomSettings.effects[i].type == _Enums.IceWater) self.AddObject(new IceWater());
			}
		}

		public void SetValues(UnityEngine.Color colorUAD, Room room, float height, int genTypeUAD, float cellUAD)
		{
			oldGen = genType;
			oldAmount = cellAmount;

			genType = genTypeUAD;
			cellAmount = cellUAD;
			color = colorUAD;
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			if (rCam.room.roomSettings.GetEffect(_Enums.IceWater) != null)
			{
				// Current input size is: (3240.0, 768.0)
				plane = new VoronoiPlane(0, 0, rCam.sSize.x + 320f, rCam.sSize.y + 100f);
				if (genType == 1)
					poisPoints = plane.GenerateRandomSites((int)cellAmount, PointGenerationMethod.Uniform);
				if (genType == 2)
					poisPoints = plane.GenerateRandomSites((int)cellAmount, PointGenerationMethod.Gaussian);
				plane.Tessellate();
				voronoiEdges = plane.Relax(3, 0.7f);
				if (plane.Sites != null)
				{
					sLeaser.sprites = new FSprite[plane.Sites.Count];
					for (int i = 0; i < plane.Sites.Count; i++)
					{
						// 0 is the center of each cell
						TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[plane.Sites[i].cell.Count - 2];
						for (int j = 0; j < tris.Length; j++)
						{
							tris[j] = new TriangleMesh.Triangle(0, j + 1, j + 2);
						}

						sLeaser.sprites[i] = new TriangleMesh("Futile_White", tris, true);
					}
				}

				/*
				for (int i = 0; i < poisPoints.Count; i++)
				{
					sLeaser.sprites[i] = new FSprite("pixel")
					{
						anchorX = 0.5f,
						anchorY = 0.5f,
						scale = 4f
					};
				}

				for (int i = poisPoints.Count; i < sLeaser.sprites.Length; i++)
				{
					int j = i - poisPoints.Count;
					float rotation = VecToDeg(DirVec(VoronoiPointToVector(voronoiEdges[j].Start), VoronoiPointToVector(voronoiEdges[j].End))) + 90f;

					sLeaser.sprites[i] = new FSprite("pixel")
					{
						anchorX = 0.5f,
						anchorY = 0.5f,
						scaleY = 2f,
						scaleX = (float)voronoiEdges[i - poisPoints.Count].Length,
						rotation = rotation
					};
				}
				*/


				AddToContainer(sLeaser, rCam, null);
			}
		}

		public bool RegenerateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.RemoveAllSpritesFromContainer();
			plane = new VoronoiPlane(0, 0, rCam.sSize.x + 320f, rCam.sSize.y + 100f);
			if (genType == 1)
				poisPoints = plane.GenerateRandomSites((int)cellAmount, PointGenerationMethod.Uniform);
			if (genType == 2)
				poisPoints = plane.GenerateRandomSites((int)cellAmount, PointGenerationMethod.Gaussian);
			plane.Tessellate();
			voronoiEdges = plane.Relax(3, 0.7f);
			if (plane.Sites != null)
			{
				sLeaser.sprites = new FSprite[plane.Sites.Count];
				for (int i = 0; i < plane.Sites.Count; i++)
				{
					// 0 is the center of each cell
					TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[plane.Sites[i].cell.Count - 2];
					for (int j = 0; j < tris.Length; j++)
					{
						tris[j] = new TriangleMesh.Triangle(0, j + 1, j + 2);
					}

					sLeaser.sprites[i] = new TriangleMesh("Futile_White", tris, true);
				}
			}
			AddToContainer(sLeaser, rCam, null);
			return false;
		}

		// Draws sprites on screen
		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (rCam.room.roomSettings.GetEffect(_Enums.IceWater) != null)
			{

				/*
				for (int i = 0; i < poisPoints.Count; i++)
				{
					//Vector2 origWaterPos = rCam.ApplyDepth(new Vector2(poisPoints[i - 1].x - 320f, room.FloatWaterLevel(poisPoints[i - 1].x)), Mathf.Lerp(-10f, 30f, poisPoints[i - 1].y / rCam.sSize.y));
					//sLeaser.sprites[i].SetPosition(detailedWaterLevelPoint(origWaterPos, rCam.sSize.y, rCam.room.waterObject, i) - camPos);
					sLeaser.sprites[i].SetPosition(new Vector2((float)poisPoints[i].X, (float)poisPoints[i].Y - 100f));
				}
				for (int i = poisPoints.Count; i < sLeaser.sprites.Length; i++)
				{
					int j = i - poisPoints.Count;
					Vector2 temp = VoronoiPointToVector(voronoiEdges[j].Mid);
					sLeaser.sprites[i].SetPosition(new Vector2(temp.x, temp.y - 100f));
				}
				*/
				if (plane.Sites != null)
				{
					for (int i = 0; i < sLeaser.sprites.Length; i++)
					{
						for (int j = 0; j < (sLeaser.sprites[i] as TriangleMesh).vertices.Length; j++)
						{

							Vector2 centroid = new Vector2((float)plane.Sites[i].X, (float)plane.Sites[i].Y);
							List<VoronoiPoint> vPoints = plane.Sites[i].ClockwisePoints.ToList<VoronoiPoint>();
							Vector2 vertice = VoronoiPointToVector(vPoints[j]);
							Vector2 final = (vertice - centroid) * rCam.room.roomSettings.GetEffectAmount(_Enums.IceWater) + centroid;

							(sLeaser.sprites[i] as TriangleMesh).MoveVertice(j, final);
							sLeaser.sprites[i].color = color;
						}
					}
				}

				// Regenerate
				if (oldAmount != cellAmount || oldGen != genType)
				{
					bool erm = RegenerateSprites(sLeaser, rCam);
				}
			}
		}

		// Unused for now
		private Vector2 detailedWaterLevelPoint(Vector2 point, float height, Water water, int a)
		{
			float horizontalPosition = point.x;
			Vector2 waterFront = new Vector2(horizontalPosition, 0f);
			Vector2 waterBack = new Vector2(horizontalPosition, 0f);

			for (int i = 0; i < 2; i++)
			{
				int num = PreviousSurfacePointFB(horizontalPosition, water, i);
				int num2 = Custom.IntClamp(num + 1, 0, water.surface.GetLength(0) - 1);
				float t = Mathf.InverseLerp(water.surface[num, i].defaultPos.x + water.surface[num, i].pos.x, water.surface[num2, i].defaultPos.x + water.surface[num2, i].pos.x, horizontalPosition);
				if (i == 0)
					waterFront.y = Mathf.Lerp(water.surface[num, 0].defaultPos.y + water.surface[num, 0].pos.y, water.surface[num2, 0].defaultPos.y + water.surface[num2, 0].pos.y, t);
				else
					waterBack.y = Mathf.Lerp(water.surface[num, 1].defaultPos.y + water.surface[num, 1].pos.y, water.surface[num2, 1].defaultPos.y + water.surface[num2, 1].pos.y, t);
			}

			return Vector2.Lerp(waterFront, waterBack, (float)poisPoints[a - 1].Y / height);
		}

		private int PreviousSurfacePointFB(float horizontalPosition, Water water, int i)
		{
			int num = Mathf.Clamp(Mathf.FloorToInt((horizontalPosition + 250f) / water.triangleWidth) + 2, 0, water.surface.GetLength(0) - 1);
			while (num > 0 && horizontalPosition < water.surface[num, i].defaultPos.x + water.surface[num, i].pos.x)
			{
				num--;
			}
			return num;
		}
		
		// Applies color to sprites
		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			if (rCam.room.roomSettings.GetEffect(_Enums.IceWater) != null)
			{
				/*
				for (int i = 0; i < poisPoints.Count; i++)
				{
					// Yellow is Random Vertices
					sLeaser.sprites[i].color = UnityEngine.Color.yellow;
				}
				for (int i = poisPoints.Count; i < sLeaser.sprites.Length; i++)
				{
					// Magenta is voronoi vertices
					sLeaser.sprites[i].color = UnityEngine.Color.magenta;
				}
				*/
				for (int i = 0; i < sLeaser.sprites.Length; i++)
				{
					for (int j = 0; j < (sLeaser.sprites[i] as TriangleMesh).vertices.Length; j++)
					{
						sLeaser.sprites[j].color = color;
					}
				}
			}
		}

		public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
		{
			newContainer ??= rCam.ReturnFContainer("Water");
			for (int i = 0; i < sLeaser.sprites.Length; i++)
				newContainer.AddChild(sLeaser.sprites[i]);
		}

		public static Vector2 VoronoiPointToVector(VoronoiPoint point)
		{
			return new Vector2((float)point.X, (float)point.Y);
		}
	}
}
