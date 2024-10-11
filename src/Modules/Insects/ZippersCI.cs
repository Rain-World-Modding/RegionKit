using Random = UnityEngine.Random;

namespace RegionKit.Modules.Insects
{
	internal static class ZippersCI
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
			return (type == _Enums.Zipper) ? !room.GetTile(testPos).AnyWater : orig(type, room, testPos);
		}

		private static void PostRoomLoad(Room self)
		{
			for (int i = 0; i < self.roomSettings.effects.Count; i++)
			{
				if (self.roomSettings.effects[i].type == _Enums.Zippers)
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
			return type == _Enums.Zippers ? _Enums.Zipper : orig(type);
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

			if (type == _Enums.Zipper)
			{
				CosmeticInsect insect = new Zipper(self.room, pos);

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
			if (type == _Enums.Zipper)
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
	public class Zipper : CosmeticInsect
	{
		public Vector2 lastLastPos;
		public Vector2 dir;
		public Vector2? zipFrom;
		public Vector2? zipTo;

		public float zip;
		public float lastZip;
		public float glow;
		public float lastGlow;

		public int zipCooldown;
		public Color zipColor;
		public Color fadeColor;
		private LightSource light;

		public Zipper(Room room, Vector2 pos) : base(room, pos, _Enums.Zipper)
		{
			lastLastPos = pos;
			creatureAvoider = new CreatureAvoider(this, 20, 80f, 0.1f);

			// Create random color
			// Somewhat based on flare bomb purple color (hsl(0.7, 1, 0.5) for reference but here has slightly random color range)
			var hsl = new HSLColor(Random.Range(0.6f, 0.8f), 1f, 0.5f);
			fadeColor = hsl.rgb;
			hsl.lightness = 0.9f;
			zipColor = hsl.rgb;
		}

		public override void Reset(Vector2 resetPos)
		{
			base.Reset(resetPos);
			zipTo = zipFrom = null;
			lastLastPos = resetPos;
			dir = Custom.RNV();
			zipCooldown = Random.Range(20, 40);
		}

		public override void EmergeFromGround(Vector2 emergePos)
		{
			base.EmergeFromGround(emergePos);
			dir = new Vector2(0f, 1f);
		}

		public override void Update(bool eu)
		{
			// Custom zipping stuff
			lastZip = zip;
			lastGlow = glow;

			if (zipTo != null)
			{
				zip = Mathf.Min(1f, zip + 0.25f);
				glow = 1f;

				pos = Vector2.Lerp(zipFrom!.Value, zipTo.Value, zip);

				if (zip == 1f)
				{
					zipCooldown = Random.Range(5, 40);
					pos = zipTo.Value;
					lastPos = pos;
					lastLastPos = pos;
					zipTo = null;
					zipFrom = null;
					dir = Custom.RNV();
				}
			}
			else
			{
				// Stuff from MiniFly
				vel *= 0.85f;
				vel += dir * 2.4f;
				dir = Vector2.Lerp(dir, Custom.DegToVec(Random.value * 360f) * Mathf.Pow(Random.value, 0.75f), Mathf.Pow(Random.value, 1.5f));
				if (wantToBurrow)
				{
					dir = Vector2.Lerp(dir, new Vector2(0f, -1f), 0.1f);
				}
				else if (OutOfBounds)
				{
					dir = Vector2.Lerp(dir, Custom.DirVec(pos, mySwarm.placedObject.pos), Mathf.InverseLerp(mySwarm.insectGroupData.Rad, mySwarm.insectGroupData.Rad + 100f, Vector2.Distance(pos, mySwarm.placedObject.pos)));
				}
				else
				{
					float num = TileScore(room.GetTilePosition(pos));
					IntVector2 intVector = new IntVector2(0, 0);
					for (int i = 0; i < 4; i++)
					{
						if (!room.GetTile(room.GetTilePosition(pos) + Custom.fourDirections[i]).Solid && TileScore(room.GetTilePosition(pos) + Custom.fourDirections[i] * 3) > num)
						{
							num = TileScore(room.GetTilePosition(pos) + Custom.fourDirections[i] * 3);
							intVector = Custom.fourDirections[i];
						}
					}
					vel += intVector.ToVector2() * 0.4f;
				}
				lastLastPos = lastPos;

				if (submerged)
				{
					vel *= 0.8f;
					dir = Custom.RNV();
				}

				// Glow and decide if want to zip
				zip = 0f;
				glow = Mathf.Lerp(glow, 0f, 0.4f); // slowly fade out glow

				if (zipCooldown == 0 && Random.value < (submerged ? 1/20f : 1/80f)) // being submerged gives bigger chance so it can escape
				{
					// Pick something nice and good, give it 10 tries
					Vector2 pickedPos = default;
					float pickedScore = float.MinValue;
					for (int i = 0; i < 10; i++)
					{
						bool big = Random.value < 0.05f;
						Vector2 testPos = pos + Custom.RNV() * Mathf.Lerp(80f, 150f, 4f * Mathf.Pow(Random.value - 0.5f, 3) * (big ? 1.8f : 1f) + 0.5f);
						if (!room.GetTile(testPos).Solid && room.VisualContact(pos, testPos) && TileScore(room.GetTilePosition(testPos)) > pickedScore)
						{
							pickedPos = testPos;
							pickedScore = TileScore(room.GetTilePosition(testPos));
							glow = 1f;
						}
					}

					if (pickedPos != default)
					{
						zipTo = pickedPos;
						zipFrom = pos;
					}
				}
				else if (zipCooldown > 0)
				{
					zipCooldown--;
				}
			}

			// FireFly stuff
			if (room.Darkness(pos) > 0f)
			{
				if (light == null)
				{
					light = new LightSource(pos, false, Color.Lerp(fadeColor, zipColor, 0.5f), this)
					{
						noGameplayImpact = true
					};
					room.AddObject(light);
				}
				light.setPos = new Vector2?(pos);
				light.setAlpha = new float?(0.75f - 0.4f * (1f - glow));
				light.setRad = new float?(40f + 15f * (1f - glow));
			}
			else if (light != null)
			{
				light.Destroy();
				light = null;
			}

			base.Update(eu);

			// Destroy if no longer needed
			if (!room.BeingViewed)
			{
				Destroy();
			}
		}

		private float TileScore(IntVector2 tile)
		{
			if (room.readyForAI && room.IsPositionInsideBoundries(tile))
			{
				float thing = Random.value / Mathf.Max(1, room.aimap.getTerrainProximity(tile) - 5) / DangerFac();
				return thing * (room.GetTile(tile).Solid ? 0.4f : 1f);
			}
			return 0f;
		}

		private float DangerFac()
		{
			if (creatureAvoider == null) return 0f;

			Creature crit = creatureAvoider.currentWorstCrit;
			if (crit != null)
			{
				float distFac = Mathf.Pow(1f - Mathf.Min(0.95f, Vector2.Distance(pos, crit.DangerPos) / 80f), 1.2f);
				return Custom.LerpMap(crit.TotalMass * distFac, 0f, 20f, 3f, 0.5f);
			}
			return 0f;
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[1];
			sLeaser.sprites[0] = new FSprite("pixel", true)
			{
				scaleX = 2f,
				anchorY = 0f
			};
			AddToContainer(sLeaser, rCam, null);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			// Update position, rotation, scale
			if (zipTo == null)
			{
				sLeaser.sprites[0].x = Mathf.Lerp(lastPos.x, pos.x, timeStacker) - camPos.x;
				sLeaser.sprites[0].y = Mathf.Lerp(lastPos.y, pos.y, timeStacker) - camPos.y;
				sLeaser.sprites[0].rotation = Custom.AimFromOneVectorToAnother(Vector2.Lerp(lastLastPos, lastPos, timeStacker), Vector2.Lerp(lastPos, pos, timeStacker));
				sLeaser.sprites[0].scaleY = Mathf.Max(2f, 2f + 1.1f * Vector2.Distance(Vector2.Lerp(lastLastPos, lastPos, timeStacker), Vector2.Lerp(lastPos, pos, timeStacker)));
			}
			else
			{
				float zipFac = Mathf.Lerp(lastZip, zip, timeStacker);
				var from = Vector2.Lerp(zipFrom!.Value, zipTo.Value, zipFac * 2f - 1f);
				var to = Vector2.Lerp(zipFrom!.Value, zipTo.Value, zipFac * 2f);

				sLeaser.sprites[0].SetPosition((from + to) / 2f - camPos);
				sLeaser.sprites[0].rotation = Custom.AimFromOneVectorToAnother(zipFrom!.Value, zipTo.Value);
				sLeaser.sprites[0].scaleY = 2f + Vector2.Distance(from, to) / 2f;
			}

			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

			// Update glowy color
			float glowFac = Mathf.Lerp(lastGlow, glow, timeStacker);
			if (glowFac > 0.5f)
			{
				sLeaser.sprites[0].color = Color.Lerp(fadeColor, zipColor, glowFac * 2f - 1f);
			}
			else
			{
				sLeaser.sprites[0].color = Color.Lerp(rCam.currentPalette.blackColor, fadeColor, Mathf.Max(glowFac * 2f, Mathf.Sqrt(room.Darkness(pos)) * 2f / 3f));
			}
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			sLeaser.sprites[0].color = palette.blackColor;
		}

	}

}
