using System;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;
using static RWCustom.Custom;

/// <summary>
/// By LB Gamer/M4rbleL1ne
/// </summary>

namespace RegionKit.Modules.Effects;

public class GlowingSwimmersCI
{
	public static class EnumExt_GlowingSwimmers
	{
		public static RoomSettings.RoomEffect.Type GlowingSwimmers;
		public static CosmeticInsect.Type GlowingSwimmerInsect;
	}

	public static void Apply()
	{
		On.Room.Loaded += (orig, self) =>
			{
				orig(self);
				for (var i = 0; i < self.roomSettings.effects.Count; i++)
				{
					var effect = self.roomSettings.effects[i];
					if (effect.type == EnumExt_GlowingSwimmers.GlowingSwimmers)
					{
						if (self.insectCoordinator is null)
						{
							self.insectCoordinator = new(self);
							self.AddObject(self.insectCoordinator);
						}
						self.insectCoordinator.AddEffect(effect);
					}
				}
			};
		On.InsectCoordinator.SpeciesDensity_Type_1 += (orig, type) => type == EnumExt_GlowingSwimmers.GlowingSwimmerInsect ? .8f : orig(type);
		On.InsectCoordinator.RoomEffectToInsectType += (orig, type) => type == EnumExt_GlowingSwimmers.GlowingSwimmers ? EnumExt_GlowingSwimmers.GlowingSwimmerInsect : orig(type);
		On.InsectCoordinator.TileLegalForInsect += (orig, type, room, testPos) => type == EnumExt_GlowingSwimmers.GlowingSwimmerInsect ? room.GetTile(testPos).DeepWater : orig(type, room, testPos);
		On.InsectCoordinator.EffectSpawnChanceForInsect += (orig, type, room, testPos, effectAmount) => type == EnumExt_GlowingSwimmers.GlowingSwimmerInsect || orig(type, room, testPos, effectAmount);
		On.InsectCoordinator.CreateInsect += (orig, self, type, pos, swarm) =>
			{
				if (!InsectCoordinator.TileLegalForInsect(type, self.room, pos) || self.room.world.rainCycle.TimeUntilRain < Random.Range(1200, 1600))
					return;
				if (type == EnumExt_GlowingSwimmers.GlowingSwimmerInsect)
				{
					var cosmeticInsect = new GlowingSwimmer(self.room, pos);
					self.allInsects.Add(cosmeticInsect);
					if (swarm != null)
					{
						swarm.members.Add(cosmeticInsect);
						cosmeticInsect.mySwarm = swarm;
					}
					self.room.AddObject(cosmeticInsect);
				}
				else
					orig(self, type, pos, swarm);
			};
	}
}

public class GlowingSwimmer : CosmeticInsect
{
	public float stressed;
	public Vector2 rot;
	public Vector2 lastRot;
	public Vector2 swimDir;
	public float breath;
	public float lastBreath;
	public Vector2[,] segments;
	public LightSource lightSource;

	public GlowingSwimmer(Room room, Vector2 pos) : base(room, pos, GlowingSwimmersCI.EnumExt_GlowingSwimmers.GlowingSwimmerInsect)
	{
		creatureAvoider = new(this, 10, 300f, .3f);
		breath = Random.value;
		segments = new Vector2[2, 2];
		Reset(pos);
	}

	public override void Update(bool eu)
	{
		lastRot = rot;
		lastBreath = breath;
		base.Update(eu);
		if (submerged)
			vel *= .8f / 1.2f;
		else
			vel.y -= .9f / 1.2f;
		for (var i = 0; i < segments.GetLength(0); i++)
		{
			segments[i, 0] += segments[i, 1];
			if (room.PointSubmerged(segments[i, 0]))
				segments[i, 1] *= .8f;
			else
				segments[i, 1].y -= .9f;
			if (i is 0)
			{
				var vector = DirVec(segments[i, 0], pos);
				var num = Vector2.Distance(segments[i, 0], pos);
				pos += vector * (4f - num) * .5f;
				vel += vector * (4f - num) * .5f;
				segments[i, 0] -= vector * (4f - num) * .5f;
				segments[i, 1] -= vector * (4f - num) * .5f;
				vel += vector * Mathf.Lerp(.8f, 1.2f, stressed) / 1.2f;
			}
			else
			{
				var vector2 = DirVec(segments[i, 0], segments[i - 1, 0]);
				var num2 = Vector2.Distance(segments[i, 0], segments[i - 1, 0]);
				segments[i - 1, 0] += vector2 * (4f - num2) * .5f;
				segments[i - 1, 1] += vector2 * (4f - num2) * .5f;
				segments[i, 0] -= vector2 * (4f - num2) * .5f;
				segments[i, 1] -= vector2 * (4f - num2) * .5f;
				segments[i - 1, 1] += vector2 * Mathf.Lerp(.8f, 1.2f, stressed);
			}
		}
		if (room != null)
		{
			if (lightSource != null)
			{
				lightSource.stayAlive = true;
				lightSource.setPos = pos;
				lightSource.color = room.game.cameras[0].currentPalette.waterColor1;
				lightSource.affectedByPaletteDarkness = 0f;
				if (lightSource.slatedForDeletetion || !submerged)
					lightSource = null;
			}
			else if (submerged)
			{
				lightSource = new(pos, false, room.game.cameras[0].currentPalette.waterColor1, this)
				{
					requireUpKeep = true,
					setRad = 200f,
					setAlpha = 1f,
					affectedByPaletteDarkness = 0f
				};
				room.AddObject(lightSource);
			}
		}
	}

