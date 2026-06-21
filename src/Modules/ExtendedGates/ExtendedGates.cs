//extended gates by Henpemaz

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using static MonoMod.InlineRT.MonoModRule;
using ExtendedLocks = RegionKit.Modules.ExtendedGates.ExtendedLocks;
using Gate = RegionGate;
using Req = RegionGate.GateRequirement;

namespace RegionKit.Modules.ExtendedGates;

/// <summary>
/// Adds more options for gate requirements
/// </summary>
public static class ExtendedGates
{
	public static bool hasSeenConstructionGateTutorial = false;

	public static Dictionary<Req, LockData> ExLocks = new();

	public static Dictionary<Req, LockData> SpecialConditions = new();
	public static List<ExtraRequirement> ExtraRequirements = new();

	private static readonly ConditionalWeakTable<Gate, List<string>> _Tags = new();
	private static readonly ConditionalWeakTable<Gate, List<(bool side, ExtraRequirement req)>> _ExtraReqs = new();
	private static readonly HashSet<WinState.EndgameID> _RegisteredPassageReqs = new();
	public static List<string> Tags(this Gate p) => _Tags.GetValue(p, _ => new());

	public static void InitExLocks()
	{
		ExLocks = new()
		{
			[_Enums.Construction] = new ExtendedLocks.Construction(),
			[_Enums.Open] = new ExtendedLocks.Open(),
			[_Enums.Forbidden] = new ExtendedLocks.Forbidden(),
			[_Enums.CommsMark] = new ExtendedLocks.ComsMark(),
			[_Enums.Glow] = new ExtendedLocks.Glow(),
			[_Enums.uwu] = new ExtendedLocks.UWU(),
			[_Enums.TenReinforced] = new ExtendedLocks.TenReinforced(),
			[_Enums.SixKarma] = new ExtendedLocks.Numerical(_Enums.SixKarma),
			[_Enums.SevenKarma] = new ExtendedLocks.Numerical(_Enums.SevenKarma),
			[_Enums.EightKarma] = new ExtendedLocks.Numerical(_Enums.EightKarma),
			[_Enums.NineKarma] = new ExtendedLocks.Numerical(_Enums.NineKarma),
			[_Enums.TenKarma] = new ExtendedLocks.Numerical(_Enums.TenKarma),
			[_Enums.Ripple1_0] = new ExtendedLocks.Ripple(1.0f),
			[_Enums.Ripple1_5] = new ExtendedLocks.Ripple(1.5f),
			[_Enums.Ripple2_0] = new ExtendedLocks.Ripple(2.0f),
			[_Enums.Ripple2_5] = new ExtendedLocks.Ripple(2.5f),
			[_Enums.Ripple3_0] = new ExtendedLocks.Ripple(3.0f),
			[_Enums.Ripple3_5] = new ExtendedLocks.Ripple(3.5f),
			[_Enums.Ripple4_0] = new ExtendedLocks.Ripple(4.0f),
			[_Enums.Ripple4_5] = new ExtendedLocks.Ripple(4.5f),
			[_Enums.Ripple5_0] = new ExtendedLocks.Ripple(5.0f),
		};

		// load reinforced before alt because so that reinforcedalt is loaded properly
		foreach (Req reinforced in _Enums.reinforced)
		{
			Req baseReq = new(reinforced.value[..^REINFORCED_POSTFIX.Length], false);

			if (ExLocks.TryGetValue(baseReq, out LockData data))
			{ ExLocks[reinforced] = new ExtendedLocks.Reinforced(data); }

			else if (int.TryParse(baseReq.value, out _))
			{ ExLocks[reinforced] = new ExtendedLocks.Reinforced(new ExtendedLocks.Numerical(baseReq)); }

			else { LogError("ExtendedGates failed to register reinforced lock for " + reinforced.value[..^REINFORCED_POSTFIX.Length]); }
		}

		foreach (Req alt in _Enums.alt)
		{
			Req baseReq = new(alt.value[..^ALT_POSTFIX.Length], false);

			if (ExLocks.TryGetValue(baseReq, out LockData data))
			{ ExLocks[alt] = new ExtendedLocks.Alt(data); }

			else if (int.TryParse(baseReq.value, out _))
			{ ExLocks[alt] = new ExtendedLocks.Alt(new ExtendedLocks.Numerical(baseReq)); }

			else { LogError("ExtendedGates failed to register alt lock for " + alt.value[..^ALT_POSTFIX.Length]); }
		}

		foreach (Req txt in _Enums.txt)
		{
			Req baseReq = new(txt.value[..^TXT_POSTFIX.Length], false);

			if (ExLocks.TryGetValue(baseReq, out LockData data))
			{ ExLocks[txt] = new ExtendedLocks.Txt(data); }

			else if (int.TryParse(baseReq.value, out _))
			{ ExLocks[txt] = new ExtendedLocks.Txt(new ExtendedLocks.Numerical(baseReq)); }

			else { LogError("ExtendedGates failed to register txt lock for " + txt.value[..^TXT_POSTFIX.Length]); }
		}

		ExtraRequirements.Add(new ExtendedRequirements.CommsMark());
		ExtraRequirements.Add(new ExtendedRequirements.Glow());
		ExtraRequirements.Add(new ExtendedRequirements.KarmaReinforced());
		RegisterPassageExtraReqs();
	}

