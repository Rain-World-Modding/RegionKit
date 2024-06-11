using Random = UnityEngine.Random;

namespace RegionKit.Modules.Effects
{
	internal static class CircuitFliesCI
	{
		internal static void Apply()
		{
			On.InsectCoordinator.CreateInsect += InsectCoordinator_CreateInsect;
			On.InsectCoordinator.TileLegalForInsect += InsectCoordinator_TileLegalForInsect;
			On.InsectCoordinator.EffectSpawnChanceForInsect += InsectCoordinator_EffectSpawnChanceForInsect;
			On.InsectCoordinator.RoomEffectToInsectType += InsectCoordinator_RoomEffectToInsectType;
			_CommonHooks.PostRoomLoad += PostRoomLoad;
		}

		internal static void Undo()
		{
			On.InsectCoordinator.CreateInsect -= InsectCoordinator_CreateInsect;
			On.InsectCoordinator.TileLegalForInsect -= InsectCoordinator_TileLegalForInsect;
			On.InsectCoordinator.EffectSpawnChanceForInsect -= InsectCoordinator_EffectSpawnChanceForInsect;
			On.InsectCoordinator.RoomEffectToInsectType -= InsectCoordinator_RoomEffectToInsectType;
			_CommonHooks.PostRoomLoad -= PostRoomLoad;
		}


		private static bool InsectCoordinator_TileLegalForInsect(On.InsectCoordinator.orig_TileLegalForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos)
		{
			return (type == _Enums.CircuitFly) ? !room.GetTile(testPos).AnyWater && !room.GetTile(testPos).Solid : orig(type, room, testPos);
		}

		private static void PostRoomLoad(Room self)
		{
			for (int i = 0; i < self.roomSettings.effects.Count; i++)
			{
				if (self.roomSettings.effects[i].type == _Enums.CircuitFlies)
				{
					if (self.insectCoordinator == null)
					{
						self.insectCoordinator = new InsectCoordinator(self);
						self.AddObject(self.insectCoordinator);
					}
					self.insectCoordinator.AddEffect(self.roomSettings.effects[i]);
				}
			}
		}

		private static CosmeticInsect.Type InsectCoordinator_RoomEffectToInsectType(On.InsectCoordinator.orig_RoomEffectToInsectType orig, RoomSettings.RoomEffect.Type type)
		{
			return type == _Enums.CircuitFlies ? _Enums.CircuitFly : orig(type);
		}

		private static void InsectCoordinator_CreateInsect(On.InsectCoordinator.orig_CreateInsect orig, InsectCoordinator self, CosmeticInsect.Type type, Vector2 pos, InsectCoordinator.Swarm swarm)
		{
			if (!InsectCoordinator.TileLegalForInsect(type, self.room, pos))
			{
				return;
			}
			if (self.room.world.rainCycle.TimeUntilRain < Random.Range(1200, 1600))
			{
				return;
			}

			if (type == _Enums.CircuitFly)
			{
				CosmeticInsect insect = new CircuitFly(self.room, pos);

				self.allInsects.Add(insect);
				if (swarm != null)
				{
					swarm.members.Add(insect);
					insect.mySwarm = swarm;
				}
				self.room.AddObject(insect);
			}
			else
			{
				orig(self, type, pos, swarm);
			}
		}

		private static bool InsectCoordinator_EffectSpawnChanceForInsect(On.InsectCoordinator.orig_EffectSpawnChanceForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos, float effectAmount)
		{
			if (type == _Enums.CircuitFly)
			{
				return Mathf.Pow(Random.value, 1f - effectAmount) > (room.readyForAI ? room.aimap.getTerrainProximity(testPos) : 5) * 0.05f;
			}
			return orig(type, room, testPos, effectAmount);
		}
	}

	/// <summary>
	/// By Alduris
	/// Fly-like that zips around brightly sometimes
	/// </summary>
	public class CircuitFly : CosmeticInsect
	{
		public CosmeticInsect? zapTarget = null;
		public int zapCooldown;
		private Vector2 startPos;
		public Vector2 targetPos;
		private Vector2? targetDir;

