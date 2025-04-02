using System.Runtime.CompilerServices;

namespace RegionKit.Modules.Insects;

/// <summary>
/// By LB/M4rbleL1ne
/// Glowing swimmer insect
/// </summary>
internal static class GlowingSwimmersCI
{
	internal static void Apply()
	{
		_CommonHooks.PostRoomLoad += PostRoomLoad;
		On.InsectCoordinator.SpeciesDensity_Type_1 += InsectCoordinatorSpeciesDensityInsectType;
		On.InsectCoordinator.RoomEffectToInsectType += InsectCoordinatorRoomEffectToInsectType;
		On.InsectCoordinator.TileLegalForInsect += InsectCoordinatorTileLegalForInsect;
		On.InsectCoordinator.EffectSpawnChanceForInsect += InsectCoordinatorEffectSpawnChanceForInsect;
		On.InsectCoordinator.CreateInsect += InsectCoordinatorCreateInsect;
	}

	internal static void Undo()
	{
		_CommonHooks.PostRoomLoad -= PostRoomLoad;
		On.InsectCoordinator.SpeciesDensity_Type_1 -= InsectCoordinatorSpeciesDensityInsectType;
		On.InsectCoordinator.RoomEffectToInsectType -= InsectCoordinatorRoomEffectToInsectType;
		On.InsectCoordinator.TileLegalForInsect -= InsectCoordinatorTileLegalForInsect;
		On.InsectCoordinator.EffectSpawnChanceForInsect -= InsectCoordinatorEffectSpawnChanceForInsect;
		On.InsectCoordinator.CreateInsect -= InsectCoordinatorCreateInsect;
	}

	private static void PostRoomLoad(Room self)
	{
		List<RoomSettings.RoomEffect> efs = self.roomSettings.effects;
		for (var i = 0; i < efs.Count; i++)
		{
			RoomSettings.RoomEffect effect = efs[i];
			if (effect.type == _Enums.GlowingSwimmers)
			{
				if (self.insectCoordinator is null)
				{
					self.insectCoordinator = new(self);
					self.AddObject(self.insectCoordinator);
				}
				self.insectCoordinator.AddEffect(effect);
				break;
			}
		}
	}

	private static float InsectCoordinatorSpeciesDensityInsectType(On.InsectCoordinator.orig_SpeciesDensity_Type_1 orig, CosmeticInsect.Type type)
	{
		return type == _Enums.GlowingSwimmerInsect ? .8f : orig(type);
	}

	private static CosmeticInsect.Type InsectCoordinatorRoomEffectToInsectType(On.InsectCoordinator.orig_RoomEffectToInsectType orig, RoomSettings.RoomEffect.Type type)
	{
		return type == _Enums.GlowingSwimmers ? _Enums.GlowingSwimmerInsect : orig(type);
	}

	private static bool InsectCoordinatorTileLegalForInsect(On.InsectCoordinator.orig_TileLegalForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos)
	{
		return type == _Enums.GlowingSwimmerInsect ? room.GetTile(testPos).DeepWater : orig(type, room, testPos);
	}

	private static bool InsectCoordinatorEffectSpawnChanceForInsect(On.InsectCoordinator.orig_EffectSpawnChanceForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos, float effectAmount)
	{
		return type == _Enums.GlowingSwimmerInsect || orig(type, room, testPos, effectAmount);
	}

	private static void InsectCoordinatorCreateInsect(On.InsectCoordinator.orig_CreateInsect orig, InsectCoordinator self, CosmeticInsect.Type type, Vector2 pos, InsectCoordinator.Swarm swarm)
	{
		if (type == _Enums.GlowingSwimmerInsect)
		{
			Room rm = self.room;
			if (!InsectCoordinator.TileLegalForInsect(type, rm, pos) || rm.world.rainCycle.TimeUntilRain < UnityEngine.Random.Range(1200, 1600))
				return;
			var cosmeticInsect = new GlowingSwimmer(rm, pos);
			self.allInsects.Add(cosmeticInsect);
			if (swarm is not null)
			{
				swarm.members.Add(cosmeticInsect);
				cosmeticInsect.mySwarm = swarm;
			}
			rm.AddObject(cosmeticInsect);
		}
		else
			orig(self, type, pos, swarm);
	}
}

/// <summary>
/// By LB/M4rbleL1ne
/// Glowing swimmer
/// </summary>
public class GlowingSwimmer : CosmeticInsect
{
	private static readonly Vector3 __swimBaseVec = new(0f, -1f);
	private Vector2 _rot, _lastRot, _swimDir;
	private float _breath, _lastBreath, _stressed;
	private readonly Vector2[][] _segments;
	private LightSource? _lightSource;

