using Random = UnityEngine.Random;

namespace RegionKit.Modules.Insects
{
	/// <summary>
	/// Variant of WaterGlowworm with modified logic
	/// </summary>
	public class RippleGlowworm : CosmeticInsect
	{
		public float stressed;
		public Vector2 rot;
		public Vector2 lastRot;
		private Vector2 floatDir;
		private float breath;
		private float lastBreath;
		public SimpleSegment[] segments;

		public RippleGlowworm(Room room, Vector2 pos) : base(room, pos, _Enums.RippleGlowworm)
		{
			creatureAvoider = new CreatureAvoider(this, 10, 300f, 0.3f);
			breath = Random.value;
			segments = new SimpleSegment[Random.Range(2, 4)];
			Reset(pos);
		}

		public override void Update(bool eu)
		{
			if (room == null || room.PointDeferred(pos))
			{
				return;
			}

			lastRot = rot;
			lastBreath = breath;
			base.Update(eu);
			vel *= 0.8f;
			for (int i = 0; i < segments.GetLength(0); i++)
			{
				segments[i].lastPos = segments[i].pos;
				segments[i].pos += segments[i].vel;
				segments[i].vel *= 0.8f;
				if (i == 0)
				{
					Vector2 dir = DirVec(segments[i].pos, pos);
					float dist = Vector2.Distance(segments[i].pos, pos);
					Vector2 change = dir * ((4f - dist) * 0.5f);
					pos += change;
					vel += change;
					segments[i].pos -= change;
					segments[i].vel -= change;
					vel += dir * Mathf.Lerp(0.8f, 1.2f, stressed);
				}
				else
				{
					Vector2 dir = DirVec(segments[i].pos, segments[i - 1].pos);
					float dist = Vector2.Distance(segments[i].pos, segments[i - 1].pos);
					Vector2 change = dir * ((4f - dist) * 0.5f);
					segments[i - 1].pos += change;
					segments[i - 1].vel += change;
					segments[i].pos -= change;
					segments[i].vel -= change;
					segments[i - 1].vel += dir * Mathf.Lerp(0.8f, 1.2f, stressed);
				}
			}
		}

		public override void Reset(Vector2 resetPos)
		{
			base.Reset(resetPos);
			for (int i = 0; i < segments.GetLength(0); i++)
			{
				segments[i].Reset(resetPos + RNV());
				segments[i].vel = RNV() * Random.value;
			}
		}

		public override void Act()
		{
			base.Act();
			if (room == null || !room.readyForAI) return;

			breath -= 1f / Mathf.Lerp(60f, 10f, stressed);
			float stressGetTo = Mathf.Pow(creatureAvoider.FleeSpeed, 0.3f);
			if (stressGetTo > stressed)
			{
				stressed = LerpAndTick(stressed, stressGetTo, 0.05f, 0.016666668f);
			}
			else
			{
				stressed = LerpAndTick(stressed, stressGetTo, 0.02f, 0.005f);
			}
			floatDir += RNV() * (Random.value * 0.5f);
			if (wantToBurrow)
			{
				floatDir.y -= 0.5f;
			}
			if (pos.x < 0f)
			{
				floatDir.x++;
			}
			else if (pos.x > room.PixelWidth)
			{
				floatDir.x--;
			}
			if (pos.y < 0f)
			{
				floatDir.y++;
			}
			if (creatureAvoider.currentWorstCrit != null)
			{
				floatDir -= DirVec(pos, creatureAvoider.currentWorstCrit.DangerPos) * creatureAvoider.FleeSpeed;
			}

			// Keep distance from ground
			float floorAltitude = room.aimap.getAItile(pos).smoothedFloorAltitude + ((pos.y % 20f) - 10f) / 20f;

			floatDir = Vector3.Slerp(floatDir, new Vector2(0f, -1f), Mathf.InverseLerp(6f, 10f, floorAltitude) * 0.5f);
			floatDir = Vector3.Slerp(floatDir, new Vector2(0f, 1f), Mathf.Pow(Mathf.InverseLerp(3f, 0f, floorAltitude), 2f) * 0.5f);

			// Avoid water
			if (room.water)
			{
				floatDir = Vector3.Slerp(floatDir, new Vector2(0f, 1f), Mathf.InverseLerp(room.FloatWaterLevel(pos) + 60f, room.FloatWaterLevel(pos), pos.y) * 0.5f);
			}

			floatDir.Normalize();
			vel += floatDir * Mathf.Lerp(0.8f, 1.1f, stressed) * 0.6f + RNV() * (Random.value * 0.06f);
			rot = Vector3.Slerp(rot, (-vel - floatDir).normalized, 0.2f);
		}

		public override void WallCollision(IntVector2 dir, bool first)
		{
			floatDir -= RNV() * Random.value + dir.ToVector2();
			floatDir.Normalize();
		}

		public override void EmergeFromGround(Vector2 emergePos)
		{
			base.EmergeFromGround(emergePos);
			pos = emergePos;
			floatDir = new Vector2(0f, 1f);
		}

		private int LegSprite(int segment, int leg)
		{
			return segment * 2 + leg;
		}

