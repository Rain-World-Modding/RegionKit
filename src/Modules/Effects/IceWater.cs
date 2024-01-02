using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EffExt;

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
					.AddIntField("Seed", 1, 1000, 1)
					.SetUADFactory((room, data, firstTimeRealized) => new IceWaterUAD(data))
					.SetCategory("RegionKit")
					.Register();
			}
			catch (Exception ex)
			{
				LogWarning($"Error on eff IceWater init {ex}");
			}
		}


	}

	 internal class IceWaterUAD : UpdatableAndDeletable
	{
		public EffectExtraData EffectData { get; }
		public Color color;
		public float height;
		public IceWater iceWater;
		public int seed;


		public IceWaterUAD(EffectExtraData effectData)
		{
			EffectData = effectData;
			color = Color.white;
			height = 1;
			iceWater = new IceWater();
			seed = 1;
		}

		public override void Update(bool eu)
		{
			color.r = EffectData.GetFloat("Red");
			color.g = EffectData.GetFloat("Green");
			color.b = EffectData.GetFloat("Blue");
			height = EffectData.GetFloat("Height");
			seed = EffectData.GetInt("Seed");


			if (iceWater != null && room.BeingViewed)
			{
				iceWater.SetValues(color, room, height, seed);
			}
		}
	}


	internal class IceWater : CosmeticSprite
	{
		public static readonly object iceWaterSprite = new();
		private static List<Vector2[]> polygons = new List<Vector2[]>();
		private static List<int[]> surfaceIndex = new List<int[]>();
		float oldWidth = 0f;
		private static Vector2[] vertices = new Vector2[0];

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

		public void SetValues(Color color, Room room, float height, int seed)
		{
			UnityEngine.Random.InitState(seed);
			float width = room.roomSettings.GetEffectAmount(_Enums.IceWater) * 10f;
			width += 0.5f;
			if (width != oldWidth)
			{
				// Updates length of vertices from original points
				foreach (Vector2[] polygon in polygons)
				{
					for (int i = 1; i < polygon.Length; i++)
					{
						polygon[i] = polygon[i].normalized * width;
					}
				}
			}
			oldWidth = width;
			calculateNewVerts(room);
		}

		public void calculateNewVerts(Room room)
		{
			for (int i = 0; i < surfaceIndex.Count; i++)
			{
				polygons.ElementAt(i)[0] = room.waterObject.surface[surfaceIndex[i][0], surfaceIndex[i][1]].pos;
				//Debug.Log("i is " + i +"\nsurfaceIndex[0] is " + surfaceIndex[i][0] +"\nsurfaceIndex[1] is " + surfaceIndex[i][1] +"\nsurface pos is " + room.waterObject.surface[surfaceIndex[i][0], surfaceIndex[i][1]].pos +"\n\n");
			}
			// Calculate corrsponding points from center
			int vertIndex = 1;
			for (int i = 0; i < polygons.Count; i++)
			{
				for (int j = 1; j < polygons.ElementAt(i).Length; j++)
				{
					vertices[vertIndex++] = polygons.ElementAt(i)[j] + polygons.ElementAt(i)[0];
				}
				vertIndex++;
			}
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			Debug.Log("Got into initiate sprites");
			polygons.Clear();
			surfaceIndex.Clear();
			if (rCam.room.roomSettings.GetEffect(_Enums.IceWater) != null)
			{
				sLeaser.sprites = new FSprite[1];
				int vertIndex = 0;
				for (int i = 0; i < rCam.room.waterObject.surface.GetLength(0); i++)
				{
					Debug.Log("Water surface count for second part is " + rCam.room.waterObject.surface.GetLength(1));
					for (int j = 0; j < rCam.room.waterObject.surface.GetLength(1); j++)
					{
						float noise = UnityEngine.Random.Range(0f, 1f);
						if (noise >= 0.5)
						{
							surfaceIndex.Add(new int[] {i, j});
							// First element of each array is the original point from which the other points will spread from
							polygons.Add(new Vector2[UnityEngine.Random.Range(3, 6)]);
							polygons.ElementAt(vertIndex)[0] = rCam.room.waterObject.surface[i, j].pos;
							for (int k = 1; k < polygons.ElementAt(vertIndex).Length; k++)
							{
								// Generates random direction for ice to form polygon
								polygons.ElementAt(vertIndex)[k] = UnityEngine.Random.insideUnitCircle.normalized;
							}
							vertIndex++;
						}
					}
				}
				int vertNum = 0;
				foreach (Vector2[] polygon in polygons)
				{
					vertNum += polygon.Length;
				}
				vertices = new Vector2[vertNum];
				for (int i = 0; i < polygons.Count; i++)
				{
					for (int j = 0; j < polygons.ElementAt(i).Length; j++)
					{
						vertices[j] = polygons.ElementAt(i)[j];
					}
				}

				// Sets triangle vertices
				Debug.Log("Polygons count " + polygons.Count);
				List<TriangleMesh.Triangle> tris = new List<TriangleMesh.Triangle>();
				int trisIndex = 0;
				vertIndex = 0;
				for (int i = 0; i < polygons.Count; i++)
				{
					for (int j = 1; j < polygons[i].Length - 1; j++)
					{
						// Logs
						/*
						Debug.Log("trisIndex: " + trisIndex + " with poly vert length " + polygons[i].Length +
							"\ntempIndex a: " + vertIndex + // Center vertice
							"\ntempIndex b: " + (vertIndex + j) +
							"\ntempIndex c: " + (vertIndex + j + 1)
							 );*/
						tris.Add(new TriangleMesh.Triangle(vertIndex, vertIndex + j, vertIndex + j + 1));
						trisIndex++;
					}
					vertIndex += polygons[i].Length;
				}

				sLeaser.sprites[0] = new TriangleMesh("Futile_White", tris.ToArray(), true)
				{
					data = iceWaterSprite,
					shader = FShader.Basic,
					color = Color.white
				};

				sLeaser.sprites[0].isVisible = true;
				Debug.Log("vertices length " + vertices.Length);
				Debug.Log("tris length" + tris.Count);
				AddToContainer(sLeaser, rCam, null);
			}
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (rCam.room.roomSettings.GetEffect(_Enums.IceWater) != null)
			{
				if (sLeaser.sprites.FirstOrDefault(x => x.data == iceWaterSprite) is TriangleMesh iceMesh)
				{
					int offset = rCam.room.waterObject.PreviousSurfacePoint(camPos.x - 30f);
					// Calculate vertex positions
					for (int i = 0; i < vertices.Length; i++)
					{
						iceMesh.MoveVertice(i, vertices[i]);
					}
				}
			}
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			sLeaser.sprites[0].color = Color.white;
		}

		public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
		{
			newContainer ??= rCam.ReturnFContainer("Water");
			rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[0]);
		}
	}
}
