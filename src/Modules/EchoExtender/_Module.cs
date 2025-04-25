using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using RWCustom;
using UnityEngine;
namespace RegionKit.Modules.EchoExtender;
///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Echo Extender")]
public static class _Module
{
	/// <summary>
	/// Applies hooks
	/// </summary>
	public static void Enable()
	{

		// Tests for spawn
		On.GhostWorldPresence.ctor_World_GhostID_int += GhostWorldPresenceOnCtor;
		On.GhostWorldPresence.GetGhostID += GhostWorldPresenceOnGetGhostID;

		// Spawn and customization
		On.Room.Loaded += RoomOnLoaded;
		On.GhostWorldPresence.SpawnGhost += GhostWorldPresenceOnSpawnGhost;
		On.GhostWorldPresence.GhostMode_AbstractRoom_Vector2 += GhostWorldPresenceOnGhostMode;

		// Save stuff
		On.StoryGameSession.ctor += StoryGameSessionOnCtor;
	}

	/// <summary>
	/// Undoes hooks
	/// </summary>
	public static void Disable()
	{
		On.GhostWorldPresence.ctor_World_GhostID_int -= GhostWorldPresenceOnCtor;
		On.GhostWorldPresence.GetGhostID -= GhostWorldPresenceOnGetGhostID;

		// Spawn and customization
		On.Room.Loaded -= RoomOnLoaded;
		On.GhostWorldPresence.SpawnGhost -= GhostWorldPresenceOnSpawnGhost;
		On.GhostWorldPresence.GhostMode_AbstractRoom_Vector2 -= GhostWorldPresenceOnGhostMode;

		// Save stuff
		On.StoryGameSession.ctor -= StoryGameSessionOnCtor;
	}


