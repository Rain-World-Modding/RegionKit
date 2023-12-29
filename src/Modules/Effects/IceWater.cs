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


		public IceWaterUAD(EffectExtraData effectData)
		{
			EffectData = effectData;
			color = Color.white;
			height = 1;
			iceWater = new IceWater();

		}

		public override void Update(bool eu)
		{
			color.r = EffectData.GetFloat("Red") / 255f;
			color.g = EffectData.GetFloat("Green") / 255f;
			color.b = EffectData.GetFloat("Blue") / 255f;
			height = EffectData.GetFloat("Height");


			if (iceWater != null && room.BeingViewed)
			{
				iceWater.SetValues(color, room, height);
			}
		}
	}


	internal class IceWater
	{
		public static readonly object iceWaterSprite = new();
		private static List<Vector2[]> polygons = new List<Vector2[]>();
		float oldWidth = 0f;
		private static Vector2[] vertices;
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
			float width = room.roomSettings.GetEffectAmount(_Enums.IceWater) * 10f;
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
		}
		private static void Water_AddToContainer(On.Water.orig_AddToContainer orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			orig(self, sLeaser, rCam, newContatiner);
			if (self.room.roomSettings.GetEffect(_Enums.IceWater) != null)
			{
				if (sLeaser.sprites.FirstOrDefault(x => x.data == iceWaterSprite) is TriangleMesh iceMesh)
				{
					rCam.ReturnFContainer("Water").AddChild(iceMesh);
					iceMesh.MoveBehindOtherNode(sLeaser.sprites[1]);
				}
			}
		}


		private static void Water_InitiateSprites(On.Water.orig_InitiateSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			int index = 0;
			polygons.Clear();
			if (self.room.roomSettings.GetEffect(_Enums.IceWater) != null)
			{
				index = sLeaser.sprites.Length;
				int vertIndex = 0;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);

				for (int i = 0; i < self.surface.GetLength(0); i++)
				{
					for (int j = 0; j < self.surface.GetLength(1); j++)
					{
						float noise = UnityEngine.Random.Range(0f, 1f);
						if (noise >= 0.5)
						{
							// First element of each array is the original point from which the other points will spread from
							polygons.Add(new Vector2[UnityEngine.Random.Range(3, 6)]);
							polygons.ElementAt(vertIndex)[0] = self.surface[i, j].pos;
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
						Debug.Log("trisIndex: " + trisIndex + " with poly vert length " + polygons[i].Length + 
							"\ntempIndex a: " + vertIndex + // Center vertice
							"\ntempIndex b: " + (vertIndex + j) +
							"\ntempIndex c: " + (vertIndex + j + 1)
							 );
						tris.Add(new TriangleMesh.Triangle(vertIndex, vertIndex + j, vertIndex + j + 1));
						trisIndex++;
					}
					vertIndex += polygons[i].Length;
				}

				sLeaser.sprites[index] = new TriangleMesh("Futile_White", tris.ToArray(), true)
				{
					data = iceWaterSprite,
					shader = FShader.Basic,
					color = Color.white
				};

				Debug.Log("vertices length " + vertices.Length);
				Debug.Log("tris length" + tris.Count);
				self.AddToContainer(sLeaser, rCam, null);
			}
		}

		private static void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (self.room.roomSettings.GetEffect(_Enums.IceWater) != null)
			{
				if (sLeaser.sprites.FirstOrDefault(x => x.data == iceWaterSprite) is TriangleMesh iceMesh)
				{
					WaterTriangleMesh waterMesh = (WaterTriangleMesh)sLeaser.sprites[0];
					int offset = self.PreviousSurfacePoint(camPos.x - 30f);
					// Calculate vertex positions
					Debug.Log("Water mesh vertices length: " + waterMesh.vertices.Length);
					Debug.Log("Vertices length: " + vertices.Length);
					for (int i = 0; i < waterMesh.vertices.Length; i++)
					{
						waterMesh.MoveVertice(i, vertices[i]);
					}
				}
			}
		}


	}
}
