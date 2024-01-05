using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EffExt;
using RegionKit.Modules.RoomSlideShow;

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
		public UnityEngine.Color color;
		public float height;
		public IceWater iceWater;
		public int seed;


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
		static List<Vector2> poisPoints = new List<Vector2>();
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

		public void SetValues(UnityEngine.Color color, Room room, float height, int seed)
		{
			UnityEngine.Random.InitState(seed);

		}

		public void calculateNewVerts(Room room)
		{
			
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			if (rCam.room.roomSettings.GetEffect(_Enums.IceWater) != null)
			{
				sLeaser.sprites = new FSprite[1];
				// Current input size is: (3240.0, 768.0)
				// This is where the code randomly runs infinitely (sometimes it doesn't but often generates a lot of extra points in the upper size of the grid)
				poisPoints = Poisson.GeneratePoint(100f, new Vector2(rCam.sSize.x + 220f, rCam.sSize.y));
				LogMessage(poisPoints.Count);
				Array.Resize<FSprite>(ref sLeaser.sprites, 1 + poisPoints.Count);

				for (int i = 1; i < sLeaser.sprites.Length; i++)
				{
					sLeaser.sprites[i] = new FSprite("pixel")
					{
						anchorX = 0.5f,
						anchorY = 0.5f,
						scale = 4f
					};
				}

				sLeaser.sprites[0] = new FSprite("pixel")
				{
					anchorX = 0.5f,
					anchorY = 0.5f,
					scale = 1f
				};
				AddToContainer(sLeaser, rCam, null);
			}
		}

		// Draws sprites on screen
		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (rCam.room.roomSettings.GetEffect(_Enums.IceWater) != null)
			{
				for (int i = 1; i < sLeaser.sprites.Length; i++)
				{
					//Vector2 origWaterPos = rCam.ApplyDepth(new Vector2(poisPoints[i - 1].x - 220f, room.FloatWaterLevel(poisPoints[i - 1].x - 220f)), Mathf.Lerp(-10f, 30f, poisPoints[i - 1].y));
					//sLeaser.sprites[i].SetPosition(detailedWaterLevelPoint(origWaterPos, rCam.sSize.y, rCam.room.waterObject, i) - camPos);
					sLeaser.sprites[i].SetPosition(poisPoints[i - 1]);
				}
				sLeaser.sprites[0].SetPosition(Vector2.zero);
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

			return Vector2.Lerp(waterFront, waterBack, poisPoints[a - 1].y / height);
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
			for (int i = 0; i < sLeaser.sprites.Length; i++)
				sLeaser.sprites[i].color = UnityEngine.Color.magenta;
		}

		public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
		{
			newContainer ??= rCam.ReturnFContainer("Water");
			for (int i = 0; i < sLeaser.sprites.Length; i++)
				newContainer.AddChild(sLeaser.sprites[i]);
		}
	}


	internal static class Poisson
	{
		static bool is_valid(List<Vector2> samples, int[,] grid, Vector2 sample, Vector2 sample_zone, float radius, float cell_size)
		{
			if (sample.x < sample_zone.x && sample.x >= 0 && sample.y < sample_zone.y && sample.y >= 0)
			{
				int x = (int)(sample.x / cell_size);
				int y = (int)(sample.y / cell_size);
				int offset_x = Mathf.Max(0, x - 2);
				int out_x = Mathf.Min(x + 2, grid.GetLength(0) - 1);
				int offset_y = Mathf.Max(0, y - 2);
				int out_y = Mathf.Min(y + 2, grid.GetLength(1) - 1);

				for (int i = offset_x; i < out_x; i++)
				{
					for (int j = offset_y; j < out_y; j++)
					{
						int s_index = grid[i, j] - 1;
						if (s_index != -1)
						{
							float _distance = (sample - samples[s_index]).sqrMagnitude;
							if (_distance < radius * radius)
							{
								return false;
							}
						}
					}
				}
				return true;
			}
			return false;
		}
		public static List<Vector2> GeneratePoint(float radius, Vector2 grid_size, int k = 30)
		{
			float cell_size = radius / Mathf.Sqrt(2);

			//to get the columns we gonna divide the width/ cell_size and rows ....
			int[,] grid = new int[Mathf.CeilToInt(grid_size.x / cell_size), Mathf.CeilToInt(grid_size.y / cell_size)];

			List<Vector2> samples = new List<Vector2>();
			List<Vector2> spawn_samples = new List<Vector2>();

			spawn_samples.Add(grid_size / 2);
			while (spawn_samples.Count > 0)
			{
				LogMessage("Running sample" + spawn_samples.Count);
				int index = UnityEngine.Random.Range(0, spawn_samples.Count);
				Vector2 current_spawn_sample = spawn_samples[index];
				bool rejected_sample = true;
				for (int i = 0; i < k; i++)
				{
					float angle_offset = UnityEngine.Random.value * Mathf.PI * 2;
					//rotate a vector at a given angle
					float x = Mathf.Sin(angle_offset);
					float y = Mathf.Cos(angle_offset);
					Vector2 offset_direction = new Vector2(x, y);

					float new_magnitude = UnityEngine.Random.Range(radius, 2 * radius);
					offset_direction *= new_magnitude;

					Vector2 sample = current_spawn_sample + offset_direction;
					if (is_valid(samples, grid, sample, grid_size, radius, cell_size))
					{
						samples.Add(sample);
						spawn_samples.Add(sample);
						grid[(int)(sample.x / cell_size), (int)(sample.y / cell_size)] = samples.Count;
						rejected_sample = false;
						break;
					}
				}

				if (rejected_sample)
				{
					spawn_samples.RemoveAt(index);
				}
			}
			return samples;
		}

	}
}
