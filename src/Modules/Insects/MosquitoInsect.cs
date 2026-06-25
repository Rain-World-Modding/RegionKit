using Watcher;

namespace RegionKit.Modules.Insects;

/// <summary>
/// By LB/M4rbleL1ne
/// Mosquito-like cosmetic insect
/// </summary>
public class MosquitoInsect : RedSwarmer
{
	public static bool ProhibitedCreatureType(Creature? creature)
	{
		if (creature == null) return true;
		if (creature is Spider or Leech or Fly or RippleSpider) return false;
		return creature.Template.type.value is "Mosquito" or "AngryMosquito" or "ExplodingMosquito" or "DrainMite";
	}

    private readonly float _bodySize = Mathf.Max(1.1f, .6f + UnityEngine.Random.value);
    private int _wantToSuckCounter = 200, _restCtr;

	///<inheritdoc/>
	public MosquitoInsect(Room room, Vector2 pos) : base(room, pos)
	{
		type = _Enums.MosquitoInsect;
	}

	///<inheritdoc/>
	public override void Update(bool eu)
    {
        base.Update(eu);
        if (room is not Room rm || !alive)
            return;
        vel /= 1.25f;
        rm.PlaySound(SoundID.Spore_Bee_Angry_Buzz, pos, .35f, 1.1f + hue);
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
			List<AbstractCreature> crits = rm.abstractRoom.creatures;
			for (var i = 0; i < crits.Count; i++)
			{
				if (crits[i].realizedCreature is Creature c && !c.dead && (c.abstractCreature.rippleLayer == 0 || c.abstractCreature.rippleBothSides) && !ProhibitedCreatureType(c) && c.Submersion < 1f)
				{
					BodyChunk[] chs = c.bodyChunks;
					for (var k = 0; k < chs.Length; k++)
					{
						BodyChunk ch = chs[k];
						if (DistLess(ch.pos, pos, 100f) && !DistLess(ch.pos, pos, ch.rad + 10f))
						{
							dir = DirVec(pos, ch.pos);
							vel += dir;
							--_wantToSuckCounter;
							break;
						}
					}
				}
			}
		}
        vel += RNV() * 2f;
        if (rm.PointSubmerged(pos))
            vel.y += !rm.waterInverted ? 1f : -1f;
    }

	///<inheritdoc/>
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        var num = Mathf.Lerp(lastInGround, inGround, timeStacker);
		FSprite s0 = sLeaser.sprites[0], s1 = sLeaser.sprites[1], s2 = sLeaser.sprites[2];
		s1.scaleY = -s1.scaleY;
        s1.scaleY /= 3f;
        s2.x = s1.x;
        s2.y = s1.y;
        s2.rotation = s1.rotation;
        s0.scaleY /= _bodySize;
        s0.scaleX /= _bodySize * .7f;
		s0.rotation += 180f;
		s1.rotation += 180f;
		s2.rotation += 180f;
        for (var i = 0; i < 2; i++)
        {
			FSprite spr = sLeaser.sprites[3 + i];
			spr.scaleY = 3.5f * (1f - num);
            spr.scaleX = 1.25f - num;
            spr.rotation = Mathf.Pow(UnityEngine.Random.value, .5f) * 80f * ((UnityEngine.Random.value < .5f) ? (-1f) : 1f) + ((i == 0) ? (-90f) : 90f);
        }
    }

	///<inheritdoc/>
	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        base.ApplyPalette(sLeaser, rCam, palette);
		Color black = palette.blackColor;
		float dark = palette.darkness;
		sLeaser.sprites[0].color = Color.Lerp(HSL2RGB(Mathf.Lerp(.005f, .02f, hue), 1f, .3f), black, .1f + .6f * dark);
        sLeaser.sprites[1].color = black;
        sLeaser.sprites[2].color = black;
        for (var j = 0; j < 2; j++)
            sLeaser.sprites[3 + j].color = Color.Lerp(Color.white, palette.fogColor, .5f + .5f * dark);
    }

	///<inheritdoc/>
	public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
    {
        newContainer = rCam.ReturnFContainer("Items");
		FSprite[] sprs = sLeaser.sprites;
        for (var i = 0; i < sprs.Length; i++)
        {
			sprs[i].RemoveFromContainer();
            newContainer.AddChild(sprs[i]);
        }
    }
}
