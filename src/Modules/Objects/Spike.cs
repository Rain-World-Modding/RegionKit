using RegionKit.Modules.TheMast;
using System.Text.RegularExpressions;
using System.Globalization;

namespace RegionKit.Modules.Objects;

public class SpikeObj : ManagedObjectType
{
	public SpikeObj() : base("Spike", _Module.GAMEPLAY_POM_CATEGORY, typeof(Spike), typeof(PlacedObject.ResizableObjectData), typeof(DevInterface.ResizeableObjectRepresentation))
	{

	}

	public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
	{
		int m = room.roomSettings.placedObjects.IndexOf(placedObject);
		if (room.game.session is not StoryGameSession)
		{
			room.AddObject(new Spike(room, placedObject, false));
		}
		else
		{
			bool broken = (room.game.session as StoryGameSession).saveState.ItemConsumed(room.world, false, room.abstractRoom.index, m);
			room.AddObject(new Spike(room, placedObject, broken));
		}

		return null;
	}
}

public class Spike : UpdatableAndDeletable, IDrawable, Explosion.IReactToExplosions
{
	private readonly PlacedObject po;
	private WorldCoordinate wc;
	private readonly Vector2[] stickPositions;
	private FloatRect impalePoint;
	public Creature? impaledCreature;
	public BodyChunk? impaledChunk;
	private float damageOverTime = 0.75f;
	private float slide = 0f;
	private float breakSize = 5f;
	private readonly float varA;
	private readonly float varB;
	private float tipWidth = 0.5f;
	private Color color = new(1f, 1f, 1f);
	public Color tipColor = new(1f, 1f, 1f);
	private bool broken = false;
	private bool snap = false;
	private bool updateFade = false;
	public bool updateTipColor = false;
	private readonly int index;
	private readonly float impaleChance = UnityEngine.Random.value;
	private readonly float startAngle;

