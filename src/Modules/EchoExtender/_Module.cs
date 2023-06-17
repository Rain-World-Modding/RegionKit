using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RWCustom;
using UnityEngine;
namespace RegionKit.Modules.EchoExtender;
///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Echo Extender")]
public static class _Module
{
	private static SlugcatStats.Name? __slugcatNumber { get; set; }
	/// <summary>
	/// Applies hooks
	/// </summary>
	public static void Enable()
	{

		// Tests for spawn
		On.World.LoadWorld += WorldOnLoadWorld;
		On.GhostWorldPresence.ctor += GhostWorldPresenceOnCtor;
		On.GhostWorldPresence.GetGhostID += GhostWorldPresenceOnGetGhostID;

		// Spawn and customization
		On.Room.Loaded += RoomOnLoaded;
		On.Ghost.ctor += GhostOnCtor;
		On.Ghost.StartConversation += GhostOnStartConversation;
		On.GhostConversation.AddEvents += GhostConversationOnAddEvents;
		On.GhostWorldPresence.SpawnGhost += GhostWorldPresenceOnSpawnGhost;
		On.GhostWorldPresence.GhostMode_AbstractRoom_Vector2 += GhostWorldPresenceOnGhostMode;

		// Save stuff
		On.DeathPersistentSaveData.ctor += DeathPersistentSaveDataOnCtor;
		On.StoryGameSession.ctor += StoryGameSessionOnCtor;
	}
	/// <summary>
	/// Undoes hooks
	/// </summary>
	public static void Disable()
	{
		On.GhostWorldPresence.ctor -= GhostWorldPresenceOnCtor;
		On.GhostWorldPresence.GetGhostID -= GhostWorldPresenceOnGetGhostID;

		// Spawn and customization
		On.Room.Loaded -= RoomOnLoaded;
		On.Ghost.ctor -= GhostOnCtor;
		On.Ghost.StartConversation -= GhostOnStartConversation;
		On.GhostConversation.AddEvents -= GhostConversationOnAddEvents;
		On.GhostWorldPresence.SpawnGhost -= GhostWorldPresenceOnSpawnGhost;
		On.GhostWorldPresence.GhostMode_AbstractRoom_Vector2 -= GhostWorldPresenceOnGhostMode;

		// Save stuff
		On.DeathPersistentSaveData.ctor -= DeathPersistentSaveDataOnCtor;
		On.StoryGameSession.ctor -= StoryGameSessionOnCtor;
	}