	/// <summary>
	/// Glowing Swimmer
	/// </summary>
	/// <param name="room"></param>
	/// <param name="pos"></param>
	public GlowingSwimmer(Room room, Vector2 pos) : base(room, pos, _Enums.GlowingSwimmerInsect)
	{
		creatureAvoider = new(this, 10, 300f, .3f);
		_breath = UnityEngine.Random.value;
		_segments = new Vector2[2][] { new Vector2[2], new Vector2[2] };
		Reset(pos);
	}

	/// <summary>
	/// Update
	/// </summary>
	/// <param name="eu"></param>
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (room is not Room rm)
			return;
		_lastRot = _rot;
		_lastBreath = _breath;
		if (submerged)
			vel *= .8f / 1.2f;
		else
			vel.y -= .9f / 1.2f;
		Vector2[][] segs = _segments;
		for (var i = 0; i < segs.Length; i++)
		{
			Vector2[] seg = segs[i];
			seg[0] += seg[1];
			if (rm.PointSubmerged(seg[0]))
				seg[1] *= .8f;
			else
				seg[1].y -= .9f;
			Vector2 vector, vecMv;
			if (i == 0)
			{
				vector = DirVec(seg[0], pos);
				vecMv = vector * (4f - Vector2.Distance(seg[0], pos)) * .5f;
				pos += vecMv;
				vel += vecMv;
				seg[0] -= vecMv;
				seg[1] -= vecMv;
				vel += vector * Mathf.Lerp(.8f, 1.2f, _stressed) / 1.2f;
			}
			else
			{
				Vector2[] seg2 = _segments[i - 1];
				vector = DirVec(seg[0], seg2[0]);
				vecMv = vector * (4f - Vector2.Distance(seg[0], seg2[0])) * .5f;
				seg2[0] += vecMv;
				seg2[1] += vecMv;
				seg[0] -= vecMv;
				seg[1] -= vecMv;
				seg2[1] += vector * Mathf.Lerp(.8f, 1.2f, _stressed);
			}
		}
		Color clr = rm.game.cameras[0].currentPalette.waterColor1;
		if (_lightSource is LightSource ls)
		{
			ls.stayAlive = true;
			ls.setPos = pos;
			ls.color = clr;
			ls.affectedByPaletteDarkness = 0f;
			if (ls.slatedForDeletetion || !submerged)
				_lightSource = null;
		}
		else if (submerged)
			rm.AddObject(_lightSource = new(pos, false, clr, this)
			{
				requireUpKeep = true,
				setRad = 200f,
				setAlpha = 1f,
				affectedByPaletteDarkness = 0f
			});
	}

	/// <summary>
	/// Reset
	/// </summary>
	/// <param name="resetPos"></param>
	public override void Reset(Vector2 resetPos)
	{
		base.Reset(resetPos);
		Vector2[][] segs = _segments;
		for (var i = 0; i < segs.Length; i++)
		{
			segs[i][0] = resetPos + RNV();
			segs[i][1] = RNV() * UnityEngine.Random.value;
		}
	}

	/// <summary>
	/// Act
	/// </summary>
	public override void Act()
	{
		if (room is not Room rm)
			return;
		base.Act();
		_breath -= 1f / Mathf.Lerp(60f, 10f, _stressed);
		float fleeSpeed = creatureAvoider.FleeSpeed, num = Mathf.Pow(fleeSpeed, .3f);
		if (num > _stressed)
			_stressed = LerpAndTick(_stressed, num, .05f, 1f / 60f);
		else
			_stressed = LerpAndTick(_stressed, num, .02f, .005f);
		if (submerged)
		{
			_swimDir += RNV() * UnityEngine.Random.value * .5f;
			if (wantToBurrow)
				_swimDir.y -= .5f;
			if (pos.x < 0f)
				_swimDir.x += 1f;
			else if (pos.x > rm.PixelWidth)
				_swimDir.x -= 1f;
			if (pos.y < 0f)
				_swimDir.y += 1f;
			if (creatureAvoider.currentWorstCrit is Creature c)
				_swimDir -= DirVec(pos, c.DangerPos) * fleeSpeed;
			if (rm.water)
			{
				float waterLvl = rm.FloatWaterLevel(pos);
				_swimDir = Vector3.Slerp(_swimDir, __swimBaseVec, Mathf.InverseLerp(waterLvl - 100f, waterLvl, pos.y) * .5f);
			}
			_swimDir.Normalize();
			vel += (_swimDir * Mathf.Lerp(.8f, 1.1f, _stressed) + RNV() * UnityEngine.Random.value * .1f) / 1.2f;
		}
		_rot = Vector3.Slerp(_rot, (-vel - _swimDir).normalized, .2f);
	}

	/// <summary>
	/// Wall collision
	/// </summary>
	/// <param name="dir"></param>
	/// <param name="first"></param>
	public override void WallCollision(IntVector2 dir, bool first)
	{
		_swimDir -= RNV() * UnityEngine.Random.value + dir.ToVector2();
		_swimDir.Normalize();
	}

	/// <summary>
	/// Emerge from ground
	/// </summary>
	/// <param name="emergePos"></param>
	public override void EmergeFromGround(Vector2 emergePos)
	{
		base.EmergeFromGround(emergePos);
		pos = emergePos;
		_swimDir = new(0f, 1f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int LegSprite(int segment, int leg) => segment * 2 + leg;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int SegmentSprite(int segment, int part) => (_segments.Length + 1) * 2 + segment * 2 + part;

	/// <summary>
	/// Initiate sprites
	/// </summary>
	/// <param name="sLeaser"></param>
	/// <param name="rCam"></param>
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		base.InitiateSprites(sLeaser, rCam);
		int lgt = _segments.Length + 1;
		sLeaser.sprites = new FSprite[lgt * 4];
		for (var i = 0; i < lgt; i++)
		{
			sLeaser.sprites[SegmentSprite(i, 0)] = new("Circle20") { anchorY = .3f };
			sLeaser.sprites[SegmentSprite(i, 1)] = new("Circle20") { anchorY = .4f };
			sLeaser.sprites[LegSprite(i, 0)] = new("pixel") { anchorY = 0f };
			sLeaser.sprites[LegSprite(i, 1)] = new("pixel") { anchorY = 0f };
		}
		AddToContainer(sLeaser, rCam, null);
	}

	/// <summary>
	/// Draw sprites
	/// </summary>
	/// <param name="sLeaser"></param>
	/// <param name="rCam"></param>
	/// <param name="timeStacker"></param>
	/// <param name="camPos"></param>
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		float num = Mathf.Lerp(lastInGround, inGround, timeStacker);
		int segLen = _segments.Length;
		for (var i = 0; i < segLen + 1; i++)
		{
			float t = .5f + .5f * Mathf.Sin((Mathf.Lerp(_lastBreath, _breath, timeStacker) + .1f * i) * 7f * Mathf.PI);
			Vector2 p = (i != 0) ? _segments[i - 1][0] : Vector2.Lerp(lastPos, pos, timeStacker), v = i switch
			{
				0 => (Vector2)(-Vector3.Slerp(_lastRot, _rot, timeStacker)),
				1 => DirVec(p, Vector2.Lerp(lastPos, pos, timeStacker)),
				_ => DirVec(p, _segments[i - 2][0]),
			};
			p.y -= 5f * num;
			float num3 = LerpMap(i, 0f, segLen, 1f, .5f, 1.2f) * (1f - num), tempNum = num3 * (1f - num) / 15f, degRot = VecToDeg(v);
			Vector2 perp = PerpendicularVector(v);
			for (var j = 0; j < 2; j++)
			{
				FSprite seg = sLeaser.sprites[SegmentSprite(i, j)], leg = sLeaser.sprites[LegSprite(i, j)];
				seg.x = p.x - camPos.x;
				seg.y = p.y - camPos.y;
				seg.rotation = degRot;
				seg.scaleX = 4f * tempNum;
				seg.scaleY = 6.5f * tempNum;
				float jTestAdd = (j != 0) ? 1f : -1f, temp2 = 2f * num3 * jTestAdd;
				leg.x = p.x - perp.x * temp2 - camPos.x;
				leg.y = p.y - perp.y * temp2 - camPos.y;
				leg.rotation = degRot + (Mathf.Lerp(-20f, 70f, t) + LerpMap(i, 0f, segLen, 70f, 140f)) * jTestAdd;
				leg.scaleY = Mathf.Lerp(3.5f + (i * 2), 3f, Mathf.Sin(Mathf.InverseLerp(0f, segLen, i) * Mathf.PI)) * num3;
			}
		}
	}

	/// <summary>
	/// Apply palette
	/// </summary>
	/// <param name="sLeaser"></param>
	/// <param name="rCam"></param>
	/// <param name="palette"></param>
	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		base.ApplyPalette(sLeaser, rCam, palette);
		Color black = palette.blackColor;
		for (var i = 0; i < _segments.Length + 1; i++)
		{
			sLeaser.sprites[SegmentSprite(i, 0)].color = black;
			sLeaser.sprites[SegmentSprite(i, 1)].color = black;
		}
	}
}