	private static void StoryGameSessionOnCtor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name savestatenumber, RainWorldGame game)
	{
		LogInfo("[Echo Extender] Loading Echoes from Region Mods...");
		EchoParser.LoadAllRegions(savestatenumber);
		orig(self, savestatenumber, game);
	}

	private static float GhostWorldPresenceOnGhostMode(On.GhostWorldPresence.orig_GhostMode_AbstractRoom_Vector2 orig, GhostWorldPresence self, AbstractRoom testRoom, Vector2 worldPos)
	{
		var result = orig(self, testRoom, worldPos);

		var presenceOverride = PresenceOverride(self, testRoom);
		if (presenceOverride != -1f) return presenceOverride;

		if (!EchoParser.__echoSettings.TryGetValue(self.ghostID, out var settings)) return result;
		if (testRoom.index == self.ghostRoom.index) return 1f;
		var echoEffectLimit = settings.EffectRadius * 1000f; //I think 1 screen is like a 1000 so I'm going with that
		Vector2 globalDistance = Custom.RestrictInRect(worldPos, FloatRect.MakeFromVector2(self.world.RoomToWorldPos(new Vector2(), self.ghostRoom.index), self.world.RoomToWorldPos(self.ghostRoom.size.ToVector2() * 20f, self.ghostRoom.index)));
		if (!Custom.DistLess(worldPos, globalDistance, echoEffectLimit)) return 0;
		var someValue = self.DegreesOfSeparation(testRoom); //No clue what this number does
		return someValue == -1 ? 0.0f
			: (float)(Mathf.Pow(Mathf.InverseLerp(echoEffectLimit, echoEffectLimit / 8f, Vector2.Distance(worldPos, globalDistance)), 2f) * (double)Custom.LerpMap(someValue, 1f, 3f, 0.6f, 0.15f) * (testRoom.layer != self.ghostRoom.layer ? 0.600000023841858 : 1.0));
	}

	private static float PresenceOverride(GhostWorldPresence self, AbstractRoom testRoom)
	{

		if (!self.RoomOverrides().ContainsKey(testRoom.name))
		{
			Room room = testRoom.realizedRoom ?? new Room(self.world.game, self.world, testRoom); //goofy but safe way to load room settings for abstract room

			if (room.roomSettings.GetEffect(_Enums.EchoPresenceOverride) != null)
			{
				self.RoomOverrides()[testRoom.name] = room.roomSettings.GetEffectAmount(_Enums.EchoPresenceOverride);
			}
			else
			{
				self.RoomOverrides()[testRoom.name] = -1f;
			}
		}

		return self.RoomOverrides()[testRoom.name];
	}

	//caching room values because loading a room is very expensive
	private static ConditionalWeakTable<GhostWorldPresence, Dictionary<string, float>> _RoomOverrides = new();
	public static Dictionary<string, float> RoomOverrides(this GhostWorldPresence p) => _RoomOverrides.GetValue(p, _ => new());

	private static void RoomOnLoaded(On.Room.orig_Loaded orig, Room self)
	{
		bool hasEEGhost = self.world.worldGhost != null && EchoParser.__extendedEchoIDs.Contains(self.world.worldGhost.ghostID);
		if (hasEEGhost)
		{
			// Backwards compatibility
			foreach (PlacedObject obj in self.roomSettings.placedObjects)
			{
				if (obj.type == PlacedObject.Type.GhostSpot)
				{
					obj.type = _Enums.EEGhostSpot;
				}
			}
		}
		orig(self);
		if (hasEEGhost)
		{
			foreach (PlacedObject obj in self.roomSettings.placedObjects)
			{
				if (obj.type == _Enums.EEGhostSpot && obj.active)
				{
					if (self.game.world.worldGhost != null && self.game.world.worldGhost.ghostRoom == self.abstractRoom)
					{
						self.AddObject(new EEGhost(self, obj, self.game.world.worldGhost));
					}
					else if (self.world.region != null)
					{
						GhostWorldPresence.GhostID ghostID = GhostWorldPresence.GetGhostID(self.world.region.name);
						if (self.game.session is StoryGameSession && (!self.game.GetStorySession.saveState.deathPersistentSaveData.ghostsTalkedTo.ContainsKey(ghostID) || self.game.GetStorySession.saveState.deathPersistentSaveData.ghostsTalkedTo[ghostID] == 0))
						{
							self.AddObject(new GhostHunch(self, ghostID));
						}
					}
				}
			}
		}
	}

	private static bool GhostWorldPresenceOnSpawnGhost(On.GhostWorldPresence.orig_SpawnGhost orig, GhostWorldPresence.GhostID ghostid, int karma, int karmacap, int ghostpreviouslyencountered, bool playingasred)
	{
		var vanilla_result = orig(ghostid, karma, karmacap, ghostpreviouslyencountered, playingasred);
		if (!EchoParser.__extendedEchoIDs.Contains(ghostid)) return vanilla_result;
		EchoSettings settings = EchoParser.__echoSettings[ghostid];
		bool SODcondition = settings.SpawnOnDifficulty;
		bool karmaCondition = settings.KarmaCondition(karma, karmacap);
		bool karmaCapCondition = settings.MinimumKarmaCap <= karmacap;
		LogInfo($"[Echo Extender] Getting echo conditions for {ghostid}");
		//LogInfo($"[Echo Extender] Using difficulty {__slugcatNumber} ({__slugcatNumber?.Index})");
		LogInfo($"[Echo Extender] Spawn On Difficulty : {(SODcondition ? "Met" : "Not Met")}");
		LogInfo($"[Echo Extender] Minimum Karma : {(karmaCondition ? "Met" : "Not Met")} [Required: {(settings.MinimumKarma == -1 ? "Dynamic" : settings.MinimumKarma.ToString())}, Having: {karma}]");
		LogInfo($"[Echo Extender] Minimum Karma Cap : {(karmaCapCondition ? "Met" : "Not Met")} [Required: {settings.MinimumKarmaCap}, Having: {karmacap}]");
		EchoSettings.PrimingKind prime = settings.RequirePriming;
		bool primedCond = prime switch
		{
			EchoSettings.PrimingKind.Yes => ghostpreviouslyencountered == 1,
			_ => ghostpreviouslyencountered != 2
		};
		LogInfo($"[Echo Extender] Primed : {(primedCond ? "Met" : "Not Met")} [Required: {(prime)}, Having {ghostpreviouslyencountered}]");
		LogInfo($"[Echo Extender] Spawning Echo : {primedCond && SODcondition && karmaCondition && karmaCapCondition}");
		return
			primedCond &&
			SODcondition &&
			karmaCondition &&
			karmaCapCondition;
	}

	private static GhostWorldPresence.GhostID GhostWorldPresenceOnGetGhostID(On.GhostWorldPresence.orig_GetGhostID orig, string regionname)
	{
		var origResult = orig(regionname);
		return EchoParser.EchoIDExists(regionname) ? EchoParser.GetEchoID(regionname) : origResult;
	}

	private static void GhostWorldPresenceOnCtor(On.GhostWorldPresence.orig_ctor_World_GhostID_int orig, GhostWorldPresence self, World world, GhostWorldPresence.GhostID ghostid, int spinningTopSpawnId)
	{
		orig(self, world, ghostid, spinningTopSpawnId);
		if (self.ghostRoom is null && EchoParser.__extendedEchoIDs.Contains(self.ghostID))
		{
			self.ghostRoom = world.GetAbstractRoom(EchoParser.__echoSettings[ghostid].EchoRoom);
			self.songName = EchoParser.__echoSettings[ghostid].EchoSong;
			LogInfo($"[Echo Extender] Set Song: {self.songName}");
			LogInfo($"[Echo Extender] Set Room {self.ghostRoom?.name}");
		}
	}
}