	private static void StoryGameSessionOnCtor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name savestatenumber, RainWorldGame game)
	{
		if (!EchoSettings.Default._initialized) EchoSettings.InitDefault();
		__logger.LogInfo("[Echo Extender] Loading Echoes from Region Mods...");
		EchoParser.LoadAllRegions(savestatenumber);
		orig(self, savestatenumber, game);
	}

	private static void WorldOnLoadWorld(On.World.orig_LoadWorld orig, World self, SlugcatStats.Name slugcatnumber, List<AbstractRoom> abstractroomslist, int[] swarmrooms, int[] shelters, int[] gates)
	{
		__slugcatNumber = slugcatnumber;
		orig(self, slugcatnumber, abstractroomslist, swarmrooms, shelters, gates);
	}
	private static float GhostWorldPresenceOnGhostMode(On.GhostWorldPresence.orig_GhostMode_AbstractRoom_Vector2 orig, GhostWorldPresence self, AbstractRoom testRoom, Vector2 worldPos)
	{
		var result = orig(self, testRoom, worldPos);
		if (!EchoParser.__echoSettings.TryGetValue(self.ghostID, out var settings)) return result;
		if (testRoom.index == self.ghostRoom.index) return 1f;
		var echoEffectLimit = settings.GetRadius(__slugcatNumber ?? SlugcatStats.Name.White) * 1000f; //I think 1 screen is like a 1000 so I'm going with that
		Vector2 globalDistance = Custom.RestrictInRect(worldPos, FloatRect.MakeFromVector2(self.world.RoomToWorldPos(new Vector2(), self.ghostRoom.index), self.world.RoomToWorldPos(self.ghostRoom.size.ToVector2() * 20f, self.ghostRoom.index)));
		if (!Custom.DistLess(worldPos, globalDistance, echoEffectLimit)) return 0;
		var someValue = self.DegreesOfSeparation(testRoom); //No clue what this number does
		return someValue == -1
			? 0.0f
			: (float)(Mathf.Pow(Mathf.InverseLerp(echoEffectLimit, echoEffectLimit / 8f, Vector2.Distance(worldPos, globalDistance)), 2f) * (double)Custom.LerpMap(someValue, 1f, 3f, 0.6f, 0.15f) * (testRoom.layer != self.ghostRoom.layer ? 0.600000023841858 : 1.0));
	}

	private static void RoomOnLoaded(On.Room.orig_Loaded orig, Room self)
	{
		// ReSharper disable once InconsistentNaming
		PlacedObject? EEGhostSpot = null;
		if (self.game != null)
		{ // Actual ingame loading
			EEGhostSpot = self.roomSettings.placedObjects.FirstOrDefault((v) => v.type == _Enums.EEGhostSpot && v.active);
			if (EEGhostSpot != null) EEGhostSpot.type = PlacedObject.Type.GhostSpot; // Temporary switcheroo to trigger vanilla code that handles ghosts
		}

		orig(self);
		// Unswitcheroo
		if (self.game != null && EEGhostSpot != null) EEGhostSpot.type = _Enums.EEGhostSpot;
	}

	private static void DeathPersistentSaveDataOnCtor(On.DeathPersistentSaveData.orig_ctor orig, DeathPersistentSaveData self, SlugcatStats.Name slugcat)
	{
		orig(self, slugcat);
		//self.ghostsTalkedTo = new int[Enum.GetValues(typeof(GhostWorldPresence.GhostID)).Length];
	}

	private static bool GhostWorldPresenceOnSpawnGhost(On.GhostWorldPresence.orig_SpawnGhost orig, GhostWorldPresence.GhostID ghostid, int karma, int karmacap, int ghostpreviouslyencountered, bool playingasred)
	{
		var vanilla_result = orig(ghostid, karma, karmacap, ghostpreviouslyencountered, playingasred);
		if (!EchoParser.__extendedEchoIDs.Contains(ghostid)) return vanilla_result;
		EchoSettings settings = EchoParser.__echoSettings[ghostid];
		bool SODcondition = settings.SpawnOnThisDifficulty(__slugcatNumber ?? SlugcatStats.Name.White);
		bool karmaCondition = settings.KarmaCondition(karma, karmacap, __slugcatNumber ?? SlugcatStats.Name.White);
		bool karmaCapCondition = settings.GetMinimumKarmaCap(__slugcatNumber ?? SlugcatStats.Name.White) <= karmacap;
		__logger.LogInfo($"[Echo Extender] Getting echo conditions for {ghostid}");
		__logger.LogInfo($"[Echo Extender] Using difficulty {__slugcatNumber} ({__slugcatNumber?.Index})");
		__logger.LogInfo($"[Echo Extender] Spawn On Difficulty : {(SODcondition ? "Met" : "Not Met")} [Required: <{string.Join(", ", (settings.SpawnOnDifficulty.Length > 0 ? settings.SpawnOnDifficulty : EchoSettings.Default.SpawnOnDifficulty).Select(i => $"{i.value} ({i.Index})").ToArray())}>]");
		__logger.LogInfo($"[Echo Extender] Minimum Karma : {(karmaCondition ? "Met" : "Not Met")} [Required: {(settings.GetMinimumKarma(__slugcatNumber ?? SlugcatStats.Name.White) == -2 ? "Dynamic" : settings.GetMinimumKarma(__slugcatNumber ?? SlugcatStats.Name.White).ToString())}, Having: {karma}]");
		__logger.LogInfo($"[Echo Extender] Minimum Karma Cap : {(karmaCapCondition ? "Met" : "Not Met")} [Required: {settings.GetMinimumKarmaCap(__slugcatNumber ?? SlugcatStats.Name.White)}, Having: {karmacap}]");
		EchoSettings.PrimingKind prime = settings.GetPriming(__slugcatNumber ?? SlugcatStats.Name.White);
		bool primedCond = prime switch
		{
			EchoSettings.PrimingKind.Yes => ghostpreviouslyencountered == 1,
			_ => ghostpreviouslyencountered != 2
		};
		__logger.LogInfo($"[Echo Extender] Primed : {(primedCond ? "Met" : "Not Met")} [Required: {(prime)}, Having {ghostpreviouslyencountered}]");
		__logger.LogInfo($"[Echo Extender] Spawning Echo : {primedCond && SODcondition && karmaCondition && karmaCapCondition}");
		return
			primedCond &&
			SODcondition &&
			karmaCondition &&
			karmaCapCondition;
	}

	private static void GhostConversationOnAddEvents(On.GhostConversation.orig_AddEvents orig, GhostConversation self)
	{
		orig(self);
		if (!EchoParser.__echoConversations.ContainsKey(self.id))
		{
			return;
		}
		foreach (string line in Regex.Split(EchoParser.__echoConversations[self.id], "(\r|\n)+"))
		{
			string? resText = null;
			__logger.LogDebug($"[Echo Extender] Processing line {line}");
			if (line.All(c => char.IsSeparator(c) || c == '\n' || c == '\r')) continue;
			if (!line.StartsWith("("))
			{
				__logger.LogDebug("line is normal");
				//self.events.Add(new Conversation.TextEvent(self, 0, line, 0));
				resText = line;
				goto MAKE_EVENT_;
			}
			int closingParenIndex = line.IndexOf(")", StringComparison.Ordinal);
			string? difficulties = line.Substring(1, closingParenIndex - 1);
			string[] diffs = difficulties.Split(',');
			__logger.LogDebug($"line is conditional. {diffs.Length} suitable diffs, testing against \"{__slugcatNumber?.value}\"");
			//if (diffs.Length is 0) continue;
			//__logger.LogDebug(diffs.)
			foreach (string diff in diffs)
			{
				__logger.LogDebug($"op: \"{diff}\" ({diff == __slugcatNumber?.value})");
				if (diff.Trim() == __slugcatNumber?.value)
				{
					//self.events.Add(new Conversation.TextEvent(self, 0, Regex.Replace(line, @"^\((\d|(\d+,)+\d)\)", ""), 0)); //we no longer numbah here
					resText = line.Substring(closingParenIndex + 1);
					break;
				}
			}
		MAKE_EVENT_:
			if (resText is null) continue;
			self.events.Add(new Conversation.TextEvent(self, 0, resText, 0));
		}
	}

	private static void GhostOnStartConversation(On.Ghost.orig_StartConversation orig, Ghost self)
	{
		orig(self);
		if (!EchoParser.__extendedEchoIDs.Contains(self.worldGhost.ghostID)) return;
		string echoRegionString = self.worldGhost.ghostID.ToString();
		self.currentConversation = new GhostConversation(EchoParser.GetConversationID(echoRegionString), self, self.room.game.cameras[0].hud.dialogBox);
	}

	private static GhostWorldPresence.GhostID GhostWorldPresenceOnGetGhostID(On.GhostWorldPresence.orig_GetGhostID orig, string regionname)
	{
		var origResult = orig(regionname);
		return EchoParser.EchoIDExists(regionname) ? EchoParser.GetEchoID(regionname) : origResult;
	}

	private static void GhostWorldPresenceOnCtor(On.GhostWorldPresence.orig_ctor orig, GhostWorldPresence self, World world, GhostWorldPresence.GhostID ghostid)
	{
		orig(self, world, ghostid);
		if (self.ghostRoom is null && EchoParser.__extendedEchoIDs.Contains(self.ghostID))
		{
			SlugcatStats.Name slugnum = __slugcatNumber ?? SlugcatStats.Name.White;
			self.ghostRoom = world.GetAbstractRoom(EchoParser.__echoSettings[ghostid].GetEchoRoom(slugnum));
			self.songName = EchoParser.__echoSettings[ghostid].GetEchoSong(slugnum);
			__logger.LogInfo($"[Echo Extender] Set Song: {self.songName}");
			__logger.LogInfo($"[Echo Extender] Set Room {self.ghostRoom?.name}");
		}
	}

	private static void GhostOnCtor(On.Ghost.orig_ctor orig, Ghost self, Room room, PlacedObject placedobject, GhostWorldPresence worldghost)
	{
		orig(self, room, placedobject, worldghost);
		if (!EchoParser.__extendedEchoIDs.Contains(self.worldGhost.ghostID)) return;
		var settings = EchoParser.__echoSettings[self.worldGhost.ghostID];
		SlugcatStats.Name slugnum = __slugcatNumber ?? SlugcatStats.Name.White;
		self.scale = settings.GetSizeMultiplier(slugnum) * 0.75f;
		self.defaultFlip = settings.GetDefaultFlip(slugnum);
	}
}