	public Spike(Room room, PlacedObject pObj, bool broken)
	{
		this.room = room;
		this.broken = broken;
		if (this.broken)
		{
			breakSize = -45f;
			tipWidth = 2f;
		}
		po = pObj;
		for (int i = 0; i < this.room.roomSettings.placedObjects.Count; i++)
		{
			if (this.room.roomSettings.placedObjects[i] == po)
			{
				index = i;
				break;
			}
		}
		wc = this.room.GetWorldCoordinate(room.GetTilePosition(po.pos));
		stickPositions = new Vector2[(int)Mathf.Clamp((pObj.data as PlacedObject.ResizableObjectData).handlePos.magnitude / 11f, 3f, 30f)];
		impalePoint = new FloatRect(po.pos.x - 13f, po.pos.y - 13f, po.pos.x + 13f, po.pos.y + 13f);
		varA = UnityEngine.Random.Range(0.5f, 1f);
		varB = UnityEngine.Random.Range(0.5f, 1f);
		if (room.game.IsStorySession)
		{
			snap = room.game.rainWorld.progression.currentSaveState.ItemConsumed(room.world, false, room.abstractRoom.index, index);
		}
		startAngle = Custom.AimFromOneVectorToAnother(po.pos, po.pos + (po.data as PlacedObject.ResizableObjectData).handlePos);
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		if (broken) return;

		var data = po.data as PlacedObject.ResizableObjectData;
		Vector2 poPos = po.pos;
		Vector2 handlePos = data.handlePos;
		float angle = Custom.AimFromOneVectorToAnother(poPos, poPos + handlePos);

		if (!snap)
		{
			foreach (var objList in room.physicalObjects)
			{
				foreach (var obj in objList)
				{
					for (int k = 0; k < obj.bodyChunks.Length; k++)
					{
						var chunk = obj.bodyChunks[k];
						Vector2 contactVec = chunk.ContactPoint.ToVector2();
						Vector2 checkPos = chunk.pos + contactVec * (chunk.rad + 30f);

						if (impalePoint.Vector2Inside(checkPos))
						{
							float impaleDir = Custom.AimFromOneVectorToAnother(chunk.lastPos, chunk.pos);
							Vector2 ang1 = Custom.DegToVec(angle);
							Vector2 ang2 = Custom.DegToVec(impaleDir);

							if (Vector2.Distance(ang1, ang2) < 0.5f &&
								Vector2.Distance(chunk.pos, chunk.lastLastPos) > 15f &&
								obj is Creature creature)
							{
								float mass = chunk.mass;

								//SPIKE BREAKS
								if ((mass > 0.75f && impaleChance > 0.9f) || (mass <= 0.75f && mass > 0.5f && impaleChance > 0.7f))
								{
									Break();
									return;
								}
								//CREATURE IMPALED
								else if (impaledChunk == null)
								{
									Impaled(creature, chunk);
									return;
								}
							}
						}
					}
				}
			}
		}

		if (snap)
		{
			breakSize = -45f;
			tipWidth = 2f;
			broken = true;
		}

		//Something is impaled on the spike
		if (impaledCreature != null && impaledChunk != null)
		{
			Vector2 impalePos = Vector2.Lerp(impalePoint.Center, poPos + handlePos, slide);
			impaledChunk.setPos = impalePos;

			if (Vector2.Distance(impaledChunk.pos, impalePos) > 25f)
			{
				impaledChunk = null;
				impaledCreature = null;
				slide = 0f;
				return;
			}

			//Impaled creature isn't dead
			if (!impaledCreature.dead)
			{
				impaledCreature.stun = 10;
				damageOverTime += (0.025f + impaledCreature.Template.baseDamageResistance * 0.1f) * 0.025f;

				if (damageOverTime > impaledCreature.Template.baseDamageResistance)
				{
					impaledCreature.Die();
				}
			}

			//Upwards spike
			if (angle is > 120f and <= 180f or < (-120f) and >= (-180f))
			{
				slide += Mathf.Lerp(0.35f, 0.01f, Mathf.InverseLerp(0f, 0.35f, slide)) * 0.025f;
				slide = Mathf.Clamp(slide, 0f, 0.35f);
			}

			//Downwards spike
			if (angle is <= 0f and > (-60f) or >= 0f and < 60f)
			{
				slide -= Mathf.Lerp(0.04f, 0.25f, Mathf.InverseLerp(0f, 0.35f, slide)) * 0.025f;

				if (slide < -0.03f)
				{
					for (int i = 0; i < impaledCreature.bodyChunks.Length; i++)
					{
						impaledCreature.bodyChunks[i].lastLastPos = impaledCreature.bodyChunks[i].pos;
						impaledCreature.bodyChunks[i].lastPos = impaledCreature.bodyChunks[i].pos;
						impaledCreature.bodyChunks[i].vel = new Vector2(0f, 0f);
					}

					impaledChunk = null;
					impaledCreature = null;
					slide = 0f;
					return;
				}
				slide = Mathf.Clamp(slide, -0.1f, 0.35f);
			}

			//Forcibly break the spike if the impaled creature is being pulled off of it by some force
			if (slide > 0.15f && Vector2.Distance(impaledChunk.pos, impaledChunk.lastPos) > 2f)
			{
				Break();
			}
		}
	}

	public void Impaled(Creature creature, BodyChunk chunk)
	{
		damageOverTime = Mathf.Lerp(0.65f, 2f,
										Mathf.InverseLerp(10f, 100f, Vector2.Distance(chunk.pos, chunk.lastLastPos)));
		impaledCreature = creature;
		impaledChunk = chunk;

		if (impaledCreature.stun == 0)
		{
			room.PlaySound(SoundID.Spear_Stick_In_Creature, impaledChunk);
		}

		for (int s = 0; s < 6; s++)
		{
			Spark spark = new(chunk.pos, chunk.vel, new Color(0.7f, 0.7f, 0.7f), null, 10, 50);
			room.AddObject(spark);
		}

		if (startAngle is <= 0f and > (-60f) or >= 0f and < 60f)
		{
			slide = 0.07f;
		}

		chunk.vel = new Vector2();
	}

