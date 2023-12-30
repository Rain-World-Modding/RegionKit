using System.Runtime.Serialization;
using System.Threading;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using Debug = UnityEngine.Debug;

namespace RegionKit.Modules.Objects;

public class RegionLoaderData : ManagedData
{
    [Vector2Field("size", 100, 100, Vector2Field.VectorReprType.rect)]
    public Vector2 size;

    [StringField("region", "", "Region")]
    public string region;

    [IntegerField("karmaRequirement", 0, 9, 0, ManagedFieldWithPanel.ControlType.slider, "Karma Requirement")]
    public int karmaRequirement;

    public RegionLoaderData(PlacedObject owner) : base(owner, null)
    {
    }
}

public class RegionLoader : UpdatableAndDeletable
{
	private static readonly OverWorld.SpecialWarpType RegionLoaderSpecialWarp = new(nameof(RegionLoaderSpecialWarp));
	private static readonly Dictionary<string, string[]> RoomCache = new();

	private static Thread? AsyncCreatingWorldThread;
	private static volatile bool AsyncCreatingWorldDone;

	private readonly PlacedObject placedObject;
	public readonly RegionLoaderData data;
	
	private static WorldLoader? worldLoader;

	private Vector2 pos => placedObject.pos;
	
	#region hooks
    public static void Apply()
    {
        On.ShortcutGraphics.GenerateSprites += ShortcutGraphics_GenerateSprites;

        IL.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
        IL.RainWorldGame.Update += RainWorldGame_Update;
        IL.WorldLoader.Update += WorldLoader_Update;
    }

    public static void Undo()
    {
	    On.ShortcutGraphics.GenerateSprites -= ShortcutGraphics_GenerateSprites;
	    
	    IL.OverWorld.WorldLoaded -= OverWorld_WorldLoaded;
	    IL.RainWorldGame.Update -= RainWorldGame_Update;
	    IL.WorldLoader.Update -= WorldLoader_Update;
    }

    private static void WorldLoader_Update(ILContext il)
    {
	    var cursor = new ILCursor(il);

	    ILLabel skipLabel = null;
	    var continueLabel = cursor.DefineLabel();

	    cursor.GotoNext(MoveType.Before,
		    i => i.MatchBrfalse(out skipLabel),
		    i => i.MatchLdarg(0),
		    i => i.MatchCallOrCallvirt<WorldLoader>(nameof(WorldLoader.CreatingWorld)));
	    
	    cursor.GotoNext(MoveType.Before,
		    i => i.MatchLdarg(0),
		    i => i.MatchCallOrCallvirt<WorldLoader>(nameof(WorldLoader.CreatingWorld)));

	    cursor.MoveAfterLabels();
	    cursor.Emit(OpCodes.Ldarg_0);
	    cursor.EmitDelegate((WorldLoader self) =>
	    {
		    if (AsyncCreatingWorldThread == null)
		    {
			    AsyncCreatingWorldThread = new Thread(() =>
			    {
				    Debug.unityLogger.logEnabled = false;
				    self.CreatingWorld();
				    AsyncCreatingWorldDone = true;
			    });
			    
			    AsyncCreatingWorldThread.Start();
		    }
		    else if (AsyncCreatingWorldDone)
		    {
			    AsyncCreatingWorldDone = false;
			    AsyncCreatingWorldThread = null;
			    return true;
		    }

		    return false;
	    });

	    cursor.Emit(OpCodes.Brtrue, continueLabel);
	    cursor.Emit(OpCodes.Br, skipLabel);
	    
	    cursor.GotoNext(MoveType.After,
		    i => i.MatchLdarg(0),
		    i => i.MatchCallOrCallvirt<WorldLoader>(nameof(WorldLoader.CreatingWorld)));

	    cursor.MarkLabel(continueLabel);
    }

    private static void ShortcutGraphics_GenerateSprites(On.ShortcutGraphics.orig_GenerateSprites orig, ShortcutGraphics self)
    {
        var gateIndex = self.room.abstractRoom.gateIndex;
        
        if (self.room.updateList.Any(x => x is RegionLoader))
        {
            self.room.abstractRoom.gateIndex = 0;
        }
        
        orig(self);

        self.room.abstractRoom.gateIndex = gateIndex;
    }

