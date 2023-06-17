namespace RegionKit.Modules.Effects;

/// <summary>
/// By LB/M4rbleL1ne
/// Mosquito-like cosmetic insect
/// </summary>
internal static class MosquitoInsectsCI
{
    internal static void Apply()
    {
		On.InsectCoordinator.CreateInsect += InsectCoordinatorCreateInsect;
		On.InsectCoordinator.TileLegalForInsect += InsectCoordinatorTileLegalForInsect;
		On.InsectCoordinator.EffectSpawnChanceForInsect += InsectCoordinatorEffectSpawnChanceForInsect;
		On.InsectCoordinator.SpeciesDensity_Type_1 += InsectCoordinatorSpeciesDensity;
		On.InsectCoordinator.RoomEffectToInsectType += InsectCoordinatorRoomEffectToInsectType;
		_CommonHooks.PostRoomLoad += PostRoomLoad;
    }

	internal static void Undo()
	{
		On.InsectCoordinator.CreateInsect -= InsectCoordinatorCreateInsect;
		On.InsectCoordinator.TileLegalForInsect -= InsectCoordinatorTileLegalForInsect;
		On.InsectCoordinator.EffectSpawnChanceForInsect -= InsectCoordinatorEffectSpawnChanceForInsect;
		On.InsectCoordinator.SpeciesDensity_Type_1 -= InsectCoordinatorSpeciesDensity;
		On.InsectCoordinator.RoomEffectToInsectType -= InsectCoordinatorRoomEffectToInsectType;
		_CommonHooks.PostRoomLoad -= PostRoomLoad;
	}

	private static CosmeticInsect.Type InsectCoordinatorRoomEffectToInsectType(On.InsectCoordinator.orig_RoomEffectToInsectType orig, RoomSettings.RoomEffect.Type type)
	{
		return type == _Enums.MosquitoInsects ? _Enums.MosquitoInsect : orig(type);
	}

	private static float InsectCoordinatorSpeciesDensity(On.InsectCoordinator.orig_SpeciesDensity_Type_1 orig, CosmeticInsect.Type type)
	{
		return type == _Enums.MosquitoInsect ? 1.5f : orig(type);
	}

	private static void PostRoomLoad(Room self)
	{
		for (var i = 0; i < self.roomSettings.effects.Count; i++)
		{
			RoomSettings.RoomEffect ef = self.roomSettings.effects[i];
			if (ef.type == _Enums.MosquitoInsects)
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

	private static bool InsectCoordinatorEffectSpawnChanceForInsect(On.InsectCoordinator.orig_EffectSpawnChanceForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos, float effectAmount)
	{
		return type == _Enums.MosquitoInsect || orig(type, room, testPos, effectAmount);
	}

	private static bool InsectCoordinatorTileLegalForInsect(On.InsectCoordinator.orig_TileLegalForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos)
	{
		var res = orig(type, room, testPos);
		if (type == _Enums.MosquitoInsect)
			res = !room.GetTile(testPos).DeepWater;
		return res;
	}

	private static void InsectCoordinatorCreateInsect(On.InsectCoordinator.orig_CreateInsect orig, InsectCoordinator self, CosmeticInsect.Type type, Vector2 pos, InsectCoordinator.Swarm swarm)
	{
		if (type == _Enums.MosquitoInsect)
		{
			if (!InsectCoordinator.TileLegalForInsect(type, self.room, pos) || self.room.world.rainCycle.TimeUntilRain < UnityEngine.Random.Range(1200, 1600))
				return;
			var cosmeticInsect = new MosquitoInsect(self.room, pos);
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
/// By LB/M4rbleL1ne
/// Mosquito-like cosmetic insect
/// </summary>
public class MosquitoInsect : RedSwarmer
{
    private readonly float _bodySize = Mathf.Max(1.1f, .6f + UnityEngine.Random.value);
    private int _wantToSuckCounter = 200, _restCtr;

	///<inheritdoc/>
	public MosquitoInsect(Room room, Vector2 pos) : base(room, pos) => type = _Enums.MosquitoInsect;

	///<inheritdoc/>
	public override void Update(bool eu)
    {
        base.Update(eu);
        if (room is null || !alive)
            return;
        vel /= 1.25f;
        room.PlaySound(SoundID.Spore_Bee_Angry_Buzz, pos, .35f, 1.1f + hue);
        var mosTp = new CreatureTemplate.Type("Mosquito");
        if (_wantToSuckCounter == 0)
        {
            _restCtr++;
            if (_restCtr > 150)
            {
                _wantToSuckCounter = 200;
                _restCtr = 0;
            }
        }
        if (_wantToSuckCounter > 0)
        {
            for (var i = 0; i < room.physicalObjects.Length; i++)
            {
				List<PhysicalObject> list = room.physicalObjects[i];
                for (var j = 0; j < list.Count; j++)
                {
                    if (list[j] is Creature c && !c.dead && c.Template.type != mosTp && c.Submersion < 1f)
                    {
                        for (var k = 0; k < c.bodyChunks.Length; k++)
                        {
							BodyChunk ch = c.bodyChunks[k];
                            if (DistLess(ch.pos, pos, 100f) && !DistLess(ch.pos, pos, ch.rad + 10f))
                            {
                                dir = DirVec(pos, ch.pos);
                                vel += dir;
                                _wantToSuckCounter--;
                                break;
                            }
                        }
                    }
                }
            }
        }
        vel += RNV() * 2f;
        if (room.PointSubmerged(pos))
            vel.y += !room.waterInverted ? 1f : -1f;
    }

	///<inheritdoc/>
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        var num = Mathf.Lerp(lastInGround, inGround, timeStacker);
        sLeaser.sprites[1].scaleY = -sLeaser.sprites[1].scaleY;
        sLeaser.sprites[1].scaleY /= 3f;
        sLeaser.sprites[2].x = sLeaser.sprites[1].x;
        sLeaser.sprites[2].y = sLeaser.sprites[1].y;
        sLeaser.sprites[2].rotation = sLeaser.sprites[1].rotation;
        sLeaser.sprites[0].scaleY /= _bodySize;
        sLeaser.sprites[0].scaleX /= _bodySize * .7f;
        for (var i = 0; i < 3; i++)
            sLeaser.sprites[i].rotation += 180f;
        for (var i = 0; i < 2; i++)
        {
            sLeaser.sprites[3 + i].scaleY = 3.5f * (1f - num);
            sLeaser.sprites[3 + i].scaleX = 1.25f - num;
            sLeaser.sprites[3 + i].rotation = Mathf.Pow(UnityEngine.Random.value, .5f) * 80f * ((UnityEngine.Random.value < .5f) ? (-1f) : 1f) + ((i == 0) ? (-90f) : 90f);
        }
    }

	///<inheritdoc/>
	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        base.ApplyPalette(sLeaser, rCam, palette);
        sLeaser.sprites[0].color = Color.Lerp(HSL2RGB(Mathf.Lerp(.005f, .02f, hue), 1f, .3f), palette.blackColor, .1f + .6f * palette.darkness);
        sLeaser.sprites[1].color = palette.blackColor;
        sLeaser.sprites[2].color = palette.blackColor;
        for (var j = 0; j < 2; j++)
            sLeaser.sprites[3 + j].color = Color.Lerp(Color.white, palette.fogColor, .5f + .5f * palette.darkness);
    }

	///<inheritdoc/>
	public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        newContainer = rCam.ReturnFContainer("Items");
        foreach (FSprite fSprite in sLeaser.sprites)
        {
            fSprite.RemoveFromContainer();
            newContainer.AddChild(fSprite);
        }
    }
}