	public void Break()
	{
		var data = po.data as PlacedObject.ResizableObjectData;
		Vector2 poPos = po.pos;
		Vector2 handlePos = data.handlePos;
		if (impaledChunk != null)
		{
			for (int b = 0; b < impaledCreature.bodyChunks.Length; b++)
			{
				impaledCreature.bodyChunks[b].vel = new Vector2();
			}
			impaledChunk = null;
			impaledCreature = null;
		}
		snap = true;

		if (room.game.IsStorySession)
		{
			room.game.rainWorld.progression.currentSaveState.ReportConsumedItem(room.world, false, room.abstractRoom.index, index, 2);
		}

		room.PlaySound(SoundID.Leviathan_Crush_Non_Organic_Object, poPos, 1.5f, 0.8f);
		room.PlaySound(SoundID.Fire_Spear_Pop, poPos, 0.5f, 0.8f);

		Vector2 vector2 = poPos + Custom.DirVec(poPos + handlePos, poPos) * breakSize;

		for (int s = 0; s < 6; s++)
		{
			room.AddObject(new ExplosiveSpear.SpearFragment(vector2, Custom.RNV() * 10f));
			room.AddObject(new Spark(vector2, Custom.RNV() * 5f, color, null, 20, 60));
		}

		room.AddObject(new ExplosionSpikes(room, vector2, 4, 4f, 7f, 2f, 50f, new Color(1f, 1f, 1f)));

		//50% chance to spawn a spike tip
		if (UnityEngine.Random.value > 0.5f)
		{
			SpikeAbstractTip apo = new(room.world, null, wc, room.game.GetNewID());
			room.abstractRoom.AddEntity(apo);
			apo.RealizeInRoom();
		}
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		color = rCam.currentPalette.blackColor;
		tipColor = rCam.currentPalette.fogColor;

		sLeaser.sprites = new FSprite[2];
		sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(stickPositions.Length, false, true);
		sLeaser.sprites[1] = new FSprite("Futile_White", true)
		{
			scale = 1f,
			alpha = 0f
		};
		AddToContainer(sLeaser, rCam, null);
	}
	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		Vector2 a = po.pos;
		Vector2 vector = po.pos + (po.data as PlacedObject.ResizableObjectData).handlePos;
		Vector2 vector2 = a + Custom.DirVec(vector, po.pos) * breakSize;
		Vector2 a2 = vector + Custom.DirVec(vector2, vector) * 5f;
		float num = 1f;
		float baseWidth = Mathf.Lerp(2.7f, 3.9f, Mathf.InverseLerp(30f, 250f, Vector2.Distance(a, vector)));
		for (int j = 0; j < stickPositions.Length; j++)
		{
			float t = j / (float)(stickPositions.Length - 1);
			float num2 = Mathf.Lerp(baseWidth + Mathf.Min((po.data as PlacedObject.ResizableObjectData).handlePos.magnitude / 190f, 3f), tipWidth, t);
			Vector2 vector3 = Vector2.Lerp(vector, vector2, t) + stickPositions[j] * Mathf.Lerp(num2 * 0.6f, 1f, t);
			Vector2 normalized = (a2 - vector3).normalized;
			Vector2 a3 = Custom.PerpendicularVector(normalized);
			float d = Vector2.Distance(a2, vector3) / 5f;
			(sLeaser.sprites[0] as TriangleMesh).MoveVertice(j * 4, a2 - normalized * d - a3 * (num2 + num) * varA - camPos);
			(sLeaser.sprites[0] as TriangleMesh).MoveVertice(j * 4 + 1, a2 - normalized * d + a3 * (num2 + num) * varB - camPos);
			(sLeaser.sprites[0] as TriangleMesh).MoveVertice(j * 4 + 2, vector3 + normalized * d - a3 * num2 - camPos);
			(sLeaser.sprites[0] as TriangleMesh).MoveVertice(j * 4 + 3, vector3 + normalized * d + a3 * num2 - camPos);
			a2 = vector3;
			num = num2;
		}
		sLeaser.sprites[1].x = impalePoint.Center.x - camPos.x;
		sLeaser.sprites[1].y = impalePoint.Center.y - camPos.y;
		if (broken && !updateFade)
		{
			ApplyPalette(sLeaser, rCam, rCam.currentPalette);
			updateFade = true;
		}
		if (updateTipColor)
		{
			ApplyPalette(sLeaser, rCam, rCam.currentPalette);
			updateTipColor = false;
		}
		if (slatedForDeletetion || room != rCam.room)
		{
			sLeaser.CleanSpritesAndRemove();
		}
	}
	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		newContatiner ??= rCam.ReturnFContainer("Background");
		for (int i = 0; i < sLeaser.sprites.Length; i++)
		{
			sLeaser.sprites[i].RemoveFromContainer();
		}
		newContatiner.AddChild(sLeaser.sprites[0]);
		rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[1]);
	}
	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		color = palette.blackColor;
		sLeaser.sprites[0].color = palette.blackColor;
		for (int i = 0; i < (sLeaser.sprites[0] as TriangleMesh).verticeColors.Length; i++)
		{
			float fade;
			if (broken)
			{
				fade = Mathf.InverseLerp((sLeaser.sprites[0] as TriangleMesh).verticeColors.Length / 2, (sLeaser.sprites[0] as TriangleMesh).verticeColors.Length * 2, i);
				(sLeaser.sprites[0] as TriangleMesh).verticeColors[i] = Color.Lerp(color, Color.Lerp(color, tipColor, 0.5f), fade);
			}
			else
			{
				fade = Mathf.InverseLerp((sLeaser.sprites[0] as TriangleMesh).verticeColors.Length / 2, (sLeaser.sprites[0] as TriangleMesh).verticeColors.Length, i);
				(sLeaser.sprites[0] as TriangleMesh).verticeColors[i] = Color.Lerp(color, Color.Lerp(color, tipColor, 0.6f), fade);
			}
		}
		sLeaser.sprites[1].color = new Color(1f, 1f, 1f);
	}

    public void Explosion(Explosion explosion)
    {
		Vector2 tip = po.pos;
		Vector2 bottom = po.pos + (po.data as PlacedObject.ResizableObjectData).handlePos;
		if (explosion != null && Custom.DistLess((tip + bottom) / 2, explosion.pos, explosion.rad * 2f))
		{
			Break();
		}
    }
}

