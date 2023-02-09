namespace RegionKit.Modules.Effects;

/// <summary>
/// By LB/M4rbleL1ne
/// </summary>
internal static class GlowingSwimmersCI
{

	internal static void Apply()
	{
		_CommonHooks.PostRoomLoad += RoomPostLoad;
		On.InsectCoordinator.SpeciesDensity_Type_1 += InsectCoordinatorSpeciesDensityInsectType;
		On.InsectCoordinator.RoomEffectToInsectType += InsectCoordinatorRoomEffectToInsectType;
		On.InsectCoordinator.TileLegalForInsect += InsectCoordinatorTileLegalForInsect;
		On.InsectCoordinator.EffectSpawnChanceForInsect += InsectCoordinatorEffectSpawnChanceForInsect;
		On.InsectCoordinator.CreateInsect += InsectCoordinatorCreateInsect;
	}

	internal static void Undo()
	{
		_CommonHooks.PostRoomLoad -= RoomPostLoad;
		On.InsectCoordinator.SpeciesDensity_Type_1 -= InsectCoordinatorSpeciesDensityInsectType;
		On.InsectCoordinator.RoomEffectToInsectType -= InsectCoordinatorRoomEffectToInsectType;
		On.InsectCoordinator.TileLegalForInsect -= InsectCoordinatorTileLegalForInsect;
		On.InsectCoordinator.EffectSpawnChanceForInsect -= InsectCoordinatorEffectSpawnChanceForInsect;
		On.InsectCoordinator.CreateInsect -= InsectCoordinatorCreateInsect;
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

	private static float InsectCoordinatorSpeciesDensityInsectType(On.InsectCoordinator.orig_SpeciesDensity_Type_1 orig, CosmeticInsect.Type type)
	{
		return type == Enums_GlowingSwimmers.GlowingSwimmerInsect ? .8f : orig(type);
	}

	private static CosmeticInsect.Type InsectCoordinatorRoomEffectToInsectType(On.InsectCoordinator.orig_RoomEffectToInsectType orig, RoomSettings.RoomEffect.Type type)
	{
		return type == Enums_GlowingSwimmers.GlowingSwimmers ? Enums_GlowingSwimmers.GlowingSwimmerInsect : orig(type);
	}

	private static bool InsectCoordinatorTileLegalForInsect(On.InsectCoordinator.orig_TileLegalForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos)
	{
		return type == Enums_GlowingSwimmers.GlowingSwimmerInsect ? room.GetTile(testPos).DeepWater : orig(type, room, testPos);
	}

	private static bool InsectCoordinatorEffectSpawnChanceForInsect(On.InsectCoordinator.orig_EffectSpawnChanceForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos, float effectAmount)
	{
		return type == Enums_GlowingSwimmers.GlowingSwimmerInsect || orig(type, room, testPos, effectAmount);
	}

	private static void InsectCoordinatorCreateInsect(On.InsectCoordinator.orig_CreateInsect orig, InsectCoordinator self, CosmeticInsect.Type type, Vector2 pos, InsectCoordinator.Swarm swarm)
	{
		if (type == Enums_GlowingSwimmers.GlowingSwimmerInsect)
		{
			if (!InsectCoordinator.TileLegalForInsect(type, self.room, pos) || self.room.world.rainCycle.TimeUntilRain < RNG.Range(1200, 1600))
				return;
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
	}
}

/// <summary>
/// Glowing swimmer
/// </summary>
public class GlowingSwimmer : CosmeticInsect
{
	private float _stressed;
	private Vector2 _rot;
	private Vector2 _lastRot;
	private Vector2 _swimDir;
	private float _breath;
	private float _lastBreath;
	private readonly Vector2[,] _segments;
	private LightSource? _lightSource;

	public GlowingSwimmer(Room room, Vector2 pos) : base(room, pos, _Enums.GlowingSwimmerInsect)
	{
		creatureAvoider = new(this, 10, 300f, .3f);
		_breath = RNG.value;
		_segments = new Vector2[2, 2];
		Reset(pos);
	}

	/// <summary>
	/// Update
	/// </summary>
	/// <param name="eu"></param>
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
			if (i == 0)
			{
				Vector2 vector = DirVec(_segments[i, 0], pos);
				var num = Vector2.Distance(_segments[i, 0], pos);
				pos += vector * (4f - num) * .5f;
				vel += vector * (4f - num) * .5f;
				_segments[i, 0] -= vector * (4f - num) * .5f;
				_segments[i, 1] -= vector * (4f - num) * .5f;
				vel += vector * Mathf.Lerp(.8f, 1.2f, _stressed) / 1.2f;
			}
			else
			{
				Vector2 vector2 = DirVec(_segments[i, 0], _segments[i - 1, 0]);
				var num2 = Vector2.Distance(_segments[i, 0], _segments[i - 1, 0]);
				_segments[i - 1, 0] += vector2 * (4f - num2) * .5f;
				_segments[i - 1, 1] += vector2 * (4f - num2) * .5f;
				_segments[i, 0] -= vector2 * (4f - num2) * .5f;
				_segments[i, 1] -= vector2 * (4f - num2) * .5f;
				_segments[i - 1, 1] += vector2 * Mathf.Lerp(.8f, 1.2f, _stressed);
			}
		}
		if (room is Room rm)
		{
			if (_lightSource is LightSource ls)
			{
				ls.stayAlive = true;
				ls.setPos = pos;
				ls.color = rm.game.cameras[0].currentPalette.waterColor1;
				ls.affectedByPaletteDarkness = 0f;
				if (ls.slatedForDeletetion || !submerged)
					_lightSource = null;
			}
			else if (submerged)
				room.AddObject(_lightSource = new(pos, false, rm.game.cameras[0].currentPalette.waterColor1, this)
				{
					requireUpKeep = true,
					setRad = 200f,
					setAlpha = 1f,
					affectedByPaletteDarkness = 0f
				});
		}
	}

