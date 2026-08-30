using Random = UnityEngine.Random;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class SandFall : UpdatableAndDeletable, IDrawable
	{
		public class SandGrain : CosmeticSprite
		{
			public float life;

			public float lastLife;

			public int lifeTime;

			public Vector2 lastLastPos;

			public Vector2 lastLastLastPos;

			public Color[] colors;

			public float randomLightness;

			public float lastRandomLightness;

			public float width;

			public bool mustExitTerrainOnceToBeDestroyedByTerrain;

			public SandGrain(Vector2 pos, Vector2 vel) : base()
			{
				life = 1f;
				lastLife = 1f;
				base.pos = pos;
				lastPos = pos;
				lastLastPos = pos;
				lastLastLastPos = pos;
				base.vel = vel;
				width = 2f;
				lifeTime = Random.Range(10, 120);
			}

			public override void Update(bool eu)
			{
				lastLastLastPos = lastLastPos;
				lastLastPos = lastPos;
				vel.y -= 0.9f * room.gravity;
				lastLife = life;
				life -= 1f / (float)lifeTime;
				if (lastLife <= 0f || pos.y < room.terrain.SnapToTerrain(pos).y || (room.GetTile(pos).Terrain == Room.Tile.TerrainType.Solid && !mustExitTerrainOnceToBeDestroyedByTerrain))
				{
					Destroy();
				}

				if (mustExitTerrainOnceToBeDestroyedByTerrain && !room.GetTile(pos).Solid)
				{
					mustExitTerrainOnceToBeDestroyedByTerrain = false;
				}

				lastRandomLightness = randomLightness;
				randomLightness = Mathf.Lerp(Random.value, 1f, 0.6f);
				base.Update(eu);
			}

			public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				sLeaser.sprites = new FSprite[2];
				TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[1]
				{
					new TriangleMesh.Triangle(0, 1, 2)
				};
				TriangleMesh triangleMesh = new TriangleMesh("RainWorld_White", tris, customColor: false, atlasedImage: true);
				sLeaser.sprites[0] = triangleMesh;
				sLeaser.sprites[1] = new FSprite("Circle20", quadType: false);
				AddToContainer(sLeaser, rCam, null);
			}

			public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
			{
				Vector2 vector = Vector2.Lerp(lastPos, pos, timeStacker);
				Vector2 vector2 = Vector2.Lerp(lastLastLastPos, lastLastPos, timeStacker);
				if (lastLife > 0f && life <= 0f)
				{
					vector2 = Vector2.Lerp(vector2, vector, timeStacker);
				}

				Vector2 vector3 = Custom.PerpendicularVector((vector - vector2).normalized);
				(sLeaser.sprites[0] as TriangleMesh).MoveVertice(0, vector + vector3 * width - camPos);
				(sLeaser.sprites[0] as TriangleMesh).MoveVertice(1, vector - vector3 * width - camPos);
				(sLeaser.sprites[0] as TriangleMesh).MoveVertice(2, vector2 - camPos);
				float num = Mathf.Lerp(lastRandomLightness, randomLightness, timeStacker) * Mathf.InverseLerp(1f, 0.5f, rCam.currentPalette.darkness);
				if (num > 0.5f && Random.value < 0.3f)
				{
					sLeaser.sprites[1].x = Mathf.Lerp(lastPos.x, pos.x, timeStacker) - camPos.x;
					sLeaser.sprites[1].y = Mathf.Lerp(lastPos.y, pos.y, timeStacker) - camPos.y;
					sLeaser.sprites[1].scale = Random.value * 0.5f * Mathf.InverseLerp(0.5f, 1f, num) * (width / 2);
					sLeaser.sprites[1].isVisible = true;
				}
				else
				{
					sLeaser.sprites[1].isVisible = false;
				}

				if (rCam.terrainPalette != null)
				{
					sLeaser.sprites[1].color = rCam.terrainPalette.LightDustColor;
				}

				float num2 = Mathf.InverseLerp(0f, 0.5f, num);
				if (num2 < 0.5f)
				{
					sLeaser.sprites[0].color = Color.Lerp(colors[0], colors[1], Mathf.InverseLerp(0f, 0.5f, num2));
				}
				else
				{
					sLeaser.sprites[0].color = Color.Lerp(colors[1], colors[2], Mathf.InverseLerp(0.5f, 1f, num2));
				}

				base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			}

			public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
			{
				base.AddToContainer(sLeaser, rCam, newContatiner);
			}

			public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
			{
				if (rCam.terrainPalette == null)
				{
					colors = new Color[3]
					{
						palette.blackColor,
						Color.Lerp(palette.blackColor, Color.white, 0.5f),
						Color.white
					};
				}
				else
				{
					colors = new Color[3]
					{
						rCam.terrainPalette.DarkDustColor,
						Color.Lerp(rCam.terrainPalette.DarkDustColor, rCam.terrainPalette.LightTint, 0.5f),
						rCam.terrainPalette.LightDustColor
					};
				}
			}
		}

		public IntVector2 tilePos;
		public float width;
		public float lastFlow;
		public float flow;

		public float visualDensity;
		public float lastVisualDens;

		public float[] topPos;
		public float[] bottomPos;

		public int divisions;
		public Vector2 pos;
		public float fallingWaterBottom = 0f;
		public float setFlow;
		public float originalFlow;

		public StaticSoundLoop topLoop;
		public StaticSoundLoop bottomLoop;

		public bool Flooded
		{
			get
			{
				if (room.terrain == null)
				{
					return false;
				}

				return room.terrain.SnapToTerrain(new Vector2(pos.x, pos.y)).y - 1f >= pos.y;
			}
		}

		public SandFall(Room room, IntVector2 tilePos, float flow, int width) : base()
		{
			base.room = room;
			this.tilePos = tilePos;
			this.flow = flow;
			originalFlow = flow;
			lastFlow = flow;
			setFlow = flow;
			this.width = width;
			pos = room.MiddleOfTile(tilePos) + new Vector2(-10f, 15f);

			topPos = new float[3] { pos.y, pos.y, 0f };

			float a;
			float b;
			if (room.terrain != null)
			{
				a = room.terrain.SnapToTerrain(pos).y - 1f;
				b = room.terrain.SnapToTerrain(new Vector2(pos.x + width, pos.y)).y - 1f;
			}
			else
			{
				a = 0f;
				b = 0f;
			}

			bottomPos = new float[3] { a, b, 0f };
			if (flow == 0f)
			{
				topPos[0] = bottomPos[0];
				topPos[1] = topPos[0];
			}

			divisions = width == 0 ? 1 : (int)(width / 20);
		}

		public override void Update(bool eu)
		{
			pos = Custom.IntVector2ToVector2(tilePos);
			lastFlow = flow;
			lastVisualDens = visualDensity;
			if (topPos[0] == pos.y)
			{
				visualDensity = Mathf.Lerp(visualDensity, flow, 0.1f);
			}

			if (topPos[0] == pos.y || (topPos[0] <= fallingWaterBottom))
			{
				flow = setFlow;
			}

			bottomPos[1] = bottomPos[0];
			bottomPos[0] += bottomPos[2];
			bottomPos[2] += -0.9f;
			if (room.terrain != null)
			{
				if (bottomPos[0] < room.terrain.SnapToTerrain(pos).y - 1f)
				{
					bottomPos[0] = room.terrain.SnapToTerrain(pos).y - 1f;
					bottomPos[2] = 0f;
				}
			}
			else
			{
				if (bottomPos[0] < 0)
				{
					bottomPos[0] = 0;
					bottomPos[2] = 0f;
				}
			}

			if (flow == 0f)
			{
				topPos[1] = topPos[0];
				topPos[0] += topPos[2];
				topPos[2] += -0.9f;
				if (room.terrain != null)
				{
					if (topPos[0] < room.terrain.SnapToTerrain(pos).y - 1f)
					{
						topPos[0] = room.terrain.SnapToTerrain(pos).y - 1f;
						topPos[2] = 0f;
						visualDensity = 0f;
					}
				}
				else
				{
					if (topPos[0] < 0)
					{
						topPos[0] = 0;
						topPos[2] = 0f;
						visualDensity = 0f;
					}
				}
			}
			else
			{
				topPos[0] = pos.y;
				topPos[1] = pos.y;
				topPos[2] = 0f;
				if (lastFlow == 0f)
				{
					bottomPos[0] = pos.y;
					bottomPos[1] = pos.y;
					bottomPos[2] = 0f;
				}
			}

			if (!Flooded && room.terrain != null && Random.value < flow)
			{
				int v = Random.Range((int)width / 4, (int)width / 2);
				for (int i = 0; i < v; i++)
				{
					float x = Mathf.Lerp(pos.x, pos.x + width, Random.value);
					Vector2 vector = new Vector2(room.terrain.SnapToTerrain(new Vector2(x, pos.y)).x, room.terrain.SnapToTerrain(new Vector2(x, pos.y)).y - 1f) + new Vector2(Mathf.Lerp(-1f, 1f, Random.value), Mathf.Lerp(0f, 4f, Random.value));

					if (vector.y < topPos[0])
					{
						SandGrain sandGrain = new SandGrain(vector, (Custom.DegToVec(360f * Random.value) + new Vector2(0f, 1f)) * Random.value * flow * 1.4f);
						room.AddObject(sandGrain);
						sandGrain.mustExitTerrainOnceToBeDestroyedByTerrain = true;
					}
				}
			}

			if (topLoop == null)
			{
				topLoop = new StaticSoundLoop(_Enums.Sandfall_LOOP, pos, room, 0f, 0f)
				{
					randomStartPosition = true,
					pitch = UnityEngine.Random.Range(1f, 1.1f)
				};
				bottomLoop = new StaticSoundLoop(_Enums.Sandfall_LOOP, pos, room, 0f, 0f)
				{
					randomStartPosition = true,
					pitch = UnityEngine.Random.Range(1f, 1.1f)
				};
			}
			topLoop.Update();
			topLoop.pos = new Vector2(pos.x + (width / 2f), pos.y);
			topLoop.volume = visualDensity;
			bottomLoop.Update();
			if (room.terrain != null)
			{

				bottomLoop.pos = room.terrain.SnapToTerrain(topLoop.pos);
			}
			else
			{
				bottomLoop.pos = topLoop.pos * Vector2.right;
			}
			bottomLoop.volume = visualDensity;
		}

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[1];
			sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(divisions, pointyTip: false, customColor: true);
			sLeaser.sprites[0].shader = room.game.rainWorld.Shaders["SandFall"];

			AddToContainer(sLeaser, rCam, null);
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (Flooded)
			{
				sLeaser.sprites[0].isVisible = false;
			}
			else
			{
				sLeaser.sprites[0].isVisible = true;

				for (int i = 0; i < divisions; i++)
				{
					float num = pos.x + (width * (i / (float)divisions));
					Vector2 topLeft = new Vector2(num, pos.y);

					Vector2 bottomLeft;
					if (room.terrain != null && visualDensity > 0)
					{
						bottomLeft = new Vector2(room.terrain.SnapToTerrain(new Vector2(num, pos.y)).x, room.terrain.SnapToTerrain(new Vector2(num, pos.y)).y - 1f);
					}
					else if (visualDensity > 0)
					{
						bottomLeft = new Vector2(num, 0f);
					}
					else
					{
						bottomLeft = new Vector2(num, pos.y);
					}


					float num2 = pos.x + (width * ((i + 1f) / divisions));
					Vector2 topRight = new Vector2(num2, pos.y);

					Vector2 bottomRight;
					if (room.terrain != null && visualDensity > 0)
					{
						bottomRight = new Vector2(room.terrain.SnapToTerrain(new Vector2(num2, pos.y)).x, room.terrain.SnapToTerrain(new Vector2(num2, pos.y)).y - 1f);
					}
					else if (visualDensity > 0)
					{
						bottomRight = new Vector2(num2, 0f);
					}
					else
					{
						bottomRight = new Vector2(num2, pos.y);
					}

					(sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4, topLeft - camPos);
					(sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 1, bottomLeft - camPos);
					(sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 2, topRight - camPos);
					(sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 3, bottomRight - camPos);
				}


				sLeaser.sprites[0].color = new Color(Mathf.Lerp(lastVisualDens, visualDensity, timeStacker), 0, 0);
			}

			if (base.slatedForDeletetion || room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
			}
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			if (newContatiner == null)
			{
				newContatiner = rCam.ReturnFContainer("Background");
			}

			for (int i = 0; i < sLeaser.sprites.Length; i++)
			{
				newContatiner.AddChild(sLeaser.sprites[i]);
			}
		}

	}
}
