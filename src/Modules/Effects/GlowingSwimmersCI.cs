using System;
using RWCustom;
using UnityEngine;
using static RWCustom.Custom;
using Random = UnityEngine.Random;

/// <summary>
/// By LB Gamer/M4rbleL1ne
/// </summary>

namespace RegionKit.Modules.Effects;

public class GlowingSwimmersCI
{

	public static void Apply()
	{
		_CommonHooks.PostRoomLoad += RoomPostLoad;
		On.InsectCoordinator.SpeciesDensity_Type_1 += (orig, type) => type == _Enums.GlowingSwimmerInsect ? .8f : orig(type);
		On.InsectCoordinator.RoomEffectToInsectType += (orig, type) => type == _Enums.GlowingSwimmers ? _Enums.GlowingSwimmerInsect : orig(type);
		On.InsectCoordinator.TileLegalForInsect += (orig, type, room, testPos) => type == _Enums.GlowingSwimmerInsect ? room.GetTile(testPos).DeepWater : orig(type, room, testPos);
		On.InsectCoordinator.EffectSpawnChanceForInsect += (orig, type, room, testPos, effectAmount) => type == _Enums.GlowingSwimmerInsect || orig(type, room, testPos, effectAmount);
		On.InsectCoordinator.CreateInsect += (orig, self, type, pos, swarm) =>
			{
				if (!InsectCoordinator.TileLegalForInsect(type, self.room, pos) || self.room.world.rainCycle.TimeUntilRain < Random.Range(1200, 1600))
					return;
				if (type == _Enums.GlowingSwimmerInsect)
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

	private static void RoomPostLoad(Room self)
	{
		for (var i = 0; i < self.roomSettings.effects.Count; i++)
		{
			var effect = self.roomSettings.effects[i];
			if (effect.type == _Enums.GlowingSwimmers)
			{
				if (self.insectCoordinator is null)
				{
					self.insectCoordinator = new(self);
					self.AddObject(self.insectCoordinator);
				}
				self.insectCoordinator.AddEffect(effect);
			}
		}
	}
}

public class GlowingSwimmer : CosmeticInsect
{
	private float _stressed;
	private Vector2 _rot;
	private Vector2 _lastRot;
	private Vector2 _swimDir;
	private float _breath;
	private float _lastBreath;
	private Vector2[,] _segments;
	private LightSource? _lightSource;

	public GlowingSwimmer(Room room, Vector2 pos) : base(room, pos, _Enums.GlowingSwimmerInsect)
	{
		creatureAvoider = new(this, 10, 300f, .3f);
		_breath = Random.value;
		_segments = new Vector2[2, 2];
		Reset(pos);
	}

	public override void Update(bool eu)
	{
		_lastRot = _rot;
		_lastBreath = _breath;
		base.Update(eu);
		if (submerged)
			vel *= .8f / 1.2f;
		else
			vel.y -= .9f / 1.2f;
		for (var i = 0; i < _segments.GetLength(0); i++)
		{
			_segments[i, 0] += _segments[i, 1];
			if (room.PointSubmerged(_segments[i, 0]))
				_segments[i, 1] *= .8f;
			else
				_segments[i, 1].y -= .9f;
			if (i is 0)
			{
				var vector = DirVec(_segments[i, 0], pos);
				var num = Vector2.Distance(_segments[i, 0], pos);
				pos += vector * (4f - num) * .5f;
				vel += vector * (4f - num) * .5f;
				_segments[i, 0] -= vector * (4f - num) * .5f;
				_segments[i, 1] -= vector * (4f - num) * .5f;
				vel += vector * Mathf.Lerp(.8f, 1.2f, _stressed) / 1.2f;
			}
			else
			{
				var vector2 = DirVec(_segments[i, 0], _segments[i - 1, 0]);
				var num2 = Vector2.Distance(_segments[i, 0], _segments[i - 1, 0]);
				_segments[i - 1, 0] += vector2 * (4f - num2) * .5f;
				_segments[i - 1, 1] += vector2 * (4f - num2) * .5f;
				_segments[i, 0] -= vector2 * (4f - num2) * .5f;
				_segments[i, 1] -= vector2 * (4f - num2) * .5f;
				_segments[i - 1, 1] += vector2 * Mathf.Lerp(.8f, 1.2f, _stressed);
			}
		}
		if (room != null)
		{
			if (_lightSource != null)
			{
				_lightSource.stayAlive = true;
				_lightSource.setPos = pos;
				_lightSource.color = room.game.cameras[0].currentPalette.waterColor1;
				_lightSource.affectedByPaletteDarkness = 0f;
				if (_lightSource.slatedForDeletetion || !submerged)
					_lightSource = null;
			}
			else if (submerged)
			{
				_lightSource = new(pos, false, room.game.cameras[0].currentPalette.waterColor1, this)
				{
					requireUpKeep = true,
					setRad = 200f,
					setAlpha = 1f,
					affectedByPaletteDarkness = 0f
				};
				room.AddObject(_lightSource);
			}
		}
	}

	public override void Reset(Vector2 resetPos)
	{
		base.Reset(resetPos);
		for (var i = 0; i < _segments.GetLength(0); i++)
		{
			_segments[i, 0] = resetPos + RNV();
			_segments[i, 1] = RNV() * Random.value;
		}
	}

	public override void Act()
	{
		base.Act();
		_breath -= 1f / Mathf.Lerp(60f, 10f, _stressed);
		var num = Mathf.Pow(creatureAvoider.FleeSpeed, .3f);
		if (num > _stressed)
			_stressed = LerpAndTick(_stressed, num, .05f, 1f / 60f);
		else
			_stressed = LerpAndTick(_stressed, num, .02f, .005f);
		if (submerged)
		{
			_swimDir += RNV() * Random.value * .5f;
			if (wantToBurrow)
				_swimDir.y -= .5f;
			if (pos.x < 0f)
				_swimDir.x += 1f;
			else if (pos.x > room.PixelWidth)
				_swimDir.x -= 1f;
			if (pos.y < 0f)
				_swimDir.y += 1f;
			if (creatureAvoider.currentWorstCrit != null)
				_swimDir -= DirVec(pos, creatureAvoider.currentWorstCrit.DangerPos) * creatureAvoider.FleeSpeed;
			if (room.water)
				_swimDir = Vector3.Slerp(_swimDir, new(0f, -1f), Mathf.InverseLerp(room.FloatWaterLevel(pos.x) - 100f, room.FloatWaterLevel(pos.x), pos.y) * .5f);
			_swimDir.Normalize();
			vel += (_swimDir * Mathf.Lerp(.8f, 1.1f, _stressed) + RNV() * Random.value * .1f) / 1.2f;
		}
		_rot = Vector3.Slerp(_rot, (-vel - _swimDir).normalized, .2f);
	}

	public override void WallCollision(IntVector2 dir, bool first)
	{
		_swimDir -= RNV() * Random.value + dir.ToVector2();
		_swimDir.Normalize();
	}

	public override void EmergeFromGround(Vector2 emergePos)
	{
		base.EmergeFromGround(emergePos);
		pos = emergePos;
		_swimDir = new(0f, 1f);
	}

	private int _LegSprite(int segment, int leg) => segment * 2 + leg;

	private int _SegmentSprite(int segment, int part) => (_segments.GetLength(0) + 1) * 2 + segment * 2 + part;

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		base.InitiateSprites(sLeaser, rCam);
		sLeaser.sprites = new FSprite[(_segments.GetLength(0) + 1) * 4];
		for (var i = 0; i < _segments.GetLength(0) + 1; i++)
		{
			sLeaser.sprites[_SegmentSprite(i, 0)] = new("Circle20") { anchorY = .3f };
			sLeaser.sprites[_SegmentSprite(i, 1)] = new("Circle20") { anchorY = .4f };
			sLeaser.sprites[_LegSprite(i, 0)] = new("pixel") { anchorY = 0f };
			sLeaser.sprites[_LegSprite(i, 1)] = new("pixel") { anchorY = 0f };
		}
		AddToContainer(sLeaser, rCam, null);
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		var num = Mathf.Lerp(lastInGround, inGround, timeStacker);
		for (var i = 0; i < _segments.GetLength(0) + 1; i++)
		{
			var t = .5f + .5f * Mathf.Sin((Mathf.Lerp(_lastBreath, _breath, timeStacker) + .1f * i) * 7f * (float)Math.PI);
			var p = (i != 0) ? _segments[i - 1, 0] : Vector2.Lerp(lastPos, pos, timeStacker);
			var v = i switch
			{
				0 => (Vector2)(-Vector3.Slerp(_lastRot, _rot, timeStacker)),
				1 => DirVec(p, Vector2.Lerp(lastPos, pos, timeStacker)),
				_ => DirVec(p, _segments[i - 2, 0]),
			};
			p.y -= 5f * num;
			var num3 = LerpMap(i, 0f, _segments.GetLength(0), 1f, .5f, 1.2f) * (1f - num);
			for (var j = 0; j < 2; j++)
			{
				var seg = sLeaser.sprites[_SegmentSprite(i, j)];
				seg.x = p.x - camPos.x;
				seg.y = p.y - camPos.y;
				seg.rotation = VecToDeg(v);
				seg.scaleX = 4f * num3 * (1f - num) / 15f;
				seg.scaleY = 6.5f * num3 * (1f - num) / 15f;
				var leg = sLeaser.sprites[_LegSprite(i, j)];
				leg.x = p.x - PerpendicularVector(v).x * 2f * num3 * ((j != 0) ? 1f : -1f) - camPos.x;
				leg.y = p.y - PerpendicularVector(v).y * 2f * num3 * ((j != 0) ? 1f : -1f) - camPos.y;
				leg.rotation = VecToDeg(v) + (Mathf.Lerp(-20f, 70f, t) + LerpMap(i, 0f, _segments.GetLength(0), 70f, 140f)) * ((j != 0) ? 1f : -1f);
				leg.scaleY = Mathf.Lerp(3.5f + (i * 2), 3f, Mathf.Sin(Mathf.InverseLerp(0f, _segments.GetLength(0), i) * Mathf.PI)) * num3;
			}
		}
	}

	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		base.ApplyPalette(sLeaser, rCam, palette);
		for (var i = 0; i < _segments.GetLength(0) + 1; i++)
		{
			sLeaser.sprites[_SegmentSprite(i, 0)].color = palette.blackColor;
			sLeaser.sprites[_SegmentSprite(i, 1)].color = palette.blackColor;
		}
	}
}