	public override void Reset(Vector2 resetPos)
	{
		base.Reset(resetPos);
		for (var i = 0; i < segments.GetLength(0); i++)
		{
			segments[i, 0] = resetPos + RNV();
			segments[i, 1] = RNV() * Random.value;
		}
	}

	public override void Act()
	{
		base.Act();
		breath -= 1f / Mathf.Lerp(60f, 10f, stressed);
		var num = Mathf.Pow(creatureAvoider.FleeSpeed, .3f);
		if (num > stressed)
			stressed = LerpAndTick(stressed, num, .05f, 1f / 60f);
		else
			stressed = LerpAndTick(stressed, num, .02f, .005f);
		if (submerged)
		{
			swimDir += RNV() * Random.value * .5f;
			if (wantToBurrow)
				swimDir.y -= .5f;
			if (pos.x < 0f)
				swimDir.x += 1f;
			else if (pos.x > room.PixelWidth)
				swimDir.x -= 1f;
			if (pos.y < 0f)
				swimDir.y += 1f;
			if (creatureAvoider.currentWorstCrit != null)
				swimDir -= DirVec(pos, creatureAvoider.currentWorstCrit.DangerPos) * creatureAvoider.FleeSpeed;
			if (room.water)
				swimDir = Vector3.Slerp(swimDir, new(0f, -1f), Mathf.InverseLerp(room.FloatWaterLevel(pos.x) - 100f, room.FloatWaterLevel(pos.x), pos.y) * .5f);
			swimDir.Normalize();
			vel += (swimDir * Mathf.Lerp(.8f, 1.1f, stressed) + RNV() * Random.value * .1f) / 1.2f;
		}
		rot = Vector3.Slerp(rot, (-vel - swimDir).normalized, .2f);
	}

	public override void WallCollision(IntVector2 dir, bool first)
	{
		swimDir -= RNV() * Random.value + dir.ToVector2();
		swimDir.Normalize();
	}

	public override void EmergeFromGround(Vector2 emergePos)
	{
		base.EmergeFromGround(emergePos);
		pos = emergePos;
		swimDir = new(0f, 1f);
	}

	public virtual int LegSprite(int segment, int leg) => segment * 2 + leg;

	public virtual int SegmentSprite(int segment, int part) => (segments.GetLength(0) + 1) * 2 + segment * 2 + part;

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		base.InitiateSprites(sLeaser, rCam);
		sLeaser.sprites = new FSprite[(segments.GetLength(0) + 1) * 4];
		for (var i = 0; i < segments.GetLength(0) + 1; i++)
		{
			sLeaser.sprites[SegmentSprite(i, 0)] = new("Circle20") { anchorY = .3f };
			sLeaser.sprites[SegmentSprite(i, 1)] = new("Circle20") { anchorY = .4f };
			sLeaser.sprites[LegSprite(i, 0)] = new("pixel") { anchorY = 0f };
			sLeaser.sprites[LegSprite(i, 1)] = new("pixel") { anchorY = 0f };
		}
		AddToContainer(sLeaser, rCam, null);
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		var num = Mathf.Lerp(lastInGround, inGround, timeStacker);
		for (var i = 0; i < segments.GetLength(0) + 1; i++)
		{
			var t = .5f + .5f * Mathf.Sin((Mathf.Lerp(lastBreath, breath, timeStacker) + .1f * i) * 7f * (float)Math.PI);
			var p = (i != 0) ? segments[i - 1, 0] : Vector2.Lerp(lastPos, pos, timeStacker);
			var v = i switch
			{
				0 => (Vector2)(-Vector3.Slerp(lastRot, rot, timeStacker)),
				1 => DirVec(p, Vector2.Lerp(lastPos, pos, timeStacker)),
				_ => DirVec(p, segments[i - 2, 0]),
			};
			p.y -= 5f * num;
			var num3 = LerpMap(i, 0f, segments.GetLength(0), 1f, .5f, 1.2f) * (1f - num);
			for (var j = 0; j < 2; j++)
			{
				var seg = sLeaser.sprites[SegmentSprite(i, j)];
				seg.x = p.x - camPos.x;
				seg.y = p.y - camPos.y;
				seg.rotation = VecToDeg(v);
				seg.scaleX = 4f * num3 * (1f - num) / 15f;
				seg.scaleY = 6.5f * num3 * (1f - num) / 15f;
				var leg = sLeaser.sprites[LegSprite(i, j)];
				leg.x = p.x - PerpendicularVector(v).x * 2f * num3 * ((j != 0) ? 1f : -1f) - camPos.x;
				leg.y = p.y - PerpendicularVector(v).y * 2f * num3 * ((j != 0) ? 1f : -1f) - camPos.y;
				leg.rotation = VecToDeg(v) + (Mathf.Lerp(-20f, 70f, t) + LerpMap(i, 0f, segments.GetLength(0), 70f, 140f)) * ((j != 0) ? 1f : -1f);
				leg.scaleY = Mathf.Lerp(3.5f + (i * 2), 3f, Mathf.Sin(Mathf.InverseLerp(0f, segments.GetLength(0), i) * Mathf.PI)) * num3;
			}
		}
	}

	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		base.ApplyPalette(sLeaser, rCam, palette);
		for (var i = 0; i < segments.GetLength(0) + 1; i++)
		{
			sLeaser.sprites[SegmentSprite(i, 0)].color = palette.blackColor;
			sLeaser.sprites[SegmentSprite(i, 1)].color = palette.blackColor;
		}
	}
}
