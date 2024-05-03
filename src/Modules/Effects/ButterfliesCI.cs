using Random = UnityEngine.Random;
using DevInterface;

namespace RegionKit.Modules.Effects
{
	internal static class ButterfliesCI
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
			return (type == _Enums.ButterflyA || type == _Enums.ButterflyB) ? !room.GetTile(testPos).AnyWater : orig(type, room, testPos);
		}

		private static void PostRoomLoad(Room self)
		{
			for (int i = 0; i < self.roomSettings.effects.Count; i++)
			{
				if (self.roomSettings.effects[i].type == _Enums.ButterfliesA || self.roomSettings.effects[i].type == _Enums.ButterfliesB)
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
			if (type == _Enums.ButterfliesA)
				return _Enums.ButterflyA;
			else if (type == _Enums.ButterfliesB)
				return _Enums.ButterflyB;
			else
				return orig(type);
		}

		private static RoomSettingsPage.DevEffectsCategories RoomSettingsPage_DevEffectGetCategoryFromEffectType(On.DevInterface.RoomSettingsPage.orig_DevEffectGetCategoryFromEffectType orig, RoomSettingsPage self, RoomSettings.RoomEffect.Type type)
		{
			if (type == _Enums.ButterfliesA || type == _Enums.ButterfliesB)
			{
				return RoomSettingsPage.DevEffectsCategories.Insects;
			}
			return orig.Invoke(self, type);
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

			if (type == _Enums.ButterflyA || type == _Enums.ButterflyB)
			{
				CosmeticInsect insect = new Butterfly(self.room, pos, type == _Enums.ButterflyA);

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
			if (type == _Enums.ButterflyA || type == _Enums.ButterflyA)
			{
				return Mathf.Pow(Random.value, 1f - effectAmount) > (room.readyForAI ? room.aimap.getTerrainProximity(testPos) : 5) * 0.05f;
			}
			return orig(type, room, testPos, effectAmount);
		}
	}

	/// <summary>
	/// By Alduris
	/// Moth-like creature.
	/// </summary>
	public class Butterfly : CosmeticInsect
	{
		public Vector2? sitPos;
		public Vector2 rot;
		public Vector2 lastRot;

		public Vector2 flyDir;
		public Vector2 wallDir;

		public float wingsOut;
		public float lastWingsOut;
		public float flap;
		public float lastFlap;

		private float colorFac;
		private int sitStill;
		private int keepFlying;
		public bool A;

		public Butterfly(Room room, Vector2 pos, bool A) : base(room, pos, A ? _Enums.ButterflyA : _Enums.ButterflyB)
		{
			creatureAvoider = new CreatureAvoider(this, 30, 80f, 0.1f);
			wingsOut = 1f;
			for (int i = 0; i < 4; i++)
			{
				if (room.GetTile(pos + Custom.fourDirections[i].ToVector2() * 5f).Solid)
				{
					sitPos = new Vector2?(pos);
					wingsOut = 0f;
					wallDir = -Custom.fourDirections[i].ToVector2();
					break;
				}
			}
			lastWingsOut = wingsOut;
			if (burrowPos != null && sitPos != null)
			{
				burrowPos = new Vector2?(sitPos.Value);
			}
			rot = Custom.RNV();
			lastRot = rot;
			sitStill = Random.Range(20, 90);

			// Random palette color
			colorFac = Mathf.Pow(Random.value, 2);
			this.A = A;
		}

		public override void Update(bool eu)
		{
			lastRot = rot;
			lastWingsOut = wingsOut;
			lastFlap = flap;
			base.Update(eu);
			if (sitPos == null)
			{
				vel.y -= 0.8f;
			}
			if (submerged)
			{
				vel *= 0.8f;
				rot = Vector3.Slerp(rot, Custom.RNV(), 0.5f);
			}
		}

		public override void Reset(Vector2 resetPos)
		{
			base.Reset(resetPos);
			sitPos = null;
		}

		public override void Act()
		{
			base.Act();
			if (wantToBurrow && sitPos != null)
			{
				sitPos = null;
			}
			if (sitPos != null)
			{
				// Sitting
				pos = sitPos.Value;
				vel *= 0f;
				wingsOut = Mathf.Max(0f, wingsOut - 1f / Mathf.Lerp(20f, 40f, Random.value));
				if (flap == 1f)
				{
					flap = 0f;
				}
				else
				{
					flap = Mathf.Min(1f, flap + 1f / (5f - 2f * wingsOut));
				}

				// Take off to avoid creature
				if (creatureAvoider.currentWorstCrit != null && Custom.DistLess(creatureAvoider.currentWorstCrit.DangerPos, pos, 70f))
				{
					TakeOff();
					return;
				}

				// Random chance to take off after done sitting
				if (sitStill > 0)
				{
					sitStill--;
					return;
				}
				if (Random.value < 1 / 60f)
				{
					TakeOff();
					return;
				}
			}
			else
			{
				// Flying around
				vel.y += 0.4f;
				vel *= 0.82f;
				flyDir += Custom.RNV() * Random.value * 0.6f;

				if (wantToBurrow)
				{
					flyDir.y -= 0.5f;
				}
				else if (submerged)
				{
					flyDir.y++;
					vel.y += 0.3f;
				}
				else if (OutOfBounds)
				{
					flyDir += Custom.DirVec(pos, mySwarm.placedObject.pos) * Random.value * 0.075f;
				}
				else if (keepFlying <= 0 && room.GetTile(pos).wallbehind && Random.value < 1 / 80f)
				{
					// Sit on L2 walls rarely
					sitPos = pos;
					if (Random.value < 0.7f)
					{
						sitStill = Random.Range(20, 90);
					}
					vel *= 0f;
					wingsOut = Mathf.Max(0f, wingsOut - 1f / Mathf.Lerp(20f, 40f, Random.value));
					wallDir = flyDir;
					return;
				}
				else
				{
					// Avoid creature
					if (creatureAvoider.currentWorstCrit != null)
					{
						flyDir += Custom.DirVec(creatureAvoider.currentWorstCrit.DangerPos, pos) * creatureAvoider.FleeSpeed * Random.value * 3f;
					}
					else
					{
						flyDir += Custom.DirVec(pos, mySwarm.placedObject.pos) * Mathf.Pow(Random.value, 2f) * 0.1f;
					}

					if (pos.x < 0f)
					{
						flyDir.x += Random.value * 0.05f;
					}
					else if (pos.x > room.PixelWidth)
					{
						flyDir.x -= Random.value * 0.05f;
					}

					if (pos.y < 0f)
					{
						flyDir.y += Random.value * 0.05f;
					}
					else if (pos.y > room.PixelHeight)
					{
						flyDir.y -= Random.value * 0.05f;
					}
				}

				flyDir.Normalize();
				vel += flyDir * 0.5f + Custom.RNV() * 0.5f * Random.value;
				rot = Vector3.Slerp(rot, (-vel - flyDir + new Vector2(0f, -8f)).normalized, 0.2f);

				wingsOut = 1f;
				if (flap == 1f)
				{
					flap = 0f;
					vel.y += 2.4f;
					rot = Vector3.Slerp(rot, new Vector2(0f, -1f), 0.5f);
					return;
				}
				flap = Mathf.Min(1f, flap + 1f / 6f);

				if (keepFlying > 0)
				{
					keepFlying--;
				}
			}
		}

		public void TakeOff()
		{
			if (sitPos == null || burrowPos != null)
			{
				return;
			}
			flyDir = (-wallDir + Custom.RNV() * Random.value).normalized;
			pos = sitPos.Value + flyDir * 2f;
			vel = flyDir * 6f;
			sitPos = null;
			keepFlying = Random.Range(30, 110);
		}

		public override void WallCollision(IntVector2 dir, bool first)
		{
			if (wantToBurrow)
			{
				return;
			}
			if (sitPos == null && Random.value < 0.5f)
			{
				sitPos = pos;
				wallDir = Custom.RNV() * 0.1f;
				for (int i = 0; i < 8; i++)
				{
					if (room.GetTile(pos + Custom.eightDirections[i].ToVector2() * 20f).Solid)
					{
						wallDir += Custom.eightDirections[i].ToVector2().normalized;
					}
				}
				wallDir.Normalize();
				rot = (Custom.PerpendicularVector(wallDir) * ((Random.value < 0.5f) ? (-1f) : 1f) + Custom.RNV() * 0.2f).normalized;
				if (Random.value < 0.5f)
				{
					sitStill = Random.Range(20, 90);
					return;
				}
			}
			else
			{
				vel -= dir.ToVector2() * 1f * Random.value;
			}
		}

		public override void EmergeFromGround(Vector2 emergePos)
		{
			base.EmergeFromGround(emergePos);
			pos = emergePos;
			sitPos = new Vector2?(emergePos + new Vector2(0f, 4f));
			TakeOff();
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			base.InitiateSprites(sLeaser, rCam);
			sLeaser.sprites = new FSprite[7];
			sLeaser.sprites[0] = new FSprite("Circle20", true)
			{
				anchorY = 0.2f,
				scale = 1.5f
			};
			sLeaser.sprites[1] = new FSprite("pixel", true);
			sLeaser.sprites[2] = new FSprite("pixel", true);
			for (int i = 0; i < 4; i++)
			{
				sLeaser.sprites[3 + i] = new FSprite("Circle20", true);
			}
			AddToContainer(sLeaser, rCam, null);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			Vector2 posFac = Vector2.Lerp(lastPos, pos, timeStacker);
			float groundFac = Mathf.Lerp(lastInGround, inGround, timeStacker);
			Vector2 rotFac = Vector3.Slerp(lastRot, rot, timeStacker);
			float wingFac = Mathf.Pow(Mathf.Max(0.3149803f, Mathf.Lerp(lastWingsOut, wingsOut, timeStacker)), 0.6f);
			float bodyRot = Custom.VecToDeg(rotFac);
			float flapFac = Custom.SCurve(Mathf.Lerp(lastFlap, flap, timeStacker), 0.6f) * Mathf.Lerp(lastWingsOut, wingsOut, timeStacker) * 0.5f;
			posFac.y -= 5f * groundFac;

			// Adjust body sprite
			sLeaser.sprites[0].x = posFac.x - camPos.x;
			sLeaser.sprites[0].y = posFac.y - camPos.y;
			sLeaser.sprites[0].rotation = bodyRot;
			sLeaser.sprites[0].scaleX = (2.5f - wingFac * 0.5f) * (1f - groundFac) / 20f;
			sLeaser.sprites[0].scaleY = 5f * (1f - groundFac) / 20f;

			// Adjust antenae sprites
			for (int i = 0; i < 2; i++)
			{
				Vector2 antenaePos = posFac - rotFac * 3f + Custom.PerpendicularVector(rotFac) * ((i == 0) ? (-1f) : 1f) * 1.6f;

				sLeaser.sprites[1 + i].x = antenaePos.x - camPos.x;
				sLeaser.sprites[1 + i].y = antenaePos.y - camPos.y;
			}

			// Adjust wing sprites
			for (int i = 0; i < 4; i++)
			{
				bool side = i < 2;
				float adjWingFac = Mathf.Lerp(0.5f, 1f, wingFac); // just gives a larger number
				float baseAngle = Mathf.Lerp(45f, side ? 60f : 0f, adjWingFac); // lerp between rest position and flight

				sLeaser.sprites[3 + i].x = posFac.x - camPos.x;
				sLeaser.sprites[3 + i].y = posFac.y - camPos.y;
				sLeaser.sprites[3 + i].scaleY = 0.3f * (1f - groundFac);
				sLeaser.sprites[3 + i].scaleX = 0.18f * adjWingFac;
				sLeaser.sprites[3 + i].rotation = (baseAngle + flapFac * 600f * Mathf.Pow(wingFac, 4f)) * wingFac * ((i == 0) ? (-1f) : 1f) + bodyRot;
				sLeaser.sprites[3 + i].anchorY = -0.1f;
			}
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			base.ApplyPalette(sLeaser, rCam, palette);

			// 3 colors being lerped here: a random light color from light palette, an effect color of the dev's choice, and the fog color
			Color palCol = palette.texture.GetPixel((int)Mathf.Lerp(14f, 20f, colorFac), 2);
			Color effCol = palette.texture.GetPixel(30, A ? 4 : 2);
			Color bodyColor = palette.blackColor;
			Color wingColor = Color.Lerp(Color.Lerp(palCol, effCol, 0.925f), palette.fogColor, 0.15f * palette.fogAmount + 0.25f * palette.darkness);
			for (int i = 0; i < 3; i++)
			{
				sLeaser.sprites[i].color = bodyColor;
			}
			for (int i = 3; i < sLeaser.sprites.Length; i++)
			{
				sLeaser.sprites[i].color = Color.Lerp(wingColor, palette.blackColor, i < 5 ? 0f : 0.1f); // bottom half ever so slightly darker
			}
		}
	}

}