		private int SegmentSprite(int segment, int part)
		{
			return (segments.GetLength(0) + 1) * 2 + segment * 2 + part;
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			base.InitiateSprites(sLeaser, rCam);
			sLeaser.sprites = new FSprite[(segments.GetLength(0) + 1) * 4];
			for (int i = 0; i < segments.GetLength(0) + 1; i++)
			{
				sLeaser.sprites[SegmentSprite(i, 0)] = new FSprite("Circle20", true)
				{
					anchorY = 0.3f,
					shader = rCam.game.rainWorld.Shaders["RippleBasicRippleSideAlt"]
				};
				sLeaser.sprites[SegmentSprite(i, 1)] = new FSprite("Circle20", true)
				{
					anchorY = 0.4f,
					shader = rCam.game.rainWorld.Shaders["RippleBasicRippleSideAlt"]
				};
				sLeaser.sprites[LegSprite(i, 0)] = new FSprite("pixel", true)
				{
					anchorY = 0f,
					shader = rCam.game.rainWorld.Shaders["RippleBasicRippleSideAlt"]
				};
				sLeaser.sprites[LegSprite(i, 1)] = new FSprite("pixel", true)
				{
					anchorY = 0f,
					shader = rCam.game.rainWorld.Shaders["RippleBasicRippleSideAlt"]
				};
			}
			AddToContainer(sLeaser, rCam, null);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (room == null || room.PointDeferred(pos))
			{
				return;
			}
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

			float useInGround = Mathf.Lerp(lastInGround, inGround, timeStacker);
			for (int i = 0; i < segments.GetLength(0) + 1; i++)
			{
				float useBreath = Mathf.Sin((Mathf.Lerp(lastBreath, breath, timeStacker) + 0.04f * i) * 2f * 3.1415927f);
				float useSwim = 0.5f + 0.5f * Mathf.Sin((Mathf.Lerp(lastBreath, breath, timeStacker) + 0.1f * i) * 7f * 3.1415927f);
				Vector2 drawPos = ((i == 0) ? Vector2.Lerp(lastPos, pos, timeStacker) : Vector2.Lerp(segments[i - 1].lastPos, segments[i - 1].pos, timeStacker));
				Vector2 dr = i switch
				{
					0 => -Vector3.Slerp(lastRot, rot, timeStacker),
					1 => DirVec(drawPos, Vector2.Lerp(lastPos, pos, timeStacker)),
					_ => DirVec(drawPos, Vector2.Lerp(segments[i - 2].lastPos, segments[i - 2].pos, timeStacker))
				};
				drawPos.y -= 5f * useInGround;
				float scale = LerpMap(i, 0f, segments.GetLength(0), 1f, 0.5f, 1.2f) * (1f - useInGround);
				for (int j = 0; j < 2; j++)
				{
					sLeaser.sprites[SegmentSprite(i, j)].x = drawPos.x - camPos.x;
					sLeaser.sprites[SegmentSprite(i, j)].y = drawPos.y - camPos.y;
					sLeaser.sprites[SegmentSprite(i, j)].rotation = VecToDeg(dr);
					sLeaser.sprites[LegSprite(i, j)].x = drawPos.x - PerpendicularVector(dr).x * 2f * scale * ((j == 0) ? (-1f) : 1f) - camPos.x;
					sLeaser.sprites[LegSprite(i, j)].y = drawPos.y - PerpendicularVector(dr).y * 2f * scale * ((j == 0) ? (-1f) : 1f) - camPos.y;
					sLeaser.sprites[LegSprite(i, j)].rotation = VecToDeg(dr) + (Mathf.Lerp(-20f, 70f, useSwim) + LerpMap(i, 0f, segments.GetLength(0), 70f, 140f)) * ((j == 0) ? (-1f) : 1f);
					sLeaser.sprites[LegSprite(i, j)].scaleY = Mathf.Lerp(3.5f + i * 2, 3f, Mathf.Sin(Mathf.InverseLerp(0f, segments.GetLength(0), i) * 3.1415927f)) * scale;
				}
				sLeaser.sprites[SegmentSprite(i, 0)].scaleX = 4f * scale * (1f - useInGround) / 20f;
				sLeaser.sprites[SegmentSprite(i, 0)].scaleY = 6.5f * scale * (1f - useInGround) / 20f;
				sLeaser.sprites[SegmentSprite(i, 1)].scaleX = 3f * scale * (1f - useInGround) * useBreath / 20f;
				sLeaser.sprites[SegmentSprite(i, 1)].scaleY = 5.5f * scale * (1f - useInGround) * useBreath / 20f;
			}

			foreach (FSprite sprite in sLeaser.sprites)
			{
				sprite.isVisible = rCam.rippleData != null && rCam.rippleData.isPassAdded;
			}
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			base.ApplyPalette(sLeaser, rCam, palette);
			for (int i = 0; i < segments.GetLength(0) + 1; i++)
			{
				sLeaser.sprites[SegmentSprite(i, 0)].color = palette.blackColor;
				sLeaser.sprites[SegmentSprite(i, 1)].color = Color.Lerp(palette.blackColor, RainWorld.RippleGold, 0.5f);
				sLeaser.sprites[LegSprite(i, 0)].color = palette.blackColor;
				sLeaser.sprites[LegSprite(i, 1)].color = palette.blackColor;
			}
		}
	}
}