public class SpikeAbstractTip : AbstractSpear
{
	public SpikeAbstractTip(World world, Spear realizedObject, WorldCoordinate pos, EntityID ID) : base(world, realizedObject, pos, ID, false)
	{
		type = _Enums.SpikeTip;
	}

	public bool StuckInWall => stuckInWallCycles != 0;

	public override void Realize()
	{
		base.Realize();
		if (type == _Enums.SpikeTip)
		{
			realizedObject ??= new SpikeTip(this, world);
		}
		for (int i = 0; i < stuckObjects.Count; i++)
		{
			if (stuckObjects[i].A.realizedObject == null && stuckObjects[i].A != this)
			{
				stuckObjects[i].A.Realize();
			}
			if (stuckObjects[i].B.realizedObject == null && stuckObjects[i].B != this)
			{
				stuckObjects[i].B.Realize();
			}
		}
	}
}

public static class SpikeSetup
{
	public static void Apply()
	{
		On.RainWorld.Start += RainWorld_Start;
		On.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;
		On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
	}

	private static string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
	{
		if (itemType == _Enums.SpikeTip)
			return "spikeTip";
		else
			return orig(itemType, intData);
	}

	private static AbstractPhysicalObject SaveState_AbstractPhysicalObjectFromString(On.SaveState.orig_AbstractPhysicalObjectFromString orig, World world, string objString)
	{
		AbstractPhysicalObject? apo = orig(world, objString);
		if (apo is not null && apo.type == _Enums.SpikeTip)
		{
			try
			{
				string[] data = Regex.Split(objString, "<oA>");
				apo = new SpikeAbstractTip(world, null!, apo.pos, apo.ID);
				(apo as SpikeAbstractTip).stuckInWallCycles = int.Parse(data[3], NumberStyles.Any, CultureInfo.InvariantCulture);

			}
			catch (Exception e)
			{
				LogError(new Exception("Failed to load Spike Tip", e));
				apo = null;
			}
		}
		return apo;
	}

