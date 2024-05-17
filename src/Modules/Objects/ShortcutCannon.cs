using System.Runtime.CompilerServices;
using DevInterface;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace RegionKit.Modules.Objects;


internal class ShortcutCannon : UpdatableAndDeletable, INotifyWhenRoomIsReady
{
	public class PostCorrector : UpdatableAndDeletable
	{
		Player player;
		ShortcutCannon cannon;
		int timer = 0;
		float origWaterRetardation = 0;

		public int TimerLength => !player.submerged ? 5 : (int)(Mathf.Pow(cannon.amount * 5, 1.2f) - 4f);

		public PostCorrector(Player player, ShortcutCannon cannon)
		{
			this.player = player;
			this.cannon = cannon;
			origWaterRetardation = player.waterRetardationImmunity;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			timer++;
			if (timer > TimerLength || player.room != room)
			{ Destroy(); player.waterRetardationImmunity = origWaterRetardation; GetPostCorrector(player).Value = null!; return; }

			if (player.submerged)
			{
				player.waterRetardationImmunity = LerpMap(timer, TimerLength / 2, TimerLength, 1f, origWaterRetardation, 1.5f);
			}

			(player.graphicsModule as PlayerGraphics)!.legsDirection = new Vector2(0f, -1f);
			EvenVelocity(player);
		}

		public float JumpBoostMultiplier() => LerpMap(cannon.boostMultiplier(), 0.4f, 2.5f, 0.25f, 0.5f);

		public float SlideFrictionResult(float orig, float oldOrig) => LerpMap(timer, 1, TimerLength, oldOrig, orig); //not used anymore, timer too short to matter
	}

	public static void EvenVelocity(Player player)
	{
		if (player.bodyChunks.Length == 2 && player.bodyChunks[0].vel.magnitude < player.bodyChunks[1].vel.magnitude)
		{
			(player.bodyChunks[0].vel, player.bodyChunks[1].vel) = (player.bodyChunks[1].vel, player.bodyChunks[0].vel);
		}
		
	}

	private static ConditionalWeakTable<Room, List<ShortcutCannon>> _shortcutCannonsInRoom = new();
	private static ConditionalWeakTable<Player, StrongBox<PostCorrector>> _shortcutCannonHelpers = new();

	public static List<ShortcutCannon> GetShortcutCannons(Room room) => _shortcutCannonsInRoom.GetValue(room, _ => new());

	public static StrongBox<PostCorrector> GetPostCorrector(Player p) => _shortcutCannonHelpers.GetValue(p, _ => new());

	public static bool TryGetSuperShortcut(Room room, IntVector2 pos, out ShortcutCannon shortcut)
	{
		//the entire point of this method is so you don't do stuff with a null value
		//so outputting a nullable is very silly
		shortcut = null!;
		foreach (ShortcutCannon ss in GetShortcutCannons(room))
		{
			if (room.GetTilePosition(ss.pObj.pos) == pos)
			{
				shortcut = ss;
				return true;

			}
		}
		return false;
	}

	public static void Apply()
	{
		On.ShortcutGraphics.GenerateSprites += ShortcutGraphics_GenerateSprites;
		On.Player.SpitOutOfShortCut += Player_SpitOutOfShortCut;
		IL.ShortcutHelper.Update += ShortcutHelper_Update;
		IL.Player.UpdateBodyMode += Player_UpdateBodyMode;
	}

	public static void Undo()
	{
		IL.ShortcutHelper.Update -= ShortcutHelper_Update;
		On.Player.SpitOutOfShortCut -= Player_SpitOutOfShortCut;
		On.ShortcutGraphics.GenerateSprites -= ShortcutGraphics_GenerateSprites;
		IL.Player.UpdateBodyMode -= Player_UpdateBodyMode;
	}


