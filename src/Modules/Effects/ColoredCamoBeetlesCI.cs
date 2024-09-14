using System.Runtime.CompilerServices;

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
		List<RoomSettings.RoomEffect> efs = self.roomSettings.effects;
		for (var i = 0; i < efs.Count; i++)
		{
			RoomSettings.RoomEffect ef = efs[i];
			if (ef.type == _Enums.ColoredCamoBeetles)
			{
				if (self.insectCoordinator is null)
				{
					self.insectCoordinator = new(self);
					self.AddObject(self.insectCoordinator);
				}
				self.insectCoordinator.AddEffect(ef);
				break;
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
		if (type == _Enums.ColoredCamoBeetle)
			return !room.GetTile(testPos).AnyWater;
		return orig(type, room, testPos);
	}

	private static void InsectCoordinatorCreateInsect(On.InsectCoordinator.orig_CreateInsect orig, InsectCoordinator self, CosmeticInsect.Type type, Vector2 pos, InsectCoordinator.Swarm swarm)
	{
		if (type == _Enums.ColoredCamoBeetle)
		{
			Room rm = self.room;
			if (!InsectCoordinator.TileLegalForInsect(type, rm, pos) || rm.world.rainCycle.TimeUntilRain < UnityEngine.Random.Range(1200, 1600))
				return;
			if (!rm.readyForAI || rm.aimap.getTerrainProximity(pos) < 5)
			{
				for (var j = 0; j < 5; j++)
				{
					Vector2? vector2 = SharedPhysics.ExactTerrainRayTracePos(rm, pos, pos + RNV() * 100f);
					if (vector2.HasValue)
					{
						pos = vector2.Value;
						break;
					}
				}
			}
			var cosmeticInsect = new ColoredCamoBeetleInsect(rm, pos);
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
/// New insect that can camo and uses one of the effect colors when stressed
/// </summary>
public class ColoredCamoBeetleInsect : Beetle
{
	private float _lerper;
	private bool _lerpUp = true;
	internal const float ADD = .075f;
	private readonly int _effectColor;
	private int _middleSleepCtr;

	private bool CanMiddleSleep
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _middleSleepCtr > 0 && stressed < .6f && room?.GetTile(pos).wallbehind is true;
	}

	/// <summary>
	/// Insect ctor
	/// </summary>
	/// <param name="room"></param>
	/// <param name="pos"></param>
    public ColoredCamoBeetleInsect(Room room, Vector2 pos) : base(room, pos)
    {
        type = _Enums.ColoredCamoBeetle;
        _effectColor = UnityEngine.Random.Range(0, 2);
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
        if (UnityEngine.Random.value < .05f && _middleSleepCtr == 0)
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
		FSprite[] sprites = sLeaser.sprites;
		sprites[0].scaleX *= 1.4f;
        sprites[0].scaleY *= 1.45f;
        Color eCol = rCam.currentPalette.texture.GetPixel(30, 5 - _effectColor * 2), bCol = rCam.currentPalette.blackColor;
        for (var i = 0; i < sprites.Length; i++)
        {
            sprites[i].color = Color.Lerp(bCol, eCol, _lerper);
            sprites[i].alpha = (stressed - .4f) * 2f;
        }
    }
}