    private static void OverWorld_WorldLoaded(ILContext il)
    {
        var cursor = new ILCursor(il);
        var label = cursor.DefineLabel();
        
        var match = new Func<Instruction, bool>[]
        {
            i => i.MatchLdloc(out _),
            i => i.MatchCallOrCallvirt<World>("get_regionState"),
            i => i.MatchLdfld<RegionState>(nameof(RegionState.gatesPassedThrough)),
            i => i.MatchLdloc(out _),
            i => i.MatchLdloc(out _),
            i => i.MatchLdfld<AbstractRoom>(nameof(AbstractRoom.name)),
            i => i.MatchCallOrCallvirt<World>(nameof(World.GetAbstractRoom)),
            i => i.MatchLdfld<AbstractRoom>(nameof(AbstractRoom.gateIndex)),
            i => i.MatchLdcI4(out _),
            i => i.MatchStelemI1()
        };
        
        cursor.GotoNext(MoveType.Before, match);

        cursor.MoveAfterLabels();
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((OverWorld ow) => ow.currentSpecialWarp == RegionLoaderSpecialWarp);
        cursor.Emit(OpCodes.Brtrue, label);

        cursor.GotoNext(MoveType.After, match);
        cursor.GotoNext(MoveType.After, match);

        cursor.MarkLabel(label);
    }
    
    private static void RainWorldGame_Update(ILContext il)
    {
        var cursor = new ILCursor(il);
        var label = cursor.DefineLabel();

        var loc = -1;
        cursor.GotoNext(MoveType.Before,
            i => i.MatchLdarg(0),
            i => i.MatchCallOrCallvirt<RainWorldGame>("get_world"),
            i => i.MatchLdfld<World>(nameof(World.activeRooms)),
            i => i.MatchLdloc(out loc),
            i => i.MatchCallOrCallvirt(out _),
            i => i.MatchCallOrCallvirt<Room>(nameof(Room.Update)));
        
        cursor.MoveAfterLabels();
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc, loc);
        cursor.EmitDelegate((RainWorldGame game, int i) => i < game.world.activeRooms.Count);
        cursor.Emit(OpCodes.Brfalse, label);

        cursor.GotoNext(MoveType.After,
            i => i.MatchLdarg(0),
            i => i.MatchCallOrCallvirt<RainWorldGame>("get_world"),
            i => i.MatchLdfld<World>(nameof(World.activeRooms)),
            i => i.MatchLdloc(out _),
            i => i.MatchCallOrCallvirt(out _),
            i => i.MatchCallOrCallvirt<Room>(nameof(Room.Update)));