	private static void Player_UpdateBodyMode(ILContext il)
	{
		var c = new ILCursor(il);

		//changes the velocity when corridor boosting, 2 matches, 1 for each body chunk
		for (int j = 0; j < 2; j++)
		{
			if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdflda<BodyChunk>(nameof(BodyChunk.vel)),
			x => x.MatchLdflda<Vector2>(nameof(Vector2.y)),
			x => x.MatchDup(),
			x => x.MatchLdindR4(),
			x => x.MatchLdcR4(j == 0 ? 15f : 10f)
			))
			{
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate((float orig, Player self) => GetPostCorrector(self).Value == null ? orig : orig * GetPostCorrector(self).Value.JumpBoostMultiplier());
			}
			else { LogError("ShortcutCannon failed to il match Player.UpdateBodyMode jump boost " + j); }
		}

		//changes the velocity when pushing a direction into a wall, mainly for wall sliding
		//4 different matches for each cardinal direction
		int index = 0;
		for (int j = 0; j < 4; j++)
		{
			if (c.TryGotoNext(MoveType.After,
				x => x.MatchLdloc(out index),
				x => x.MatchLdelemRef(),
				x => x.MatchLdfld<BodyChunk>(nameof(BodyChunk.pos)),
				x => x.MatchCallvirt<Room>(nameof(Room.GetTilePosition)),
				x => x.MatchCallvirt<Room>(nameof(Room.MiddleOfTile)),
				x => x.MatchLdfld(out _),
				x => x.MatchSub(),
				x => x.MatchLdcR4(0.2f),
				x => x.MatchMul(),
				x => x.MatchSub()
				))
			{
				c.Emit(OpCodes.Ldloc, index);
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate((Func<float, int, Player, float>)(j switch
				{
					0 => (float orig, int i, Player self) => GetPostCorrector(self).Value == null ? orig : self.bodyChunks[i].vel.y,
					1 => (float orig, int i, Player self) => GetPostCorrector(self).Value == null ? orig : self.bodyChunks[1 - i].vel.y,
					2 => (float orig, int i, Player self) => GetPostCorrector(self).Value == null ? orig : self.bodyChunks[i].vel.x,
					3 => (float orig, int i, Player self) => GetPostCorrector(self).Value == null ? orig : self.bodyChunks[1 - i].vel.x,
					_ => (float orig, int i, Player self) => orig
				}));
			}
			else { LogError("ShortcutCannon failed to il match Player.UpdateBodyMode friction slide " + j); }
		}
	}

	private static void ShortcutGraphics_GenerateSprites(On.ShortcutGraphics.orig_GenerateSprites orig, ShortcutGraphics self)
	{
		orig(self);
		for (int l = 0; l < self.room.shortcuts.Length; l++)
		{
			if (TryGetSuperShortcut(self.room, self.room.shortcuts[l].StartTile, out ShortcutCannon superShortcut))
			{
				self.entranceSprites[l, 0].element = Futile.atlasManager.GetElementWithName($"Shortcutcannon_Symbol_{superShortcut.amount}flip");

				int endIndex = Array.IndexOf(self.room.shortcutsIndex, self.room.shortcuts[l].DestTile);
				if (endIndex != -1 && self.room.shortcuts[l].shortCutType == ShortcutData.Type.Normal)
				{ self.entranceSprites[endIndex, 0].element = Futile.atlasManager.GetElementWithName($"Shortcutcannon_Symbol_{superShortcut.amount}"); }
			}
		}
	}

	static int countdown = 0;

	private static void ShortcutHelper_Update(ILContext il)
	{
		var c = new ILCursor(il);
		ILLabel label = null!;
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdsfld<Player.AnimationIndex>(nameof(Player.AnimationIndex.DownOnFours)),
			x => x.MatchCall(typeof(ExtEnum<Player.AnimationIndex>).GetMethod("op_Equality")),
			x => x.MatchBrtrue(out label)) && label != null)
		{
			c.MoveAfterLabels();
			c.Emit(OpCodes.Ldarg_0);
			c.Emit(OpCodes.Ldloc_1);
			c.Emit(OpCodes.Ldloc, 4);
			c.EmitDelegate((ShortcutHelper self, Player player, int i) =>
			{
				if (TryGetSuperShortcut(self.room, self.pushers[i].shortCutPos, out ShortcutCannon shortcutCannon))
				{
					CannonLaunch(self, player, self.pushers[i], shortcutCannon);
					return true;
				}
				return false;
			});
			c.Emit(OpCodes.Brtrue, label);
		}
		else { LogError("ShortcutCannon failed to il match ShortcutHelper.Update"); }
	}

	private static void CannonLaunch(ShortcutHelper self, Player player, ShortcutHelper.ShortcutPusher pusher, ShortcutCannon shortcutCannon)
	{
		for (int k = 0; k < player.bodyChunks.Length; k++)
		{
			float chunkDistToEntrance = 10f + player.bodyChunks[k].rad;

			if (Vector2.Distance(player.bodyChunks[k].pos, pusher.pushPos) < chunkDistToEntrance)
			{
				Vector2 add = pusher.pushPos + (IntVector2ToVector2(pusher.shortcutDir) * chunkDistToEntrance) - player.bodyChunks[k].pos;
				add *= shortcutCannon.boostMultiplier();

				//upwards correction
				if (pusher.shortcutDir.x != 0)
				{ add.y = Math.Abs(add.x * 0.15f * self.room.gravity); }
				else { add.x = 0f; }

				player.bodyChunks[k].vel += add;
				EvenVelocity(player);
			}
		}
	}

	private static void Player_SpitOutOfShortCut(On.Player.orig_SpitOutOfShortCut orig, Player self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
	{
		orig(self, pos, newRoom, spitOutAllSticks);
		if (!TryGetSuperShortcut(newRoom, pos, out var shortcutCannon)) return;

		GetPostCorrector(self).Value?.Destroy();
		GetPostCorrector(self).Value = new(self, shortcutCannon);
		newRoom.AddObject(GetPostCorrector(self).Value);

		Vector2 a = IntVector2ToVector2(newRoom.ShorcutEntranceHoleDirection(pos));
		for (int i = 0; i < self.bodyChunks.Length; i++)
		{
			self.bodyChunks[i].HardSetPosition(self.bodyChunks[i].pos + (a * 10));
		}
	}

	public PlacedObject pObj;

	public int prevAmount;

	public int amount => (pObj.data as shortcutCannonData)!.boost;

	public float boostMultiplier()
	{
		return amount switch
		{
			1 => 0.4f,
			2 => 0.735f,
			3 => 0.97f,
			4 => 1.22f,
			5 => 1.58f,
			6 => 1.9f,
			7 => 2.5f,
			_ => 0f,
		};
	}

	public int shortcutIndex = -1;

	public bool IsOnShortcut() => room.GetTile(pObj.pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance;

	public void AssignIndex()
	{
		shortcutIndex = -1;
		if (!IsOnShortcut()) { return; }

		for (int i = 0; i < room.shortcuts.Length; i++)
		{
			if (room.GetTilePosition(pObj.pos) == room.shortcuts[i].StartTile)
			{
				shortcutIndex = i;
				break;
			}
		}
	}

	public void ResetGraphicsAndLocks()
	{
		if (shortcutIndex == -1) return;
		foreach (RoomCamera camera in room.game.cameras)
		{
			if (camera.room == room && !camera.shortcutGraphics.waitingForRoomToGenerateShortcuts)
			{
				//this just resets sprites
				camera.shortcutGraphics.NewRoom();
			}
		}
		room.lockedShortcuts.Remove(room.shortcuts[shortcutIndex].StartTile);
	}

	public void UpdateGraphics()
	{
		if (shortcutIndex == -1) return;
		foreach (RoomCamera camera in room.game.cameras)
		{
			if (camera.room == room)
			{
				camera.shortcutGraphics.entranceSprites[shortcutIndex, 0].element = Futile.atlasManager.GetElementWithName($"Shortcutcannon_Symbol_{amount}flip");

				int endIndex = Array.IndexOf(room.shortcutsIndex, room.shortcuts[shortcutIndex].DestTile);
				if (endIndex != -1 && room.shortcuts[shortcutIndex].shortCutType == ShortcutData.Type.Normal)
				{ camera.shortcutGraphics.entranceSprites[endIndex, 0].element = Futile.atlasManager.GetElementWithName($"Shortcutcannon_Symbol_{amount}"); }
			}
		}
	}

	public ShortcutCannon(PlacedObject pObj, Room room)
	{
		this.pObj = pObj;
		this.room = room;
		prevAmount = amount;
		if (!GetShortcutCannons(room).Contains(this))
		{ GetShortcutCannons(room).Add(this); }
	}

	public void AIMapReady()
	{ }

	public void ShortcutsReady()
	{ Refresh(); }

	public void Refresh()
	{
		if (!IsOnShortcut() && shortcutIndex != -1)
		{
			ResetGraphicsAndLocks();
			shortcutIndex = -1;
		}

		int prevIndex = shortcutIndex;
		AssignIndex();

		if (prevIndex != shortcutIndex || prevAmount != amount)
		{
			ResetGraphicsAndLocks();

			prevAmount = amount;

			if (shortcutIndex != -1)
			{ room.lockedShortcuts.Add(room.GetTilePosition(pObj.pos)); }
		}
	}
}

internal class shortcutCannonData : ManagedData
{
	[IntegerField("boost", 1, 7, 2)]
	public int boost;
	public shortcutCannonData(PlacedObject po) : base(po, new ManagedField[] { })
	{
	}
}

internal class ShortcutCannonRepresentation : ManagedRepresentation
{

	ShortcutCannon? shortcutCannon;

	private int tileSprite = -1;

	public ShortcutCannonRepresentation(PlacedObject.Type placedType, ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
	{
		if (shortcutCannon == null)
		{
			for (int i = 0; i < owner.room.updateList.Count; i++)
			{
				if (owner.room.updateList[i] is ShortcutCannon && (owner.room.updateList[i] as ShortcutCannon)!.pObj == pObj)
				{
					shortcutCannon = (owner.room.updateList[i] as ShortcutCannon);
					break;
				}
			}
		}

		if (shortcutCannon == null)
		{
			shortcutCannon = new ShortcutCannon(pObj, owner.room);
			owner.room.AddObject(shortcutCannon);
		}

		fSprites.Add(new FSprite("pixel", true));
		tileSprite = fSprites.Count - 1;
		fSprites[tileSprite].scale = 20f;
		fSprites[tileSprite].alpha = 0.4f;
		owner.placedObjectsContainer.AddChild(fSprites[tileSprite]);
	}

	public override void Refresh()
	{
		base.Refresh();
		shortcutCannon?.Refresh();

		if (tileSprite > -1)
		{
			fSprites[tileSprite].x = owner.room.MiddleOfTile(pObj.pos).x - owner.room.game.cameras[0].pos.x;
			fSprites[tileSprite].y = owner.room.MiddleOfTile(pObj.pos).y - owner.room.game.cameras[0].pos.y;
		}

		if (shortcutCannon != null && shortcutCannon.IsOnShortcut())
		{ fSprites[tileSprite].color = Color.green; }

		else
		{ fSprites[tileSprite].color = Color.red; }
	}
}
