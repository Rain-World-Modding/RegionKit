//extended gates by Henpemaz

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

using Gate = RegionGate;
using Req = RegionGate.GateRequirement;

namespace RegionKit.Modules.Misc;

public static class ExtendedGates
{
	public static bool IsVanilla(Req req)
	{
		return req?.ToString() is "1" or "2" or "3" or "4" or "5" or "L" or "R";
	}
	public static int GetKarmaLevel(this Req req)
	{
		int result = -1;
		string str = req.ToString();
		if (str.EndsWith(ALT_POSTFIX))
		{
			int.TryParse(str[..^ALT_POSTFIX.Length], out result);
		}
		else
		{
			int.TryParse(str, out result);
		}
		return result - 1;
	}
	private const string ALT_POSTFIX = "alt";

	public static bool IsAlt(Req req)
	{
		return req?.ToString().EndsWith(ALT_POSTFIX) ?? false;
	}
	public static class Enums_EG
	{

		public static Req uwu = new(nameof(uwu), true);
		public static Req Open = new(nameof(Open), true);
		public static Req Forbidden = new(nameof(Forbidden), true);
		public static Req Glow = new(nameof(Glow), true);
		public static Req CommsMark = new(nameof(CommsMark), true);
		public static Req TenReinforced = new(nameof(TenReinforced), true);
		public static Req SixKarma = new("6", true);
		public static Req SevenKarma = new("7", true);
		public static Req EightKarma = new("8", true);
		public static Req NineKarma = new("9", true);
		public static Req TenKarma = new("10", true);
		public static Req[] alt = new Req[10];
		public static void Register()
		{
			foreach (int i in Range(10))
			{
				alt[i] = new(i + ALT_POSTFIX, true);
			}
		}
	}
	public const string ModID = "ExtendedGates";
	public const string Version = "1.3";
	public const string author = "Henpemaz";
	// 1.0 initial release
	// 1.1 13/06/2021 bugfix 6 karma at 5 cap; fix showing open side over karma for inregion minimap

	static Type? uwu;

	public static void Enable()
	{
		On.RainWorld.LoadResources += RainWorld_LoadResources;

		On.GateKarmaGlyph.DrawSprites += GateKarmaGlyph_DrawSprites;

		On.RegionGate.ctor += RegionGate_ctor;
		On.RegionGate.Update += RegionGate_Update;
		On.RegionGate.KarmaBlinkRed += RegionGate_KarmaBlinkRed;

		On.RegionGateGraphics.Update += RegionGateGraphics_Update;
		On.RegionGateGraphics.DrawSprites += RegionGateGraphics_DrawSprites;

		On.HUD.Map.GateMarker.ctor += GateMarker_ctor;
		On.HUD.Map.MapData.KarmaOfGate += MapData_KarmaOfGate;
		uwu = null;
		foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (asm.GetName().Name == "UwUMod")
			{
				uwu = asm.GetType("UwUMod.UwUMod");
			}
		}

