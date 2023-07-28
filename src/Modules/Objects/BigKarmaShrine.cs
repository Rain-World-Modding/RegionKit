using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Objects;

internal class BigKarmaShrine : UpdatableAndDeletable
{
	#region superslow hooks
	public static void Apply()
	{
		IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
		On.HUD.KarmaMeter.Update += KarmaMeter_Update;
		On.HUD.FadeCircle.Update += FadeCircle_Update;
		On.HUD.FoodMeter.Update += FoodMeter_Update;
	}
	public static void Undo()
	{
		IL.RainWorldGame.RawUpdate -= RainWorldGame_RawUpdate;
		On.HUD.KarmaMeter.Update -= KarmaMeter_Update;
		On.HUD.FadeCircle.Update -= FadeCircle_Update;
		On.HUD.FoodMeter.Update -= FoodMeter_Update;
	}

	private static void FoodMeter_Update(On.HUD.FoodMeter.orig_Update orig, HUD.FoodMeter self)
	{
		var lastFade = self.fade;
		var lastPos = self.pos;
		var lastShowSurvLim = self.showSurvLim;
		orig(self);
		if (exSlowdown)
		{
			for (int i = 0; i < 8; i++)
			{ orig(self); }

			self.lastFade = lastFade;
			self.lastPos = lastPos;
			self.lastShowSurvLim = lastShowSurvLim;
		}
	}

	private static void FadeCircle_Update(On.HUD.FadeCircle.orig_Update orig, HUD.FadeCircle self)
	{
		var lastRad = self.rad;
		orig(self);
		if (exSlowdown)
		{
			for (int i = 0; i < 12; i++)
			{ orig(self); }

			self.circle.lastRad = lastRad;
		}
	}

	private static void KarmaMeter_Update(On.HUD.KarmaMeter.orig_Update orig, HUD.KarmaMeter self)
	{
		var lastPos = self.pos;
		var lastFade = self.fade;
		var lastRad = self.rad;
		var lastGlowyFac = self.glowyFac;
		var lastReinforcementCycle = self.reinforcementCycle;

		orig(self);

		if (exSlowdown)
		{
			for (int i = 0; i < 12; i++)
			{ orig(self); }

			self.lastPos = lastPos;
			self.lastFade = lastFade;
			self.lastRad = lastRad;
			self.lastGlowyFac = lastGlowyFac;
			self.lastReinforcementCycle = lastReinforcementCycle;
		}
	}