		private int flyCooldown;
		private float lastFly = 0f;
		private float fly = 0f;

		public float lastZap = 0f;
		public float zap = 0f;

		private Color baseColor;
		private Color zapColor;

		private static Vector2 RestrictAlongGrid(Vector2 orig) => new((int)(orig.x / 10) * 10 + 5, (int)(orig.y / 10) * 10 + 5);

		public CircuitFly(Room room, Vector2 pos) : base(room, RestrictAlongGrid(pos), _Enums.CircuitFly)
		{
			zapCooldown = Random.Range(10, 80);
			flyCooldown = Random.Range(0, 10);
			targetPos = pos;

			float hue = Random.Range(0.3f, 0.4f);
			baseColor = HSL2RGB(hue, Random.Range(0.6f, 0.75f), Random.Range(0.2f, 0.3f));
			zapColor = HSL2RGB(hue, 1f, Random.Range(0.45f, 0.6f));
		}

		public override void Reset(Vector2 resetPos)
		{
			resetPos = RestrictAlongGrid(resetPos);
			base.Reset(resetPos);
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			lastZap = zap;
			lastFly = fly;
			lastPos = pos;

			// Update fly position
			if (flyCooldown > 0)
			{
				flyCooldown--;
				fly = 0f;
				lastFly = 0f;
			}
			else if (targetDir == null)
			{
				// Figure out potential tiles by checking all four directions
				List<Room.Tile> tiles = new() { };
				IntVector2 startTile = room.GetTilePosition(pos);
				const int CHECK_DIST = 10;

				for (int i = 0; i < CHECK_DIST; i++) // left
				{
					Room.Tile tile = room.GetTile(startTile.x - i, startTile.y);
					if (tile.Solid || tile.AnyWater)
					{
						break;
					}
					if (i >= 2)
					{
						tiles.Add(tile);
					}
				}
				for (int i = 0; i < CHECK_DIST; i++) // right
				{
					Room.Tile tile = room.GetTile(startTile.x + i, startTile.y);
					if (tile.Solid || tile.AnyWater)
					{
						break;
					}
					if (i >= 2)
					{
						tiles.Add(tile);
					}
				}
				for (int i = 0; i < CHECK_DIST; i++) // down
				{
					Room.Tile tile = room.GetTile(startTile.x - i, startTile.y);
					if (tile.Solid || tile.AnyWater)
					{
						break;
					}
					if (i >= 2)
					{
						tiles.Add(tile);
					}
				}
				for (int i = 0; i < CHECK_DIST; i++) // up
				{
					Room.Tile tile = room.GetTile(startTile.x + i, startTile.y);
					if (tile.Solid)
					{
						break;
					}
					if (i >= 2 && !tile.AnyWater)
					{
						tiles.Add(tile);
					}
				}

				// Pick a tile or destroy ourselves if there is somehow nowhere to go
				if (tiles.Count > 0)
				{
					Room.Tile pickedTile = tiles[Random.Range(0, tiles.Count)];
					Vector2 picked = room.MiddleOfTile(pickedTile.X, pickedTile.Y);
					Vector2 currPos = room.MiddleOfTile(pos);

					startPos = targetPos;
					targetDir = (currPos - picked).normalized;
					targetPos = currPos + targetDir.Value * Vector2.Distance(currPos, picked);
				}
				else
				{
					// Destroy();
				}
			}
			else
			{
				fly = Mathf.Min(1f, fly + 1f / 6f);

				if (fly == 1f)
				{
					flyCooldown = 10;
				}

				pos = Vector2.Lerp(startPos, targetPos, 0.5f - Mathf.Cos(fly * Mathf.PI));
			}

			// Update zap
			if (zapCooldown > 0)
			{
				zapCooldown--;
			}
			else if (zapTarget != null)
			{
				// Update zap
				zap = Custom.LerpAndTick(zap, 1f, 0.5f, 1f / 16f);

				// Special behavior
				if (zapTarget is CircuitFly cf)
				{
					cf.zap = zap;
				}

				// Cancel zap after some time
				if (zap == 1f)
				{
					zapTarget = null;
					zapCooldown = Random.Range(10, 80);
				}
			}
			else if (mySwarm.members.Count > 1 && Random.value < 1f / 60f)
			{
				// Pick a new zap target
				int i = 0;
				do
				{
					if (i++ > 100)
					{
						zapTarget = null;
						break;
					}
					zapTarget = mySwarm.members[Random.Range(0, mySwarm.members.Count)];
				}
				while (
					// Don't target ourself
					zapTarget == this ||
					// Try to target things farther away but not too far away
					Vector2.Distance(pos, zapTarget.pos) < Mathf.Pow(Random.value, 2) * 60f ||
					180f - Vector2.Distance(pos, zapTarget.pos) < Mathf.Pow(Random.value, 2) * 60f ||
					// Must have visual contact
					!room.VisualContact(pos, zapTarget.pos) ||

					// Don't target another circuit fly that is on zap cooldown, plus partial chance to ignore ones in same row/column as us
					(zapTarget is CircuitFly cf && (cf.zapCooldown > 0 || ((cf.pos.x == pos.x || cf.pos.y == pos.y) && Random.value < 0.4f))) ||
					// Don't target zippers that are zipping (if there are other types of bugs in the swarm)
					(zapTarget is Zipper z && z.zipFrom != null) ||
					// Prefer to target circuit flies or zippers
					(zapTarget is not CircuitFly && Random.value < (zapTarget is not Zipper ? 0.4f : 0.8f))
				);

				// Special behavior
				if (zapTarget is CircuitFly cf2)
				{
					cf2.zapCooldown = Random.Range(20, 90);
				}
				else if (zapTarget is Zipper z2)
				{
					z2.zipCooldown = Random.Range(10, 40);
				}

			}
			else
			{
				zap = 0f;
				lastZap = 0f;
				targetDir = null;
			}

			// Destroy if no longer needed
			if (!room.BeingViewed)
			{
				Destroy();
			}
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[]
			{
				new("pixel", true)
				{
					scaleX = 2f,
					anchorY = 0f
				},
				new("pixel", true)
				{
					scaleX = 1f,
					anchorY = 0f,
					alpha = 0f,
					color = zapColor
				}
			};
			AddToContainer(sLeaser, rCam, null);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			// Update position, rotation, scale
			sLeaser.sprites[0].x = Mathf.Lerp(lastPos.x, pos.x, timeStacker) - camPos.x;
			sLeaser.sprites[0].y = Mathf.Lerp(lastPos.y, pos.y, timeStacker) - camPos.y;
			sLeaser.sprites[0].rotation = targetDir?.GetAngle() ?? sLeaser.sprites[0].rotation;
			sLeaser.sprites[0].scaleY = Mathf.Max(2f, 2f + 1.1f * Vector2.Distance(lastPos, pos));

			if (zapTarget != null)
			{
				var from = Vector2.Lerp(lastPos, pos, timeStacker);
				var to = Vector2.Lerp(zapTarget.lastPos, zapTarget.pos, timeStacker);

				sLeaser.sprites[1].SetPosition((from + to) / 2f - camPos);
				sLeaser.sprites[1].rotation = Custom.AimFromOneVectorToAnother(from, to);
				sLeaser.sprites[1].scaleY = 2f + Vector2.Distance(from, to) / 2f;
			}

			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

			// Update glowy color
			sLeaser.sprites[0].color = Color.Lerp(baseColor, rCam.currentPalette.fogColor, rCam.currentPalette.fogAmount / 3f);
			sLeaser.sprites[0].color = Color.Lerp(baseColor, zapColor, Mathf.Sin(Mathf.Lerp(lastFly, fly, timeStacker) * Mathf.PI));
			if (zapTarget != null)
			{
				sLeaser.sprites[0].color = Color.Lerp(zapColor, sLeaser.sprites[0].color, Mathf.Lerp(lastZap, zap, timeStacker));
			}

			// sLeaser.sprites[1].color = new Color(zapTarget == null ? 0f : (1f - Mathf.Lerp(lastZap, zap, timeStacker)), 0.5f, RGB2HSL(zapColor).x); // if lightning bolt
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			sLeaser.sprites[0].color = palette.blackColor;
		}

	}

}
