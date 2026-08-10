using Unity.Mathematics;

using static RegionKit.Modules.MoonStuff.ConveyorBeltType;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class ConveyorBelt : UpdatableAndDeletable, IDrawable
	{
		public PlacedObject PlacedObject;

		public int leftbelt;

		public int beltstart;

		public int beltend;

		public int rightbelt;

		public int gearstart;

		public int gearend;

		public int pipstart;

		public int pipend;

		public float time;

		private bool GeoInit;

		public bool Reversed => (PlacedObject.data as ConveyorBeltData).reversed;

		public float speed => (PlacedObject.data as ConveyorBeltData).speed;

		public ConveyorBelt(PlacedObject placedObject, Room room) : base()
		{
			this.PlacedObject = placedObject;
			this.room = room;
			GeoInit = false;

			leftbelt = 0;
			beltstart = leftbelt + 1;
			beltend = beltstart + (PlacedObject.data as ConveyorBeltData).Size.x - 6;
			rightbelt = beltend + 1;
			gearstart = rightbelt + 1;
			gearend = gearstart + ((PlacedObject.data as ConveyorBeltData).Covers + 1);
			pipstart = gearend + 1;
			pipend = pipstart + (((PlacedObject.data as ConveyorBeltData).Size.x - 6) * 4) + 32;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			if (!Reversed)
			{
				time -= 0.0025f * speed;
				if (time < 0f)
				{
					time = 4f;
				}
			}
			else
			{
				time += 0.0025f * speed;
				if (time > 4f)
				{
					time = 0f;
				}
			}

			IntVector2 pos = room.GetTilePosition(PlacedObject.pos);
			IntRect top = new IntRect(pos.x - 1, pos.y + 2, pos.x + (PlacedObject.data as ConveyorBeltData).Size.x + 1, pos.y + 3);
			IntRect bottom = new IntRect(pos.x - 1, pos.y - 1, pos.x + (PlacedObject.data as ConveyorBeltData).Size.x + 1, pos.y);

			float R = 30f;
			float W = ((PlacedObject.data as ConveyorBeltData).Size.x * 20f) - 60f;
			float num = (2f * W) + (2f * Mathf.PI * R);

			float M = Mathf.Lerp(0f, num, ((float)(1) / (float)(pipend - pipstart)));

			float S = Reversed ? speed : -speed;

			for (int i = 0; i < room.physicalObjects.Length; i++)
			{
				for (int o = 0; o < room.physicalObjects[i].Count; o++)
				{
					PhysicalObject obj = room.physicalObjects[i][o];

					float V = (M / 20f) * (S / obj.surfaceFriction);

					for (int b = 0; b < obj.bodyChunks.Length; b++)
					{
						BodyChunk chunk = obj.bodyChunks[b];

						if (Custom.InsideRect(room.GetTilePosition(chunk.pos), top)) // top side
						{
							Push(obj, new Vector2(V, 0f));
							break;
						}
						else if (Custom.InsideRect(room.GetTilePosition(chunk.pos), bottom)) // bottom side
						{
							Push(obj, new Vector2(V, 0f));
							break;
						}
					}
				}
			}

			if (GeoInit) return;
			GeoInit = true;

			for (int x = 0; x < (PlacedObject.data as ConveyorBeltData).Size.x; x++)
			{
				for (int y = 0; y < 3; y++)
				{
					IntVector2 tile = room.GetTilePosition(PlacedObject.pos) + new IntVector2(x, y);

					if ((x == 0 || x == (PlacedObject.data as ConveyorBeltData).Size.x - 1) && (y == 0 || y == (PlacedObject.data as ConveyorBeltData).Size.y - 1))
					{
						room.GetTile(tile).Terrain = Room.Tile.TerrainType.Slope;
					}
					else
					{
						room.GetTile(tile).Terrain = Room.Tile.TerrainType.Solid;
					}
				}
			}
		}

		public void Push(PhysicalObject obj, Vector2 vel)
		{
			for (int i = 0; i < obj.bodyChunks.Length; i++)
			{
				obj.bodyChunks[i].vel += vel;
			}
		}

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[pipend];

			sLeaser.sprites[leftbelt] = new FSprite("ConveyorBelt_TrackLeft");
			sLeaser.sprites[leftbelt].anchorX = 0f;
			sLeaser.sprites[leftbelt].anchorY = 0f;
			sLeaser.sprites[leftbelt].alpha = 0.9f;
			for (int i = beltstart; i <= beltend; i++)
			{
				sLeaser.sprites[i] = new FSprite("ConveyorBelt_Track");
				sLeaser.sprites[i].anchorX = 0f;
				sLeaser.sprites[i].anchorY = 0f;
				sLeaser.sprites[i].alpha = 0.9f;
			}

			sLeaser.sprites[rightbelt] = new FSprite("ConveyorBelt_TrackRight");
			sLeaser.sprites[rightbelt].anchorX = 1f;
			sLeaser.sprites[rightbelt].anchorY = 0f;
			sLeaser.sprites[rightbelt].alpha = 0.9f;

			for (int i = gearstart; i <= gearend; i++)
			{
				sLeaser.sprites[i] = new FSprite("ConveyorBelt_Gear");
			}

			for (int i = pipstart; i < pipend; i++)
			{
				sLeaser.sprites[i] = new FSprite("ConveyorBelt_Pip");
			}

			for (int i = 0; i < sLeaser.sprites.Length; i++)
			{
				sLeaser.sprites[i].shader = room.game.rainWorld.Shaders["ColoredSprite2"];
			}

			AddToContainer(sLeaser, rCam, null);
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			Vector2 bottomleft = room.MiddleOfTile(PlacedObject.pos) - new Vector2(10f, 10f);
			Vector2 bottomright = room.MiddleOfTile(PlacedObject.pos) + new Vector2(((PlacedObject.data as ConveyorBeltData).Size.x * 20f) - 10f, -10f);

			sLeaser.sprites[leftbelt].SetPosition(bottomleft - camPos);
			sLeaser.sprites[rightbelt].SetPosition(bottomright - camPos);

			for (int i = beltstart; i <= beltend; i++)
			{
				sLeaser.sprites[i].y = bottomleft.y - camPos.y;
				sLeaser.sprites[i].x = bottomleft.x + (20f * (i + 1)) - camPos.x;
			}

			float R = 30f;
			float W = ((PlacedObject.data as ConveyorBeltData).Size.x * 20f) - 60f;
			float num = (2f * W) + (2f * Mathf.PI * R);

			for (int i = gearstart; i <= gearend; i++)
			{
				Vector2 a = bottomleft + new Vector2(30f, 30f);
				Vector2 b = bottomright + new Vector2(-30f, 30f);

				sLeaser.sprites[i].SetPosition(room.MiddleOfTile(new Vector2(a.x + ((b.x - a.x) * ((float)(i - (float)gearstart) / (float)(gearend - gearstart))), a.y)) - camPos);
				sLeaser.sprites[i].rotation = math.fmod(time, 1f) * num * 2f;
			}

			for (int i = pipstart; i < pipend; i++)
			{
				float t = math.fmod(Mathf.Lerp(0f, num, ((float)(i - pipstart) / (float)(pipend - pipstart))) + (math.fmod(time, 1f) * num), num);
				t = Mathf.Abs(t - num);

				Vector2 vector = bottomleft + new Vector2(30f, 0f);
				float angle = 0f;

				if (t < W)
				{
					vector += new Vector2(t, 0);
					angle = 180f;
				}
				else if (t < W + (Mathf.PI * R))
				{
					vector += new Vector2(W + R * Mathf.Sin((t - W) / R), R - (R * Mathf.Cos((t - W) / R)));
					angle = Custom.AimFromOneVectorToAnother(bottomright + new Vector2(-30f, 30f), vector);
				}
				else if (t < (2 * W) + (Mathf.PI * R))
				{
					vector += new Vector2(((Mathf.PI * R) + (2 * W)) - t, 2 * R);
				}
				else
				{
					vector += new Vector2(-R * Mathf.Sin((t - (2 * W) - (Mathf.PI * R)) / R), R + (R * Mathf.Cos((t - (2 * W) - (Mathf.PI * R)) / R)));
					angle = Custom.AimFromOneVectorToAnother(bottomleft + new Vector2(30f, 30f), vector);
				}

				sLeaser.sprites[i].SetPosition(vector - camPos);
				sLeaser.sprites[i].rotation = angle;
			}

			if (base.slatedForDeletetion || room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
			}
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
		}

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			if (newContatiner == null)
			{
				newContatiner = rCam.ReturnFContainer("Items");
			}

			for (int i = 0; i < sLeaser.sprites.Length; i++)
			{
				newContatiner.AddChild(sLeaser.sprites[i]);
			}
		}
	}
}