		On.VirtualMicrophone.NewRoom += VirtualMicrophone_NewRoom;
	}
	public static void Disable()
	{
		On.RainWorld.LoadResources -= RainWorld_LoadResources;

		On.GateKarmaGlyph.DrawSprites -= GateKarmaGlyph_DrawSprites;

		On.RegionGate.ctor -= RegionGate_ctor;
		On.RegionGate.Update -= RegionGate_Update;
		On.RegionGate.KarmaBlinkRed -= RegionGate_KarmaBlinkRed;

		On.RegionGateGraphics.Update -= RegionGateGraphics_Update;
		On.RegionGateGraphics.DrawSprites -= RegionGateGraphics_DrawSprites;

		On.HUD.Map.GateMarker.ctor -= GateMarker_ctor;
		On.HUD.Map.MapData.KarmaOfGate -= MapData_KarmaOfGate;

		On.VirtualMicrophone.NewRoom -= VirtualMicrophone_NewRoom;
	}

	private static void RegionGateGraphics_DrawSprites(On.RegionGateGraphics.orig_DrawSprites orig, RegionGateGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		orig(self, sLeaser, rCam, timeStacker, camPos);
		if (self.gate is ElectricGate eg && eg.batteryLeft > 1.1f)
		{
			sLeaser.sprites[self.BatteryMeterSprite].scaleX = 420f;
			float num4 = (!eg.batteryChanging) ? 0f : 1f;
			sLeaser.sprites[self.BatteryMeterSprite].color = Color.Lerp(RWCustom.Custom.HSL2RGB(0.03f + 0.3f * (Mathf.InverseLerp(1.1f, 30f, eg.batteryLeft)) + UnityEngine.Random.value * (0.035f * num4 + 0.025f), 1f, (0.5f + UnityEngine.Random.value * 0.2f * num4) * Mathf.Lerp(1f, 0.25f, self.darkness)), self.blackColor, 0.5f);
		}
	}

	private static void RegionGateGraphics_Update(On.RegionGateGraphics.orig_Update orig, RegionGateGraphics self)
	{
		orig(self);
		if (self.gate is WaterGate wg && wg.waterLeft > 2f && self.water != null) self.WaterLevel = 1f; // Caps max display water so it doesnt look silly dark
	}

	/// <summary>
	/// Fix gate noises following the player through rooms
	/// </summary>
	private static void VirtualMicrophone_NewRoom(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
	{
		orig(self, room);
		for (int i = self.soundObjects.Count - 1; i >= 0; i--)
		{
			if (self.soundObjects[i] is VirtualMicrophone.PositionedSound) // Doesn't make sense that this carries over
			{
				// I was going to do somehtin supercomplicated like test if controller as loop was in the same room but screw it
				//VirtualMicrophone.ObjectSound obj = (self.soundObjects[i] as VirtualMicrophone.ObjectSound);
				//if (obj.controller != null && )
				self.soundObjects[i].Destroy();
				self.soundObjects.RemoveAt(i);
			}
		}
	}

	private static void RainWorld_LoadResources(On.RainWorld.orig_LoadResources orig, RainWorld self)
	{
		_Assets.LoadAditionalResources(); // Don't want to overwrite, wants to be overwritable, load first :)
		orig(self);
	}

	#region MAPHOOKS
	private static void GateMarker_ctor(On.HUD.Map.GateMarker.orig_ctor orig, HUD.Map.GateMarker self, HUD.Map map, int room, RegionGate.GateRequirement req, bool showAsOpen)
	{
		orig(self, map, room, IsVanilla(req) ? req : Req.OneKarma, showAsOpen);
		if (IsVanilla(req))
		{
			__logger.LogInfo($"Vanilla req {req}, skipping");
			return;
		}
		int karma = 0;
		__logger.LogInfo($"Constructing custom gate marker image...");
		string reqstr = req.ToString();
		switch (reqstr)
		{
		//todo: did you convert it right?
		case nameof(Enums_EG.Open): // open
			self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaOpen"); // Custom
			break;
		case nameof(Enums_EG.TenReinforced): // 10reinforced
			self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarma10reinforced"); // Custom
			break;
		case nameof(Enums_EG.Forbidden): // forbidden
			self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaForbidden"); // Custom
			break;
		case nameof(Enums_EG.CommsMark): // comsmark
			self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaComsmark"); // Custom
			break;
		case nameof(Enums_EG.uwu): // uwu
			self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaUwu"); // Custom
			break;
		case nameof(Enums_EG.Glow): // glow
			self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaGlow"); // Custom
			break;
		case "6":
		case "7":
		case "8":
		case "9":
		case "10":
		default:
			karma = req.GetKarmaLevel();
			if (karma > 4)
			{
				int? cap = map.hud.rainWorld.progression?.currentSaveState?.deathPersistentSaveData?.karmaCap;
				if (!cap.HasValue || cap.Value < karma) cap = Mathf.Max(6, karma);
				self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarma" + karma.ToString()
				+ "-" + cap.Value.ToString()); // Vanilla, zero-indexed
			}
			else
			{
				self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaNoRing" + karma);
			}
			break;
		}
	}

	private static Req MapData_KarmaOfGate(On.HUD.Map.MapData.orig_KarmaOfGate orig, HUD.Map.MapData self, PlayerProgression progression, World initWorld, string roomName)
	{
		string path = AssetManager.ResolveFilePath("world/gates/extendedlocks.txt");
		if (!File.Exists(path)) goto DEFAULT_;
		string[] array = File.ReadAllLines(path);
		for (int i = 0; i < array.Length; i++)
		{
			if (string.IsNullOrEmpty(array[i]) || string.IsNullOrEmpty(array[i].Trim())) continue;
			if (array[i].IndexOf("//") > -1)
			{
				array[i] = array[i].Substring(array[i].IndexOf("//"));
			}
			if (string.IsNullOrEmpty(array[i]) || string.IsNullOrEmpty(array[i].Trim())) continue;
			string[] array2 = Regex.Split(array[i], " : ");
			if (array2[0] == roomName)
			{

				string req1 = array2[1];
				string req2 = array2[2];
				Req? result = new(req1);
				Req? result2 = new(req2);
				//if (result > 1000) result -= 1000; // alt art mode irrelevant here
				//if (result2 > 1000) result2 -= 1000; // alt art mode irrelevant here

				bool thisGateIsFlippedForWhateverReason = false;
				if (roomName == "GATE_LF_SB" || roomName == "GATE_DS_SB" || roomName == "GATE_HI_CC" || roomName == "GATE_SS_UW")
				{
					thisGateIsFlippedForWhateverReason = true;
				}

				string[] namearray = Regex.Split(roomName, "_");
				if (namearray.Length == 3)
				{
					for (int j = 0; j < namearray.Length; j++)
					{
						if (namearray[j] == "UX")
						{
							namearray[j] = "UW";
						}
						else if (namearray[j] == "SX")
						{
							namearray[j] = "SS";
						}
					}
				}
				if (namearray.Length != 3 || (namearray[1] == namearray[2]) || (namearray[1] != initWorld.region.name && namearray[2] != initWorld.region.name)) // In-region gate support
				{
					//todo wtf is this supposed to mean
					// Not worht the trouble of telling which "side" the player is looking from the minimap, pick max
					//if (result == 1000) result = 0; // open is less important than any karma values
					//if (result2 == 1000) result2 = 0;
					//return Mathf.Max(result, result2);
				}

				if (namearray[1] == initWorld.region.name != thisGateIsFlippedForWhateverReason)
				{
					return result ?? Req.OneKarma;
				}
				return result2 ?? Req.OneKarma;
			}
		}

	DEFAULT_:;
		return orig(self, progression, initWorld, roomName);
	}

	#endregion MAPHOOKS

	#region GATEHOOKS

	/// <summary>
	/// Loads karmaGate requirements
	/// </summary>
	private static void RegionGate_ctor(On.RegionGate.orig_ctor orig, RegionGate self, Room room)
	{
		orig(self, room);

		string path2 = AssetManager.ResolveFilePath("world/gates/extendedlocks.txt");//path + "World" + Path.DirectorySeparatorChar + "Gates" + Path.DirectorySeparatorChar + "extendedLocks.txt";
		if (!File.Exists(path2))
		{
			__logger.LogMessage("ExtendedLocks is not present; skipping");
			return;
		}
		string[] lines = File.ReadAllLines(path2);

		for (int i = 0; i < lines.Length; i++)
		{
			if (string.IsNullOrEmpty(lines[i]) || string.IsNullOrEmpty(lines[i].Trim())) continue;
			if (lines[i].IndexOf("//") > -1)
			{
				__logger.LogMessage("Found comment?..");
				lines[i] = lines[i].Substring(lines[i].IndexOf("//"));
			}
			if (string.IsNullOrEmpty(lines[i]) || string.IsNullOrEmpty(lines[i].Trim())) continue;

			string[] array2 = Regex.Split(lines[i], "\\s+:\\s+");
			if (array2[0] != room.abstractRoom.name)
			{
				__logger.LogDebug($"{lines[i]} does not match {room.abstractRoom.name}");
				continue;
			}
			__logger.LogMessage($"Found a match!");
			self.karmaGlyphs[0].Destroy();
			self.karmaGlyphs[1].Destroy();
			string req1 = array2[1];
			string req2 = array2[2];
			self.karmaRequirements[0] = new(req1);
			self.karmaRequirements[1] = new(req2);
			bool alt1 = IsAlt(self.karmaRequirements[0]);
			bool alt2 = IsAlt(self.karmaRequirements[1]);
			__logger.LogMessage($"{req1} {req2} {alt1} {alt2}");
			self.karmaGlyphs = new GateKarmaGlyph[2];
			self.karmaGlyphs[0] = new GateKarmaGlyph(false, self, self.karmaRequirements[0]/*  + (alt1 ? 1000 : 0) */);
			room.AddObject(self.karmaGlyphs[0]);
			self.karmaGlyphs[1] = new GateKarmaGlyph(true, self, self.karmaRequirements[1] /* + (alt2 ? 1000 : 0) */);
			room.AddObject(self.karmaGlyphs[1]);

			if (array2.Length > 3 && array2[3].ToLower() == "multi") // "Infinite" uses
			{
				if (self is WaterGate wg) // sets water level, don't want to get into crazy float craze
				{
					wg.waterLeft = 30f; // ((!room.world.regionState.gatesPassedThrough[room.abstractRoom.gateIndex]) ? 2f : 1f);
				}
				else if (self is ElectricGate eg)
				{
					eg.batteryLeft = 30f; // ((!room.world.regionState.gatesPassedThrough[room.abstractRoom.gateIndex]) ? 2f : 1f);
				}
			}
			return;
		}
	}

	/// <summary>
	/// Adds support for special gate types UwU
	/// </summary>
	private static void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
	{
		int preupdateCounter = self.startCounter;
		orig(self, eu);
		if (!self.room.game.IsStorySession) return; // Paranoid, just like in the base game
		switch (self.mode.ToString())
		{
		case nameof(RegionGate.Mode.MiddleClosed):
			int num = self.PlayersInZone();
			//if (RNG.value < 0.05f) __logger.LogDebug($"{num},{self.dontOpen},{self.PlayersStandingStill()},{self.EnergyEnoughToOpen},{PlayersMeetSpecialRequirements(self)}");
			if (num > 0 && num < 3 && !self.dontOpen && self.PlayersStandingStill() && self.EnergyEnoughToOpen && PlayersMeetSpecialRequirements(self))
			{
				self.startCounter = preupdateCounter + 1;
			}
			if (self.startCounter == 59)
			{
				__logger.LogDebug("Opening...");
				// OPEN THE GATES on the next frame
				if (self.room.game.GetStorySession.saveStateNumber == SlugcatStats.Name.Yellow)
				{
					self.Unlock(); // sets savestate thing for monk
				}
				self.unlocked = true;
			}
			break;

		case nameof(RegionGate.Mode.ClosingAirLock):
			if (preupdateCounter == 59) // We did it... last frame
			{
				// if it shouldn't be unlocked, lock it back
				self.unlocked =
				(
					self.room.game.GetStorySession.saveStateNumber == SlugcatStats.Name.Yellow
					&& self.room.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates.Contains(self.room.abstractRoom.name)
				)
				||
				(
					self.room.game.StoryCharacter != SlugcatStats.Name.Red
					&& File.Exists(AssetManager.ResolveFilePath("nifflasmode.txt"))
				);
			}

			if (self.room.game.overWorld.worldLoader == null) // In-region gate support
			{
				self.waitingForWorldLoader = false;
			}
			break;
		case nameof(RegionGate.Mode.Closed): // Support for multi-usage gates
			if (self.EnergyEnoughToOpen) self.mode = RegionGate.Mode.MiddleClosed;
			break;
		}
	}

	private static bool RegionGate_KarmaBlinkRed(On.RegionGate.orig_KarmaBlinkRed orig, RegionGate self)
	{
		return orig(self) && !PlayersMeetSpecialRequirements(self);
		// return false;
		// if (self.mode != RegionGate.Mode.MiddleClosed)
		// {
		// 	int num = self.PlayersInZone();
		// 	if (num > 0 && num < 3)
		// 	{
		// 		//self.letThroughDir = (num == 1);
		// 		if (!self.dontOpen && self.karmaRequirements[(!(num == 1)) ? 1 : 0] == Enums_EG.Forbidden) // Forbidden
		// 		{
		// 			return true;
		// 		}
		// 	}
		// }
		// Orig doesn't blink if "unlocked", but we know better, forbiden shall stay forbidden
	}

	private static bool PlayersMeetSpecialRequirements(RegionGate self)
	{
		switch (self.karmaRequirements[(!self.letThroughDir) ? 1 : 0].ToString())
		{
		case nameof(Enums_EG.Open): // open
			return true;
		case nameof(Enums_EG.TenReinforced): // 10reinforced
			if (((self.room.game.Players[0].realizedCreature is Player p) && p.Karma == 9 && p.KarmaIsReinforced) || self.unlocked)
				return true;
			break;
		case nameof(Enums_EG.Forbidden): // forbidden
			self.startCounter = 0;
			// caused problems with karmablinkred
			// self.dontOpen = true; // Hope this works against MONK players smh.
			break;
		case nameof(Enums_EG.CommsMark): // comsmark
			if (self.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark || self.unlocked)
				return true;
			break;
		case nameof(Enums_EG.uwu): // uwu
			if (uwu != null || self.unlocked)
				return true;
			break;
		case nameof(Enums_EG.Glow): // glow
			if (self.room.game.GetStorySession.saveState.theGlow || self.unlocked)
				return true;
			break;
		default: // default karma gate handled by the game
			var player = (self.room.game.Players.FirstOrDefault().realizedCreature as Player);
			return player?.Karma >= self.karmaRequirements[0].GetKarmaLevel();
			
		}

		return false;
	}

	/// <summary>
	/// Adds support to karma 6 thru 10 for gates, also special gates
	/// </summary>
	private static void GateKarmaGlyph_DrawSprites(On.GateKarmaGlyph.orig_DrawSprites orig, GateKarmaGlyph self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
	{
		if (self.symbolDirty) // redraw
		{
			bool altArt = DoesPlayerDeserveAltArt(self); // this was probably too costly to call every frame, moved
			if ((!self.gate.unlocked || self.requirement == Enums_EG.Forbidden) && (!IsVanilla(self.requirement))) // Custom
			{
				switch (self.requirement.ToString())
				{
				case nameof(Enums_EG.Open): // open
					sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol0"); // its vanilla
					break;
				case nameof(Enums_EG.TenReinforced): // 10reinforced
					sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol10reinforced"); // Custom
					break;
				case nameof(Enums_EG.Forbidden): // forbidden
					sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbolForbidden"); // Custom
					break;
				case nameof(Enums_EG.CommsMark): // comsmark
					sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbolComsmark"); // Custom
					break;
				case nameof(Enums_EG.uwu): // uwu
					sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbolUwu"); // Custom
					break;
				case nameof(Enums_EG.Glow): // glow
					sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbolGlow"); // Custom
					break;
				default:
					var trueReq = self.requirement;
					if (trueReq.ToString().EndsWith(ALT_POSTFIX)) // alt art
					{
						altArt = true;
					}
					var intreq = trueReq.GetKarmaLevel();
					if (intreq > 5)
					{
						int cap = (self.room.game.session as StoryGameSession)!.saveState.deathPersistentSaveData.karmaCap;
						if (cap < intreq) cap = Mathf.Max(6, intreq);
						sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol" + (intreq + 1).ToString() + "-" + (cap + 1).ToString() + (altArt ? "alt" : "")); // Custom, 1-indexed
					}
					else
					{
						sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol" + (intreq + 1).ToString() + (altArt ? "alt" : "")); // Alt art for vanilla gates
					}
					break;
				}
				self.symbolDirty = false;
			}
		}
		orig(self, sLeaser, rCam, timeStacker, camPos);
	}

	private static bool DoesPlayerDeserveAltArt(GateKarmaGlyph self)
	{

		SaveState? saveState = (self.room?.game?.session as StoryGameSession)?.saveState;
		if (saveState == null) return false;
		WinState? winState = saveState.deathPersistentSaveData?.winState;
		if (winState == null) return false;

		float chieftain = self.room!.game.session.creatureCommunities.LikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0);
		if (self.room.game.StoryCharacter == SlugcatStats.Name.Yellow)
		{
			chieftain = Mathf.InverseLerp(0.42f, 0.9f, chieftain);
		}
		else
		{
			chieftain = Mathf.InverseLerp(0.1f, 0.8f, chieftain);
		}
		chieftain = Mathf.Floor(chieftain * 20f) / 20f;
		if (chieftain < 0.5f) return false;

		int passages = 0;
		for (int i = 0; i < winState.endgameTrackers.Count; i++)
		{
			if (winState.endgameTrackers[i].GoalFullfilled)
			{
				passages++;
			}
		}
		if (passages < 6) return false;

		float lizfrend = self.room.game.session.creatureCommunities.LikeOfPlayer(CreatureCommunities.CommunityID.Lizards, -1, 0);
		if (lizfrend < -0.45f) return false;

		if (saveState.cycleNumber < 43) return false;

		WinState.BoolArrayTracker wanderer = (winState.GetTracker(WinState.EndgameID.Traveller, false) as WinState.BoolArrayTracker)!;
		if (wanderer == null || wanderer.progress.Count(c => c) < 5) return false;

		return true;
	}

	#endregion GATEHOOKS

	
}