	private static void RainWorldGame_RawUpdate(MonoMod.Cil.ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdloc(out _),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<MainLoopProcess>(nameof(MainLoopProcess.framesPerSecond)),
			x => x.MatchConvR4(),
			x => x.MatchLdarg(0),
			x => x.MatchCall<RainWorldGame>("get_cameras"),
			x => x.MatchLdcI4(out _),
			x => x.MatchLdelemRef(),
			x => x.MatchLdfld<RoomCamera>(nameof(RoomCamera.ghostMode)),
			x => x.MatchLdcR4(out _),
			x => x.MatchMul(),
			x => x.MatchSub(),
			x => x.MatchCall(typeof(Math), nameof(Math.Min))
			))
		{
			c.EmitDelegate(ChangeFrameRate);
		}
		else
		{ __logger.LogWarning("il hook to RawUpdate failed"); }
	}

	public static float ChangeFrameRate(float originalFPS)
	{
		if (!exSlowdown) return originalFPS;
		return Mathf.Lerp(1f, 40f, (float)Math.Pow(originalFPS / 40f, 2.8f));
	}
	#endregion

	#region main object
	public enum direction
	{
		Any,
		Left,
		Right,
		Up,
		Down
	}

	public static bool exSlowdown = false;
	public static float exSlowdownAmount;

	public PlacedObject pObj;
	public ManagedData Data;
	public bool addKarma = true;
	public MarkSprite? sprite;

	public RoomSettings.RoomEffect? meltEffect;
	public float effectAdd;
	public float effectInitLevel;

	public BigKarmaShrine(PlacedObject pObj, Room room)
	{
		this.pObj = pObj;
		Data = (pObj.data as ManagedData)!;
		for (int i = 0; i < room.roomSettings.effects.Count; i++)
		{
			if (room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.VoidMelt)
			{
				meltEffect = room.roomSettings.effects[i];
				effectInitLevel = meltEffect.amount;
				break;
			}
		}

		if (useSprite)
		{
			sprite = new MarkSprite(this);
			room.AddObject(sprite);
		}
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		SpriteUpdate();
		if (Activate())
		{
			StoryGameSession session = (room.game.session as StoryGameSession)!;
			if (this.SetKarma != -1)
			{ session.saveState.deathPersistentSaveData.karma = this.SetKarma; }

			if (this.SetKarmaCap != -1)
			{ session.saveState.deathPersistentSaveData.karmaCap = this.SetKarmaCap; }

			room.game.cameras[0].hud.karmaMeter.reinforceAnimation = 1;
			exSlowdown = Data.GetValue<bool>("superslow");
			Debug.Log($"exSlowdown: {exSlowdown}");

			addKarma = false;
			room.PlaySound(SoundID.SB_A14, 0f, 1f, 1f);
			for (int i = 0; i < (exSlowdown ? 60 : 20); i++)
			{
				room.AddObject(new MeltLights.MeltLight(1f, room.RandomPos(), room, RainWorld.GoldRGB));
			}
			effectAdd = 1f;
		}

		effectAdd = Mathf.Max(0f, effectAdd - (exSlowdown ? 0.016f : 0.016666668f));
		if (meltEffect != null && effectAdd != 0f)
		{
			meltEffect.amount = Mathf.Lerp(effectInitLevel, 1f, Custom.SCurve(effectAdd, 0.6f));
			exSlowdownAmount = meltEffect.amount;
		}
		else
		{ exSlowdown = false; }
	}
	public bool PosRequirement(Vector2 pos)
	{
		if (pos.magnitude < this.Radius.magnitude)
		{
			return this.Direction switch
			{
				direction.Any => true,
				direction.Left => pos.x < 0f,
				direction.Right => pos.x > 0f,
				direction.Up => pos.y > 0f,
				direction.Down => pos.y < 0f,
				_ => false
			};
		}
		else
		return false;
	}

	public bool KarmaRequirement(StoryGameSession session, AbstractCreature creature)
	{
		//creature is passed in for the future...
		DeathPersistentSaveData data = session.saveState.deathPersistentSaveData;
		return data.karmaCap >= this.ReqKarmaCap && data.karma >= this.ReqKarma;
	}

	public bool Activate()
	{
		if (!addKarma || room.game.session is not StoryGameSession session || room.game.Players.Count <= 0) return false;
		
		foreach (AbstractCreature player in room.game.Players)
		{
			if (player.realizedCreature == null || player.realizedCreature.room != room) continue;

			if (KarmaRequirement(session, player) && PosRequirement(player.realizedCreature.firstChunk.pos - pObj.pos))
			{
				return true;
			}
		}

		return false;
	}

	public void SpriteUpdate()
	{
		if (sprite != null && !useSprite)
		{
			sprite.RemoveFromRoom();
			sprite = null!;
		}
		else if (sprite == null && useSprite)
		{
			sprite = new MarkSprite(this);
			room.AddObject(sprite);
		}

		if (sprite == null) return;

		sprite.InheritFromOwner(this, out sprite.refreshSprites);
	}


	public int SpriteNumber()
	{
		return (this.SetKarma != -1 ? this.SetKarma : this.SetKarmaCap);
	}

	public int DefaultDepth()
	{
		if (room == null) return 20;
		Room.Tile tile = room.GetTile(pObj.pos);
		if (tile.Solid) return 0;
		if (tile.wallbehind) return 10;
		return 20;
	}

	int ReqKarma => Data.GetValue<int>("reqkarma");
	int ReqKarmaCap => Data.GetValue<int>("reqkarmacap");
	int SetKarma => Data.GetValue<int>("setkarma");
	int SetKarmaCap => Data.GetValue<int>("setkarmacap");
	direction Direction => Data.GetValue<direction>("direction");
	Vector2 Radius => Data.GetValue<Vector2>("radius");
	bool useSprite => Data.GetValue<bool>("sprite");
	#endregion
	internal class MarkSprite : CosmeticSprite
	{
	public MarkSprite(BigKarmaShrine owner)
		{
			pObj = owner.pObj;
			Data = null!;
			InheritFromOwner(owner, out _);
		}
		public MarkSprite(PlacedObject pObj, Room room)
		{
			this.pObj = pObj;
			Data = (pObj.data as ManagedData)!;
			InheritFromPlacedObject(out _);
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[2];
			sLeaser.sprites[0] = new CustomFSprite("BigShrineFrame");
			sLeaser.sprites[1] = new CustomFSprite("BigShrineFrame");
			//sLeaser.sprites[0] = new FSprite("BigShrineFrame", true);
			RefreshSprites(sLeaser);

			foreach (FSprite sprite in sLeaser.sprites)
			{
				sprite.shader = rCam.game.rainWorld.Shaders["CustomDepth"];
			}

			this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Water"));
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			InheritFromPlacedObject(out bool refresh);
			if (refreshSprites || refresh) RefreshSprites(sLeaser);
			refreshSprites = false;

			foreach (FSprite sprite in sLeaser.sprites)
			{
				sprite.SetPosition(pObj.pos - camPos);
			}
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		}

		public void InheritFromOwner(BigKarmaShrine owner, out bool refresh)
		{
			refresh = false;
			if (SetIfDifferent(ref spriteNum, owner.SpriteNumber())) refresh = true;
			if (SetIfDifferent(ref depth, owner.DefaultDepth())) refresh = true;
		}

		public void InheritFromPlacedObject(out bool refresh)
		{
			refresh = false;
			if (Data == null) return;

			if (SetIfDifferent(ref spriteNum, Data.GetValue<int>("spriteindex"))) refresh = true;
			if (SetIfDifferent(ref depth, Data.GetValue<int>("depth"))) refresh = true;
			if (SetIfDifferent(ref topColor, Data.GetValue<Color>("topcolor"))) refresh = true;
			if (SetIfDifferent(ref bottomColor, Data.GetValue<Color>("bottomcolor"))) refresh = true;
			if (SetIfDifferent(ref half, Data.GetValue<Vector2>("radius").magnitude)) refresh = true;
			if (SetIfDifferent(ref useFrame, Data.GetValue<bool>("frame"))) refresh = true;
			if (SetIfDifferent(ref spriteOverrideName!, Data.GetValue<string>("spritename"))) refresh = true;
		}

		public bool SetIfDifferent<T>(ref T one, T two)
		{
			if (one != null && one.Equals(two)) return true;
			if (one == null && two == null) return true;
			one = two; return false;
		}

		public void RefreshSprites(RoomCamera.SpriteLeaser sLeaser)
		{
			sLeaser.sprites[1].isVisible = useFrame;

			string spriteName = "BigShrine" + spriteNum;
			if (!spriteOverrideName.IsNullOrWhiteSpace() && Futile.atlasManager.DoesContainElementWithName(spriteOverrideName))
			{ spriteName = spriteOverrideName; }
			if (Futile.atlasManager.DoesContainElementWithName(spriteName))
			{
				sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName(spriteName);
			}
			foreach (FSprite sprite in sLeaser.sprites)
			{
				sprite.color = topColor;
				sprite.alpha = depthFloat;
				if (sprite is CustomFSprite custom)
				{
					for (int i = 0; i < 4; i++)
					{
						custom.vertices = quads;
						custom.verticeColors[i] = i < 2 ? topColor : bottomColor;
						custom.verticeColors[i].a = custom.alpha;
					}
					sprite._isMeshDirty = (half != 134.5f);
				}
			}
		}
		public PlacedObject pObj;
		public ManagedData Data;

		public bool refreshSprites;

		public bool useFrame = true;

		public string spriteOverrideName = "";

		public int spriteNum = -1;

		public int depth = -1;
		public float depthFloat => 1f - (depth / 30f);

		public Color topColor = new Color(1f, 0.7f, 0.2f);

		public Color bottomColor = new Color(0.6f, 0.46f, 0.14f);

		private float half = 134.5f;
		public Vector2[] quads => new Vector2[] {
				new Vector2(-half, half),
				new Vector2(half, half),
				new Vector2(half, -half),
				new Vector2(-half, -half)
				};
	}
}
