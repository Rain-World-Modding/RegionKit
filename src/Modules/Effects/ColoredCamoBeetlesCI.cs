namespace RegionKit.Modules.Effects;

/// <summary>
/// By LB/M4rbleL1ne
/// Colored camo beetle insect
/// </summary>
internal static class ColoredCamoBeetlesCI
{
    internal static void Apply()
    {
		On.InsectCoordinator.CreateInsect += InsectCoordinatorCreateInsect;
		On.InsectCoordinator.TileLegalForInsect += InsectCoordinatorTileLegalForInsect;
		On.InsectCoordinator.EffectSpawnChanceForInsect += InsectCoordinatorEffectSpawnChanceForInsect;
		On.InsectCoordinator.RoomEffectToInsectType += InsectCoordinatorRoomEffectToInsectType;
		_CommonHooks.PostRoomLoad += PostRoomLoad;
    }

	internal static void Undo()
	{
		On.InsectCoordinator.CreateInsect -= InsectCoordinatorCreateInsect;
		On.InsectCoordinator.TileLegalForInsect -= InsectCoordinatorTileLegalForInsect;
		On.InsectCoordinator.EffectSpawnChanceForInsect -= InsectCoordinatorEffectSpawnChanceForInsect;
		On.InsectCoordinator.RoomEffectToInsectType -= InsectCoordinatorRoomEffectToInsectType;
		_CommonHooks.PostRoomLoad -= PostRoomLoad;
	}

	private static void PostRoomLoad(Room self)
	{
		for (var i = 0; i < self.roomSettings.effects.Count; i++)
		{
			RoomSettings.RoomEffect ef = self.roomSettings.effects[i];
			if (ef.type == _Enums.ColoredCamoBeetles)
			{
				if (self.insectCoordinator == null)
				{
					self.insectCoordinator = new(self);
					self.AddObject(self.insectCoordinator);
				}
				self.insectCoordinator.AddEffect(ef);
			}
		}
	}

	private static CosmeticInsect.Type InsectCoordinatorRoomEffectToInsectType(On.InsectCoordinator.orig_RoomEffectToInsectType orig, RoomSettings.RoomEffect.Type type)
	{
		return type == _Enums.ColoredCamoBeetles ? _Enums.ColoredCamoBeetle : orig(type);
	}

	private static bool InsectCoordinatorEffectSpawnChanceForInsect(On.InsectCoordinator.orig_EffectSpawnChanceForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos, float effectAmount)
	{
		var res = orig(type, room, testPos, effectAmount);
		if (type == _Enums.ColoredCamoBeetle)
		{
			if (room.readyForAI)
				res = !room.aimap.getAItile(testPos).narrowSpace;
			else
				res = true;
		}
		return res;
	}

	private static bool InsectCoordinatorTileLegalForInsect(On.InsectCoordinator.orig_TileLegalForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos)
	{
		var res = orig(type, room, testPos);
		if (type == _Enums.ColoredCamoBeetle)
			res = !room.GetTile(testPos).AnyWater;
		return res;
	}

	private static void InsectCoordinatorCreateInsect(On.InsectCoordinator.orig_CreateInsect orig, InsectCoordinator self, CosmeticInsect.Type type, Vector2 pos, InsectCoordinator.Swarm swarm)
	{
		if (type == _Enums.ColoredCamoBeetle)
		{
			if (!InsectCoordinator.TileLegalForInsect(type, self.room, pos) || self.room.world.rainCycle.TimeUntilRain < RNG.Range(1200, 1600))
				return;
			if (!self.room.readyForAI || self.room.aimap.getAItile(pos).terrainProximity < 5)
			{
				for (var j = 0; j < 5; j++)
				{
					Vector2? vector2 = SharedPhysics.ExactTerrainRayTracePos(self.room, pos, pos + RNV() * 100f);
					if (vector2.HasValue)
					{
						pos = vector2.Value;
						break;
					}
				}
			}
			var cosmeticInsect = new ColoredCamoBeetleInsect(self.room, pos);
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
/// New insect that can camo and uses one of the effect colors when stressed
/// </summary>
public class ColoredCamoBeetleInsect : Beetle
{
    float _lerper;
    bool _lerpUp = true;
    internal const float ADD = .075f;
    readonly int _effectColor;
    int _middleSleepCtr;

    bool CanMiddleSleep => _middleSleepCtr > 0 && stressed < .6f && room?.GetTile(pos).wallbehind is true;

	/// <summary>
	/// Insect ctor
	/// </summary>
	/// <param name="room"></param>
	/// <param name="pos"></param>
    public ColoredCamoBeetleInsect(Room room, Vector2 pos) : base(room, pos)
    {
        type = _Enums.ColoredCamoBeetle;
        _effectColor = RNG.Range(0, 2);
    }

	/// <summary>
	/// Update
	/// </summary>
	/// <param name="eu"></param>
    public override void Update(bool eu)
    {
        if (_lerper <= -.25f)
            _lerpUp = true;
        else if (_lerper >= 1f)
            _lerpUp = false;
        if (_lerpUp)
            _lerper += ADD;
        else
            _lerper -= ADD;
        if (!sitPos.HasValue && CanMiddleSleep)
            vel.y += .8f;
        base.Update(eu);
    }

	/// <summary>
	/// Act
	/// </summary>
    public override void Act()
    {
        base.Act();
        if (RNG.value < .05f && _middleSleepCtr == 0)
            _middleSleepCtr = 200;
        if (CanMiddleSleep)
        {
            vel *= 0f;
            wingsOut = 0f;
            _middleSleepCtr--;
        }
    }

	/// <summary>
	/// Initiate sprites
	/// </summary>
	/// <param name="sLeaser"></param>
	/// <param name="rCam"></param>
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        base.InitiateSprites(sLeaser, rCam);
        AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("GrabShaders"));
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
        sLeaser.sprites[0].scaleX *= 1.4f;
        sLeaser.sprites[0].scaleY *= 1.45f;
        Color eCol = rCam.currentPalette.texture.GetPixel(30, 5 - _effectColor * 2), bCol = rCam.currentPalette.blackColor;
        for (var i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].color = Color.Lerp(bCol, eCol, _lerper);
            sLeaser.sprites[i].alpha = (stressed - .4f) * 2f;
        }
    }
}