        cursor.MoveAfterLabels();
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc, loc);
        cursor.EmitDelegate((RainWorldGame game, int i) => i < game.world.activeRooms.Count);
        cursor.Emit(OpCodes.Brfalse, label);

        cursor.GotoNext(MoveType.After,
            i => i.MatchLdarg(0),
            i => i.MatchCallOrCallvirt<RainWorldGame>("get_world"),
            i => i.MatchLdfld<World>(nameof(World.activeRooms)),
            i => i.MatchLdloc(out _),
            i => i.MatchCallOrCallvirt(out _),
            i => i.MatchCallOrCallvirt<Room>(nameof(Room.PausedUpdate)));

        cursor.MarkLabel(label);
    }
    #endregion
	
	public RegionLoader(PlacedObject placedObject, Room room)
	{
		this.placedObject = placedObject;
		data = (RegionLoaderData)placedObject.data;
	}
	
    public override void Update(bool eu)
    {
        base.Update(eu);

        WorldLoaderUpdate();

        if (worldLoader != null || string.IsNullOrEmpty(data.region) || room.world.region.name == data.region) return;

        if (data.karmaRequirement > 0 && room.game.session is StoryGameSession session && session.saveState.deathPersistentSaveData.karma < data.karmaRequirement) return;

        //-- TODO: some kind of indicator for this?
        if (room.game.AlivePlayers.Count != room.PlayersInRoom.Count) return;

        var startPos = pos;
        if (data.size.x < 0)
        {
            data.size.x = -data.size.x;
            startPos.x -= data.size.x;
        }
        if (data.size.y < 0)
        {
            data.size.y = -data.size.y;
            startPos.y -= data.size.y;
        }

        var affectedRect = new Rect(startPos, data.size);
        foreach (var player in room.PlayersInRoom)
        {
            if (player != null && player.room == room && !player.isNPC && player.playerState.playerNumber == 0 && affectedRect.Contains(player.mainBodyChunk.pos))
            {
                SwitchRegions();
                break;
            }
        }
    }

    private void WorldLoaderUpdate()
    {
	    if (worldLoader == null) return;

	    Debug.unityLogger.logEnabled = false;
	    worldLoader.Update();
	    Debug.unityLogger.logEnabled = true;
	    
	    if (worldLoader.Finished)
	    {
		    room.game.overWorld.worldLoader = worldLoader;
		    worldLoader = null;
		    room.game.overWorld.reportBackToGate = ProxyGate.Create(room);
		    room.game.overWorld.WorldLoaded();

		    UnlockShortcuts();

		    var helper = room.updateList.FirstOrDefault(x => x is ShortcutHelper);
		    if (helper != null)
		    {
			    room.RemoveObject(helper);
			    helper.Destroy();
		    }

		    room.AddObject(new ShortcutHelper(room));
	    }
    }

    private void SwitchRegions()
    {
	    room.game.manager.musicPlayer?.GateEvent();
	    LockShortcuts();

	    var ow = room.game.overWorld;

        var oldRegion = room.world.region.name;
        var newRegion = data.region;

        #region base game stuff
		if (newRegion == "GW")
		{
			room.game.session.creatureCommunities.scavengerShyness = 0f;
		}
		if (ModManager.MSC)
		{
			var region = ow.GetRegion(oldRegion);
			var listTracker = (WinState.ListTracker)room.game.GetStorySession.saveState.deathPersistentSaveData.winState.GetTracker(MoreSlugcatsEnums.EndgameID.Nomad, addIfMissing: true);
			if (!listTracker.GoalAlreadyFullfilled)
			{
				if (RainWorld.ShowLogs)
				{
					Debug.Log("Journey list before gate: " + listTracker.myList.Count);
				}
				if (listTracker.myLastList.Count > listTracker.myList.Count)
				{
					if (RainWorld.ShowLogs)
					{
						Debug.Log("Stale journey max progress cleared");
					}
					listTracker.myLastList.Clear();
				}
				if (listTracker.myList.Count == 0 || listTracker.myList[listTracker.myList.Count - 1] != ow.GetRegion(newRegion).regionNumber)
				{
					if (RainWorld.ShowLogs)
					{
						Debug.Log("Journey progress updated with " + region.regionNumber);
					}
					listTracker.myList.Add(region.regionNumber);
				}
				else if (RainWorld.ShowLogs)
				{
					Debug.Log("Journey is backtracking " + listTracker.myList[listTracker.myList.Count - 1]);
				}
				if (RainWorld.ShowLogs)
				{
					Debug.Log("Journey list: " + listTracker.myList.Count);
				}
				if (RainWorld.ShowLogs)
				{
					Debug.Log("Old Journey list: " + listTracker.myLastList.Count);
				}
			}
		}
		#endregion

		ow.worldLoader = null;
		ow.currentSpecialWarp = RegionLoaderSpecialWarp;
		worldLoader = new WorldLoader(room.game, ow.PlayerCharacterNumber, false, newRegion, ow.GetRegion(newRegion), room.game.setupValues);
        worldLoader.NextActivity();
    }

    private void LockShortcuts() => room.lockedShortcuts.AddRange(room.shortcutsIndex);

    private void UnlockShortcuts() => room.lockedShortcuts.Clear();

    public class ProxyGate : RegionGate
	{
		public ProxyGate(Room room) : base(room) { }
    
		public static ProxyGate Create(Room room)
		{
			var proxyGate = (ProxyGate)FormatterServices.GetUninitializedObject(typeof(ProxyGate));
			proxyGate.room = room;

			return proxyGate;
		}
	}
}