	/// <summary>
	/// Reset
	/// </summary>
	/// <param name="resetPos"></param>
	public override void Reset(Vector2 resetPos)
	{
		base.Reset(resetPos);
		for (var i = 0; i < _segments.GetLength(0); i++)
		{
			_segments[i, 0] = resetPos + RNV();
			_segments[i, 1] = RNV() * RNG.value;
		}
	}

	/// <summary>
	/// Act
	/// </summary>
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
			_swimDir += RNV() * RNG.value * .5f;
			if (wantToBurrow)
				_swimDir.y -= .5f;
			if (pos.x < 0f)
				_swimDir.x += 1f;
			else if (pos.x > room?.PixelWidth)
				_swimDir.x -= 1f;
			if (pos.y < 0f)
				_swimDir.y += 1f;
			if (creatureAvoider.currentWorstCrit is Creature c)
				_swimDir -= DirVec(pos, c.DangerPos) * creatureAvoider.FleeSpeed;
			if (room is Room rm && rm.water)
				_swimDir = Vector3.Slerp(_swimDir, new(0f, -1f), Mathf.InverseLerp(rm.FloatWaterLevel(pos.x) - 100f, rm.FloatWaterLevel(pos.x), pos.y) * .5f);
			_swimDir.Normalize();
			vel += (_swimDir * Mathf.Lerp(.8f, 1.1f, _stressed) + RNV() * RNG.value * .1f) / 1.2f;
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
		_swimDir -= RNV() * RNG.value + dir.ToVector2();
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

	private int LegSprite(int segment, int leg) => segment * 2 + leg;

	private int SegmentSprite(int segment, int part) => (_segments.GetLength(0) + 1) * 2 + segment * 2 + part;

	/// <summary>
	/// Initiate sprites
	/// </summary>
	/// <param name="sLeaser"></param>
	/// <param name="rCam"></param>
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		base.InitiateSprites(sLeaser, rCam);
		sLeaser.sprites = new FSprite[(_segments.GetLength(0) + 1) * 4];
		for (var i = 0; i < _segments.GetLength(0) + 1; i++)
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
		var num = Mathf.Lerp(lastInGround, inGround, timeStacker);
		for (var i = 0; i < _segments.GetLength(0) + 1; i++)
		{
			var t = .5f + .5f * Mathf.Sin((Mathf.Lerp(_lastBreath, _breath, timeStacker) + .1f * i) * 7f * Mathf.PI);
			Vector2 p = (i != 0) ? _segments[i - 1, 0] : Vector2.Lerp(lastPos, pos, timeStacker), v = i switch
			{
				0 => (Vector2)(-Vector3.Slerp(_lastRot, _rot, timeStacker)),
				1 => DirVec(p, Vector2.Lerp(lastPos, pos, timeStacker)),
				_ => DirVec(p, _segments[i - 2, 0]),
			};
			p.y -= 5f * num;
			var num3 = LerpMap(i, 0f, _segments.GetLength(0), 1f, .5f, 1.2f) * (1f - num);
			for (var j = 0; j < 2; j++)
			{
				FSprite seg = sLeaser.sprites[SegmentSprite(i, j)], leg = sLeaser.sprites[LegSprite(i, j)];
				seg.x = p.x - camPos.x;
				seg.y = p.y - camPos.y;
				seg.rotation = VecToDeg(v);
				seg.scaleX = 4f * num3 * (1f - num) / 15f;
				seg.scaleY = 6.5f * num3 * (1f - num) / 15f;
				leg.x = p.x - PerpendicularVector(v).x * 2f * num3 * ((j != 0) ? 1f : -1f) - camPos.x;
				leg.y = p.y - PerpendicularVector(v).y * 2f * num3 * ((j != 0) ? 1f : -1f) - camPos.y;
				leg.rotation = VecToDeg(v) + (Mathf.Lerp(-20f, 70f, t) + LerpMap(i, 0f, _segments.GetLength(0), 70f, 140f)) * ((j != 0) ? 1f : -1f);
				leg.scaleY = Mathf.Lerp(3.5f + (i * 2), 3f, Mathf.Sin(Mathf.InverseLerp(0f, _segments.GetLength(0), i) * Mathf.PI)) * num3;
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
		for (var i = 0; i < _segments.GetLength(0) + 1; i++)
		{
			sLeaser.sprites[SegmentSprite(i, 0)].color = palette.blackColor;
			sLeaser.sprites[SegmentSprite(i, 1)].color = palette.blackColor;
		}
	}
}