	private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
	{
		orig(self);
		CustomAtlases.FetchAtlas("spikeTip");
	}
}

public class SpikeTip(AbstractPhysicalObject abstractPhysicalObject, World world) : Spear(abstractPhysicalObject, world)
{
	public Color tipColor;
	public bool embedded = false; //Tracks whether the spear is stuck in a creature so that it can move sprite layers
	public bool updateSpriteLayer = false;
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[2];
		sLeaser.sprites[0] = new FSprite("spikeTip", true);
		sLeaser.sprites[1] = new FSprite("spikeTipCol", true)
		{
			alpha = 0.4f
		};
		AddToContainer(sLeaser, rCam, null);
	}
	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		color = palette.blackColor;
		tipColor = Color.Lerp(palette.blackColor, palette.fogColor, 0.7f);

		sLeaser.sprites[0].color = color;
		sLeaser.sprites[1].color = tipColor;
	}

	public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		base.AddToContainer(sLeaser, rCam, newContatiner);
		if (embedded)
		{
			rCam.ReturnFContainer("Background").AddChild(sLeaser.sprites[0]);
			rCam.ReturnFContainer("Background").AddChild(sLeaser.sprites[1]);
		}
		else
		{
			rCam.ReturnFContainer("Items").AddChild(sLeaser.sprites[0]);
			rCam.ReturnFContainer("Items").AddChild(sLeaser.sprites[1]);
		}
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		Vector2 a = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
		if (vibrate > 0)
		{
			a += Custom.DegToVec(UnityEngine.Random.value * 360f) * 2f * UnityEngine.Random.value;
		}
		Vector3 v = Vector3.Slerp(lastRotation, rotation, timeStacker);
		for (int i = 0; i < sLeaser.sprites.Length; i++)
		{
			sLeaser.sprites[i].x = a.x - camPos.x;
			sLeaser.sprites[i].y = a.y - camPos.y;
			sLeaser.sprites[i].anchorY = Mathf.Lerp(!lastPivotAtTip ? 0.5f : 0.85f, !pivotAtTip ? 0.5f : 0.85f, timeStacker);
			sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), v);
		}
		if (blink > 0 && UnityEngine.Random.value < 0.5f)
		{
			sLeaser.sprites[0].color = blinkColor;
			sLeaser.sprites[1].color = blinkColor;
		}
		else
		{
			sLeaser.sprites[0].color = color;
			sLeaser.sprites[1].color = tipColor;
		}
		if (slatedForDeletetion || room != rCam.room)
		{
			sLeaser.CleanSpritesAndRemove();
		}
		if (updateSpriteLayer)
		{
			sLeaser.sprites[0].RemoveFromContainer();
			sLeaser.sprites[1].RemoveFromContainer();

			AddToContainer(sLeaser, rCam, null);
			updateSpriteLayer = false;
		}
	}

	public override void ChangeMode(Mode newMode)
	{
		base.ChangeMode(newMode);
		embedded = newMode == Mode.StuckInCreature;
		updateSpriteLayer = true;
	}

	public override void PlaceInRoom(Room placeRoom)
	{
		base.PlaceInRoom(placeRoom);
		if ((abstractPhysicalObject as SpikeAbstractTip).StuckInWall)
		{
			stuckInWall = new Vector2?(placeRoom.MiddleOfTile(abstractPhysicalObject.pos.Tile));
			ChangeMode(Mode.StuckInWall);
		}
	}
}



