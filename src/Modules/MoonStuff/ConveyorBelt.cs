using RegionKit.Extras;
using Unity.Mathematics;

using static RegionKit.Modules.MoonStuff.ConveyorBeltType;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class ConveyorBelt : UpdatableAndDeletable
	{
		private const float BaseSpeed = 1f / 400f;

		public PlacedObject placedObject;
		public ConveyorBeltData Data => placedObject.data as ConveyorBeltData;

		private int TotalTrackPieces => Math.Max(0, Data.Size.x - 6);
		private int TotalGears => Math.Max(0, Data.Covers);
		private int TotalPips => TotalTrackPieces * 4 + 32;


		public float time;

		private bool hasInitGeo = false;
		private bool firstInitGeo = true;
		private Dictionary<IntVector2, Room.Tile.TerrainType> overriddenGeo = [];

		private Vector2 lastPObjPos;
		private IntVector2 lastPObjSize;

		public bool Reversed => Data.reversed;
		public float speed => Data.speed;

		private bool hasInitDynamicLevelElements;
		private DynamicLevelAtlasElement leftSide;
		private DynamicLevelAtlasElement rightSide;
		private List<DynamicLevelAtlasElement> trackPieces;
		private List<DynamicLevelAtlasElement> gears;
		private List<DynamicLevelAtlasElement> pips;

		public ConveyorBelt(PlacedObject placedObject, Room room) : base()
		{
			this.placedObject = placedObject;
			this.room = room;
			lastPObjPos = placedObject.pos;
			lastPObjSize = Data.Size;

			trackPieces = [];
			gears = [];
			pips = [];
			InitDynamicLevelElements();
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			if (!Reversed)
			{
				time -= BaseSpeed * speed;
				if (time < 0f)
				{
					time += 1f;
				}
			}
			else
			{
				time += BaseSpeed * speed;
				if (time >= 1f)
				{
					time -= 1f;
				}
			}

			if (placedObject.pos != lastPObjPos)
			{
				hasInitGeo = false;
				lastPObjPos = placedObject.pos;
			}
			if (Data.Size != lastPObjSize)
			{
				hasInitGeo = false;
				lastPObjSize = Data.Size;
				InitDynamicLevelElements();
			}

			IntVector2 pos = room.GetTilePosition(placedObject.pos);
			IntRect top = new IntRect(pos.x, pos.y + 2, pos.x + Data.Size.x, pos.y + 3);
			IntRect bottom = new IntRect(pos.x, pos.y - 1, pos.x + Data.Size.x, pos.y);

			float radius = 30f;
			float flatWidth = (Data.Size.x * 20f) - 60f;
			float perimeter = (2f * flatWidth) + (2f * Mathf.PI * radius);

			//float M = Mathf.Lerp(0f, perimeter, 1f / TotalPips);
			float M = perimeter / 20f / TotalPips;

			float S = Reversed ? speed : -speed;

			for (int i = 0; i < room.physicalObjects.Length; i++)
			{
				for (int o = 0; o < room.physicalObjects[i].Count; o++)
				{
					PhysicalObject obj = room.physicalObjects[i][o];

					float V = M * (S / obj.surfaceFriction / obj.airFriction);

					for (int b = 0; b < obj.bodyChunks.Length; b++)
					{
						BodyChunk chunk = obj.bodyChunks[b];

						if (Custom.InsideRect(room.GetTilePosition(chunk.pos), top)) // top side
						{
							//Push(chunk, new Vector2(V, 0f));
							Push(obj, new Vector2(V, 0f));
							break;
						}
						else if (Custom.InsideRect(room.GetTilePosition(chunk.pos), bottom)) // bottom side
						{
							//Push(chunk, new Vector2(-V, 0f));
							Push(obj, new Vector2(-V, 0f));
							break;
						}
					}
				}
			}

			UpdateDynamicLevelElements();

			if (hasInitGeo) return;
			hasInitGeo = true;
			InitGeo();
		}

		private void InitGeo()
		{
			if (firstInitGeo)
			{
				firstInitGeo = false;
			}
			else
			{
				foreach ((IntVector2 pos, Room.Tile.TerrainType terrain) in overriddenGeo)
				{
					room.GetTile(pos).Terrain = terrain;
				}
				overriddenGeo.Clear();
			}

			for (int x = 0; x < Data.Size.x; x++)
			{
				for (int y = 0; y < 3; y++)
				{
					IntVector2 tilePos = room.GetTilePosition(placedObject.pos) + new IntVector2(x, y);
					if (!room.IsPositionInsideBoundries(tilePos)) continue;

					Room.Tile tile = room.GetTile(tilePos);
					overriddenGeo.Add(tilePos, tile.Terrain);
					if ((x == 0 || x == Data.Size.x - 1) && (y == 0 || y == Data.Size.y - 1))
					{
						tile.Terrain = Room.Tile.TerrainType.Slope;
					}
					else
					{
						tile.Terrain = Room.Tile.TerrainType.Solid;
					}
				}
			}
		}

		public void Push(BodyChunk chunk, Vector2 vel)
		{
			chunk.vel += vel;
		}

		public void Push(PhysicalObject obj, Vector2 vel)
		{
			for (int i = 0; i < obj.bodyChunks.Length; i++)
			{
				obj.bodyChunks[i].vel += vel;
			}
		}

		public override void Destroy()
		{
			base.Destroy();
			UninitDynamicLevelElements();
		}

		private void InitDynamicLevelElements()
		{
			if (hasInitDynamicLevelElements)
			{
				UninitDynamicLevelElements();
			}
			hasInitDynamicLevelElements = true;

			leftSide = new DynamicLevelAtlasElement(Vector2.zero, Vector2.one, "ConveyorBelt_TrackLeft", 4);
			rightSide = new DynamicLevelAtlasElement(Vector2.zero, Vector2.one, "ConveyorBelt_TrackRight", 4);

			room.AddObject(leftSide);
			room.AddObject(rightSide);

			for (int i = 0; i < TotalTrackPieces; i++)
			{
				var dle = new DynamicLevelAtlasElement(Vector2.zero, Vector2.one, "ConveyorBelt_Track", 4);
				trackPieces.Add(dle);
				room.AddObject(dle);
			}

			for (int i = 0; i < TotalGears; i++)
			{
				var dle = new DynamicLevelAtlasElement(Vector2.zero, Vector2.one, "ConveyorBelt_Gear", 3);
				gears.Add(dle);
				room.AddObject(dle);
			}

			for (int i = 0; i < TotalPips; i++)
			{
				var dle = new DynamicLevelAtlasElement(Vector2.zero, Vector2.one, "ConveyorBelt_Pip", 3);
				pips.Add(dle);
				room.AddObject(dle);
			}

			UpdateDynamicLevelElements();
		}

		private void UpdateDynamicLevelElements()
		{
			Vector2 leftTrackPos = room.MiddleOfTile(placedObject.pos) + new Vector2(20f, 20f);
			Vector2 rightTrackPos = room.MiddleOfTile(placedObject.pos) + new Vector2((Data.Size.x * 20f) - 40f, 20f);

			leftSide.pos = leftTrackPos;
			rightSide.pos = rightTrackPos;

			for (int i = 0; i < trackPieces.Count; i++)
			{
				DynamicLevelAtlasElement track = trackPieces[i];
				track.pos = leftTrackPos + new Vector2(20f * (i + 2), 0);
			}

			float radius = 30f;
			float flatWidth = (Data.Size.x * 20f) - 60f;
			float perimeter = (2f * flatWidth) + (2f * Mathf.PI * radius);
			for (int i = 0; i < gears.Count; i++)
			{
				DynamicLevelAtlasElement gear = gears[i];

				gear.pos = room.MiddleOfTile(new Vector2(Custom.LerpMap(i, 0, gears.Count - 1, leftTrackPos.x, rightTrackPos.x), leftTrackPos.y));
				gear.rotation = math.fmod(time, 1f) * perimeter * 2f;
			}

			for (int i = 0; i < pips.Count; i++)
			{
				float t = math.fmod(Custom.LerpMap(i, 0, pips.Count, 0f, perimeter) + (math.fmod(time, 1f) * perimeter), perimeter);
				t = Mathf.Abs(t - perimeter);

				Vector2 pipPos = leftTrackPos + new Vector2(0f, -30f);
				float angle = 0f;

				if (t < flatWidth)
				{
					pipPos += new Vector2(t, 0);
					angle = 180f;
				}
				else if (t < flatWidth + (Mathf.PI * radius))
				{
					pipPos += new Vector2(flatWidth + radius * Mathf.Sin((t - flatWidth) / radius), radius - (radius * Mathf.Cos((t - flatWidth) / radius)));
					angle = Custom.AimFromOneVectorToAnother(rightTrackPos, pipPos);
				}
				else if (t < (2 * flatWidth) + (Mathf.PI * radius))
				{
					pipPos += new Vector2(((Mathf.PI * radius) + (2 * flatWidth)) - t, 2 * radius);
				}
				else
				{
					pipPos += new Vector2(-radius * Mathf.Sin((t - (2 * flatWidth) - (Mathf.PI * radius)) / radius), radius + (radius * Mathf.Cos((t - (2 * flatWidth) - (Mathf.PI * radius)) / radius)));
					angle = Custom.AimFromOneVectorToAnother(leftTrackPos, pipPos);
				}

				DynamicLevelAtlasElement pip = pips[i];
				pip.pos = pipPos;
				pip.rotation = angle;
			}
		}

		private void UninitDynamicLevelElements()
		{
			leftSide.Destroy();
			rightSide.Destroy();
			foreach (var track in trackPieces)
			{
				track.Destroy();
			}
			foreach (var gear in gears)
			{
				gear.Destroy();
			}
			foreach (var pip in pips)
			{
				pip.Destroy();
			}

			leftSide = null;
			rightSide = null;
			trackPieces.Clear();
			gears.Clear();
			pips.Clear();

			hasInitDynamicLevelElements = false;
		}
	}
}