	private static void RegisterPassageExtraReqs()
	{
		foreach (string passageName in WinState.EndgameID.values.entries)
		{
			WinState.EndgameID passage = new WinState.EndgameID(passageName, false);
			if (!_RegisteredPassageReqs.Contains(passage))
			{
				ExtraRequirements.Add(new ExtendedRequirements.Passage(passage));
				_RegisteredPassageReqs.Add(passage);
			}
		}
	}

	internal static bool uwu;

	// 1.0 initial release
	// 1.1 13/06/2021 bugfix 6 karma at 5 cap; fix showing open side over karma for inregion minimap
	/// <summary>
	/// Returns a karma requirement for given gate, -1 by 
	/// </summary>
	public static int GetKarmaLevel(this Req req)
	{
		string str = req.ToString();
		if (str.EndsWith(ALT_POSTFIX))
		{
			str = str[..^ALT_POSTFIX.Length];
		}
		else if (str.EndsWith(TXT_POSTFIX))
		{
			str = str[..^TXT_POSTFIX.Length];
		}
		if (str.EndsWith(REINFORCED_POSTFIX))
		{
			str = str[..^REINFORCED_POSTFIX.Length];
		}
		if (int.TryParse(str, out int result))
		{ return result - 1; }

		return -1;
	}
	internal const string ALT_POSTFIX = "alt";
	internal const string TXT_POSTFIX = "txt";
	internal const string REINFORCED_POSTFIX = "reinforced";

	internal static void Enable()
	{
		On.RegionGateGraphics.DrawSprites += RegionGateGraphics_DrawSprites;
		On.RegionGateGraphics.Update += RegionGateGraphics_Update;
		On.VirtualMicrophone.NewRoom += VirtualMicrophone_NewRoom;

		IL.RegionGate.ctor += RegionGate_ctorIL;
		On.RegionGate.ctor += RegionGate_ctor;
		On.RegionGate.customKarmaGateRequirements += RegionGate_customKarmaGateRequirements;
		On.RegionGate.Update += RegionGate_Update;

		RegionGateMeetRequirementHook = new Hook(typeof(Gate).GetProperty("MeetRequirement",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod(), RegionGate_MeetRequirement);

		On.GateKarmaGlyph.DrawSprites += GateKarmaGlyph_DrawSprites;
		On.HUD.Map.GateMarker.ctor += GateMarker_ctor;
		On.HUD.Map.MapData.KarmaOfGate += MapData_KarmaOfGate;

		uwu = ModManager.ActiveMods.Any(x => x.id == "henpemaz_uwumod");
	}

	public static Hook? RegionGateMeetRequirementHook = null;

	internal static void Disable()
	{
		On.RegionGateGraphics.DrawSprites -= RegionGateGraphics_DrawSprites;
		On.RegionGateGraphics.Update -= RegionGateGraphics_Update;
		On.VirtualMicrophone.NewRoom -= VirtualMicrophone_NewRoom;

		IL.RegionGate.ctor -= RegionGate_ctorIL;
		On.RegionGate.ctor -= RegionGate_ctor;
		On.RegionGate.customKarmaGateRequirements -= RegionGate_customKarmaGateRequirements;
		On.RegionGate.Update -= RegionGate_Update;
		RegionGateMeetRequirementHook?.Undo();

		On.GateKarmaGlyph.DrawSprites -= GateKarmaGlyph_DrawSprites;
		On.HUD.Map.GateMarker.ctor -= GateMarker_ctor;
		On.HUD.Map.MapData.KarmaOfGate += MapData_KarmaOfGate;
	}
	#region misc fixes
	private static void RegionGateGraphics_DrawSprites(On.RegionGateGraphics.orig_DrawSprites orig, RegionGateGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		orig(self, sLeaser, rCam, timeStacker, camPos);
		if (self.gate is ElectricGate eg && eg.batteryLeft > 1.1f)
		{
			sLeaser.sprites[self.BatteryMeterSprite].scaleX = 420f;
			float num4 = !eg.batteryChanging ? 0f : 1f;
			sLeaser.sprites[self.BatteryMeterSprite].color = Color.Lerp(HSL2RGB(0.03f + 0.3f * Mathf.InverseLerp(1.1f, 30f, eg.batteryLeft) + UnityEngine.Random.value * (0.035f * num4 + 0.025f), 1f, (0.5f + UnityEngine.Random.value * 0.2f * num4) * Mathf.Lerp(1f, 0.25f, self.darkness)), self.blackColor, 0.5f);
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
	#endregion

	#region GATEHOOKS

	/// <summary>
	/// Loads karmaGate requirements
	/// </summary>
	private static void RegionGate_ctorIL(ILContext il)
	{
		try
		{
			var c = new ILCursor(il);

			if (c.TryGotoNext(MoveType.After,
				x => x.MatchLdarg(1),
				x => x.MatchCallvirt<Room>("get_abstractRoom"),
				x => x.MatchLdfld<AbstractRoom>("name"),
				x => x.MatchCall<string>("op_Equality"),
				x => x.MatchBrfalse(out _)))
			{
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldloc_0);
				c.Emit(OpCodes.Ldloc_2);
				c.Emit(OpCodes.Ldelem_Ref);
				c.EmitDelegate((Gate self, string line) =>
				{
					string[] array = Regex.Split(line, " : ");
					self.Tags().Clear();
					for (int i = 3; i < array.Length; i++)
					{ self.Tags().Add(array[i]); }
				});
			}
			else
			{
				LogError("[ExtendedGates] : failed to hook RegionGate.ctor");
			}
		}
		catch (Exception e) { LogError("[ExtendedGates] : ERROR WHEN IL HOOKING RegionGate.ctor\n" + e); }
	}

	private static void RegionGate_ctor(On.RegionGate.orig_ctor orig, Gate self, Room room)
	{
		orig(self, room);

		// Multi-use gates
		if (self.Tags().Contains("multi"))
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

		// Passage gates
		IEnumerable<string> passageTags = self.Tags().Where(x => x.StartsWith("Left-", StringComparison.InvariantCultureIgnoreCase) || x.StartsWith("Right-", StringComparison.InvariantCultureIgnoreCase));
		if (passageTags.Any() && !self.unlocked)
		{
			RegisterPassageExtraReqs(); // just in case more have been registered since

			List<(bool side, ExtraRequirement req)> extraReqs = [];
			_ExtraReqs.Add(self, extraReqs);

			int leftCount = 0;
			int rightCount = 0;
			foreach (var passageTag in passageTags)
			{
				bool side = passageTag.StartsWith("Left-", StringComparison.InvariantCultureIgnoreCase);
				string keyword = passageTag[(side ? 5 : 6)..].TrimEnd();
				foreach (ExtraRequirement extraReq in ExtraRequirements)
				{
					if (extraReq.BaseKeyword.Equals(keyword, StringComparison.InvariantCultureIgnoreCase))
					{
						extraReqs.Add((side, extraReq));
						if (side)
							leftCount++;
						else
							rightCount++;
					}
				}
			}

			for (int i = 0, l = 0, r = 0; i < extraReqs.Count; i++)
			{
				bool side = extraReqs[i].side;
				extraReqs[i].req.SpawnGateSymbol(self, side, side ? l : r, side ? leftCount : rightCount);
				if (side)
					l++;
				else
					r++;
			}
		}
	}

	private static void RegionGate_customKarmaGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, Gate self)
	{
		//OE gate lol (should also il hook Map.ctor, but that's just cosmetic and too much pain)
		if (self.Tags().Contains("OELock") && !self.customOEGateRequirements())
		{
			self.karmaRequirements[0] = MoreSlugcats.MoreSlugcatsEnums.GateRequirement.OELock;
			self.karmaRequirements[1] = MoreSlugcats.MoreSlugcatsEnums.GateRequirement.OELock;
		}

		if (ExtendedLocks.Construction.RegionGateUnderConstruction(self.room.abstractRoom.name, self.room.world.region.name))
		{
			//find alt construction
			Req altConstruction = _Enums.alt.FirstOrDefault(x => x.value[..^ALT_POSTFIX.Length] == _Enums.Construction.value);
			altConstruction ??= _Enums.Construction; //use default if alt isn't found (shouldn't happen)

			self.karmaRequirements[0] = _Enums.alt.Contains(self.karmaRequirements[0]) ? altConstruction : _Enums.Construction;
			self.karmaRequirements[1] = _Enums.alt.Contains(self.karmaRequirements[1]) ? altConstruction : _Enums.Construction;

			if (!hasSeenConstructionGateTutorial)
			{
				self.room.AddObject(new ConstructionGateTutorial(self.room));
			}
		}

		//guard clause, stop crashing when there are no locks!
		for (int i = 0; i < self.karmaRequirements.Length; i++)
		{
			if (self.karmaRequirements[i] == null || self.karmaRequirements[i].index == -1)
			{ self.karmaRequirements[i] = Req.OneKarma; }

			//alt art ig
			if (int.TryParse(self.karmaRequirements[i].value, out int v) && self.room?.game != null && DoesPlayerDeserveAltArt(self.room.game))
			{
				var alt = new Req(v + ALT_POSTFIX, false);
				if (alt.index != -1) { self.karmaRequirements[i] = alt; }
			}
		}

		//we actually want all (most) custom gates to be overwritten by orig when applicable
		orig(self);
	}

	/// <summary>
	/// Adds support for special gate types UwU
	/// </summary>
	private static void RegionGate_Update(On.RegionGate.orig_Update orig, Gate self, bool eu)
	{
		orig(self, eu);
		if (!self.room.game.IsStorySession) return; // Paranoid, just like in the base game

		switch (self.mode.ToString())
		{
		case nameof(Gate.Mode.ClosingAirLock):
			if (self.room.game.overWorld.worldLoader == null) // In-region gate support
			{
				self.waitingForWorldLoader = false;
			}
			break;
		case nameof(Gate.Mode.Closed): // Support for multi-usage gates
			if (self.EnergyEnoughToOpen) self.mode = Gate.Mode.MiddleClosed;
			break;
		}
	}
	public static bool RegionGate_MeetRequirement(Func<Gate, bool> orig, Gate self)
	{
		// Passages
		if (_ExtraReqs.TryGetValue(self, out List<(bool side, ExtraRequirement req)> extraReqs))
		{
			foreach ((bool side, ExtraRequirement extraReq) in extraReqs)
			{
				if (side == self.letThroughDir && !extraReq.CompletedAtGate(self)) return false;
			}
		}

		// Normal requirements
		Req req = self.karmaRequirements[self.letThroughDir ? 0 : 1];
		if (!ExLocks.ContainsKey(req)) return orig(self);

		return ExLocks[req].Requirement(self) || self.unlocked;
	}

	/// <summary>
	/// Image used in the gate room
	/// </summary>
	private static void GateKarmaGlyph_DrawSprites(On.GateKarmaGlyph.orig_DrawSprites orig, GateKarmaGlyph self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (self.symbolDirty) // redraw
		{
			bool canUseUnlockGates = self.room.game.GetStorySession.saveState.deathPersistentSaveData.CanUseUnlockedGates(self.room.game.GetStorySession.saveStateNumber);
			if (self.requirement != null && ExLocks.ContainsKey(self.requirement) && !(canUseUnlockGates && self.gate.unlocked))
			{
				string element = ExLocks[self.requirement].GateElementName(self);
				if (Futile.atlasManager.DoesContainElementWithName(element))
				{
					sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName(element);
				}
				else
				{
					LogWarning($"[ExtendedGates] couldn't find gate atlas element [{element}] for lock [{self.requirement.value}], using default");
					sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol0");
				}
				self.symbolDirty = false;
			}
		}
		orig(self, sLeaser, rCam, timeStacker, camPos);
	}

	private static void GateMarker_ctor(On.HUD.Map.GateMarker.orig_ctor orig, Map.GateMarker self, Map map, int room, Req req, bool showAsOpen)
	{
		orig(self, map, room, !(req != null && ExLocks.ContainsKey(req)) ? req : null, showAsOpen);

		// Custom icon
		if (req != null && ExLocks.ContainsKey(req))
		{
			string element = ExLocks[req].MapElementName(self);
			if (Futile.atlasManager.DoesContainElementWithName(element))
			{
				self.symbolSprite.element = Futile.atlasManager.GetElementWithName(element);
			}
			else
			{
				LogWarning($"[ExtendedGates] couldn't find map atlas element [{element}] for lock [{req.value}], using default");
				self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaNoRing-1");
			}
		}

		// Get tags for extra requirement map stuff
		(bool leftSide, string[] tags) = Tags(Custom.rainWorld.progression, map.mapData.world, map.mapData.world.GetAbstractRoom(room).name);
		if (tags.Length > 0)
		{
			IEnumerable<string> extraReqTags = tags.Where(x => x.StartsWith(leftSide ? "Left-" : "Right-", StringComparison.InvariantCultureIgnoreCase));
			if (extraReqTags.Any())
			{
				List<ExtraRequirement> extraReqs = [];
				foreach (string extraReqTag in extraReqTags)
				{
					string tag = leftSide ? extraReqTag[5..] : extraReqTag[6..];
					foreach (ExtraRequirement extraReq in ExtraRequirements)
					{
						if (extraReq.BaseKeyword.Equals(tag, StringComparison.InvariantCultureIgnoreCase))
						{
							extraReqs.Add(extraReq);
						}
					}
				}

				for (int i = 0; i < extraReqs.Count; i++)
				{
					extraReqs[i].SpawnMapSymbol(self, i, extraReqs.Count);
				}
			}
		}

		static (bool leftSide, string[] tags) Tags(PlayerProgression progression, World initWorld, string roomName)
		{
			if (initWorld.game != null && initWorld.game.IsStorySession && initWorld.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates != null && initWorld.game.GetStorySession.saveState.deathPersistentSaveData.CanUseUnlockedGates(initWorld.game.StoryCharacter))
			{
				for (int i = 0; i < initWorld.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates.Count; i++)
				{
					if (initWorld.game.GetStorySession.saveState.deathPersistentSaveData.unlockedGates[i] == roomName)
					{
						return (false, []);
					}
				}
			}
			for (int j = 0; j < progression.karmaLocks.Length; j++)
			{
				string[] split = Regex.Split(progression.karmaLocks[j], " : ");
				if (split[0] == roomName && split.Length >= 4)
				{
					// Figure out side
					bool leftSide = false;
					bool swap = split[3] == "SWAPMAPSYMBOL";
					if (Region.EquivalentRegion(Regex.Split(roomName, "_")[1], initWorld.region.name) != swap)
					{
						leftSide = true;
					}

					// Tags
					string[] tags = new string[split.Length - 3];
					for (int i = 3; i < split.Length; i++)
					{
						tags[i - 3] = split[i];
					}
					return (leftSide, [.. tags]);
				}
			}
			return (false, []);
		}
	}

	private static Req MapData_KarmaOfGate(On.HUD.Map.MapData.orig_KarmaOfGate orig, Map.MapData self, PlayerProgression progression, World initWorld, string roomName)
	{
		Req result = orig(self, progression, initWorld, roomName);

		if (ExtendedLocks.Construction.RegionGateUnderConstruction(roomName, initWorld.region.name))
		{
			return _Enums.Construction;
		}

		return result;
	}

	private static bool DoesPlayerDeserveAltArt(RainWorldGame game)
	{
		if (!ModOptions.AltGateArt.Value) return false;
		SaveState? saveState = (game.session as StoryGameSession)?.saveState;
		if (saveState == null) return false;
		WinState? winState = saveState.deathPersistentSaveData?.winState;
		if (winState == null) return false;

		float chieftain = game.session.creatureCommunities.LikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0);
		if (game.StoryCharacter == SlugcatStats.Name.Yellow)
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

		float lizfrend = game.session.creatureCommunities.LikeOfPlayer(CreatureCommunities.CommunityID.Lizards, -1, 0);
		if (lizfrend < -0.45f) return false;

		if (saveState.cycleNumber < 43) return false;

		WinState.BoolArrayTracker wanderer = (winState.GetTracker(WinState.EndgameID.Traveller, false) as WinState.BoolArrayTracker)!;
		if (wanderer == null || wanderer.progress.Count(c => c) < 5) return false;

		return true;
	}


	#endregion GATEHOOKS


}
