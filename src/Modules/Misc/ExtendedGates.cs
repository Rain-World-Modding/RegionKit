//extended gates by Henpemaz

//using System.IO;
//using System.Reflection;
//using System.Text.RegularExpressions;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Gate = RegionGate;
using Req = RegionGate.GateRequirement;

namespace RegionKit.Modules.Misc;
/// <summary>
/// Adds more options for gate requirements
/// </summary>
public static class ExtendedGates
{
	public static class ExtendedLocks
	{
		public interface LockData
		{
			public string GateElementName(GateKarmaGlyph glyph);
			public string MapElementName(Map.GateMarker gateMarker);
			public bool Requirement(RegionGate regionGate);
		}

		public class Open : LockData
		{
			public string GateElementName(GateKarmaGlyph glyph) => "gateSymbol0";
			public string MapElementName(Map.GateMarker gateMarker) => "smallKarmaOpen";
			public bool Requirement(Gate regionGate) => true;
		}
		public class Forbidden : LockData
		{
			public string GateElementName(GateKarmaGlyph glyph) => "gateSymbolForbidden";
			public string MapElementName(Map.GateMarker gateMarker) => "smallKarmaForbidden";
			public bool Requirement(Gate gate) => false;
		}
		public class TenReinforced : LockData
		{
			public string GateElementName(GateKarmaGlyph glyph) => "gateSymbol10reinforced";
			public string MapElementName(Map.GateMarker gateMarker) => "smallKarma10reinforced";
			public bool Requirement(Gate gate) => (gate.room.game.Players[0].realizedCreature is Player p) && p.Karma == 9 && p.KarmaIsReinforced;
		}
		public class ComsMark : LockData
		{
			public string GateElementName(GateKarmaGlyph glyph) => "gateSymbolComsmark";
			public string MapElementName(Map.GateMarker gateMarker) => "smallKarmaComsmark";
			public bool Requirement(Gate gate) => gate.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark;
		}
		public class Glow : LockData
		{
			public string GateElementName(GateKarmaGlyph glyph) => "gateSymbolGlow";
			public string MapElementName(Map.GateMarker gateMarker) => "smallKarmaGlow";
			public bool Requirement(Gate gate) => gate.room.game.GetStorySession.saveState.theGlow;
		}
		public class UWU : LockData
		{
			public string GateElementName(GateKarmaGlyph glyph) => "gateSymbolUwu";
			public string MapElementName(Map.GateMarker gateMarker) => "smallKarmaUwu";
			public bool Requirement(Gate gate) => uwu != null;
		}
		public class Numerical : LockData
		{
			public Numerical(Req req) => this.req = req;

			public Req req;
			public virtual string GateElementName(GateKarmaGlyph glyph)
			{
				var intreq = req.GetKarmaLevel();
				if (intreq >= 5)
				{
					int cap = (glyph.room.game.session as StoryGameSession)!.saveState.deathPersistentSaveData.karmaCap;
					if (cap < intreq) cap = Mathf.Max(6, intreq);
					return "gateSymbol" + (intreq + 1).ToString() + "-" + (cap + 1).ToString(); // Custom, 1-indexed
				}
				else
				{ return "gateSymbol" + (intreq + 1).ToString(); }
			}
			public virtual string MapElementName(Map.GateMarker gateMarker)
			{
				int karma = req.GetKarmaLevel();
				if (karma > 4)
				{
					int? cap = gateMarker.map.hud.rainWorld.progression?.currentSaveState?.deathPersistentSaveData?.karmaCap;
					if (!cap.HasValue || cap.Value < karma) cap = Mathf.Max(6, karma);
					return "smallKarma" + karma.ToString() + "-" + cap.Value.ToString(); // Vanilla, zero-indexed
				}
				else
				{
					return "smallKarmaNoRing" + karma;
				}
			}
			public bool Requirement(Gate gate)
			{
				AbstractCreature firstAlivePlayer = gate.room.game.FirstAlivePlayer;
				if (gate.room.game.Players.Count == 0 || firstAlivePlayer == null || (firstAlivePlayer.realizedCreature == null && ModManager.CoopAvailable))
				{ return false; }

				Player player;
				if (ModManager.CoopAvailable && gate.room.game.AlivePlayers.Count > 0)
				{ player = (firstAlivePlayer.realizedCreature as Player)!; }
				else
				{ player = (gate.room.game.Players[0].realizedCreature as Player)!; }

				int num = player.Karma;
				if (ModManager.MSC && player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer && player.grasps.Length != 0)
				{
					for (int i = 0; i < player.grasps.Length; i++)
					{
						if (player.grasps[i] != null && player.grasps[i].grabbedChunk != null && player.grasps[i].grabbedChunk.owner is Scavenger)
						{
							num = (gate.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma + (player.grasps[i].grabbedChunk.owner as Scavenger).abstractCreature.karmicPotential;
							break;
						}
					}
				}

				return num >= req.GetKarmaLevel();
			}
		}
		public class NumericalAlt : Numerical
		{
			public NumericalAlt(Req req) : base(req) { }
			public override string GateElementName(GateKarmaGlyph glyph) => base.GateElementName(glyph) + "alt";
		}
		internal record DelegateDriven(
			Req req,
			string gateElementName,
			string mapElementName,
			Func<Gate, bool> requirement) : LockData
		{
			public string GateElementName(GateKarmaGlyph glyph) => gateElementName;
			public string MapElementName(Map.GateMarker gateMarker) => mapElementName;
			public bool Requirement(Gate regionGate) => requirement(regionGate);
		}
	}

	public static Dictionary<Req, ExtendedLocks.LockData> ExLocks = new();

	private static readonly ConditionalWeakTable<RegionGate, List<string>> _Tags = new();
	public static List<string> Tags(this RegionGate p) => _Tags.GetValue(p, _ => new());

	public static void InitExLocks()
	{
		ExLocks = new()
		{
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
		};

		foreach (Req req in _Enums.alt)
		{ ExLocks[req] = new ExtendedLocks.NumericalAlt(req); }
	}

	internal const string Version = "1.4";
	internal const string author = "Henpemaz";
	static Type? uwu;
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
		if (int.TryParse(str, out int result))
		{ return result - 1; }

		return -1;
	}
	internal const string ALT_POSTFIX = "alt";

	internal static void Enable()
	{
		On.RegionGateGraphics.DrawSprites += RegionGateGraphics_DrawSprites;
		On.RegionGateGraphics.Update += RegionGateGraphics_Update;
		On.VirtualMicrophone.NewRoom += VirtualMicrophone_NewRoom;

		IL.RegionGate.ctor += RegionGate_ctorIL;
		On.RegionGate.ctor += RegionGate_ctor;
		On.RegionGate.customKarmaGateRequirements += RegionGate_customKarmaGateRequirements;
		On.RegionGate.Update += RegionGate_Update;

		RegionGateMeetRequirementHook = new Hook(typeof(RegionGate).GetProperty("MeetRequirement",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod(), RegionGate_MeetRequirement);

		On.GateKarmaGlyph.DrawSprites += GateKarmaGlyph_DrawSprites;
		On.HUD.Map.GateMarker.ctor += GateMarker_ctor;

		uwu = null;
		foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (asm.GetName().Name == "UwUMod")
			{
				uwu = asm.GetType("UwUMod.UwUMod");
			}
		}
	}

	public static Hook RegionGateMeetRequirementHook = null;

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
	}
	#region misc fixes
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
	#endregion

	#region GATEHOOKS

	/// <summary>
	/// Loads karmaGate requirements
	/// </summary>
	private static void RegionGate_ctorIL(MonoMod.Cil.ILContext il)
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
				c.EmitDelegate((RegionGate self, string line) =>
				{
					string[] array = Regex.Split(line, " : ");
					self.Tags().Clear();
					for (int i = 3; i < array.Length; i++)
					{ self.Tags().Add(array[i]); }
				});
			}
			else
			{
				__logger.LogError("[ExtendedGates] : failed to hook RegionGate.ctor");
			}
		}
		catch (Exception e) { __logger.LogError("[ExtendedGates] : ERROR WHEN IL HOOKING RegionGate.ctor\n" + e); }
	}

	private static void RegionGate_ctor(On.RegionGate.orig_ctor orig, Gate self, Room room)
	{
		orig(self, room);
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
	}

	private static void RegionGate_customKarmaGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, Gate self)
	{
		//OE gate lol (should also il hook Map.ctor, but that's just cosmetic and too much pain)
		if (self.Tags().Contains("OELock") && !self.customOEGateRequirements())
		{
			self.karmaRequirements[0] = MoreSlugcats.MoreSlugcatsEnums.GateRequirement.OELock;
			self.karmaRequirements[1] = MoreSlugcats.MoreSlugcatsEnums.GateRequirement.OELock;
		}

		//we actually want all (most) custom gates to be overwritten by orig when applicable
		orig(self);

		//guard clause, stop crashing when there are no locks!
		for (int i = 0; i < self.karmaRequirements.Length; i++)
		{
			if (self.karmaRequirements[i] == null || self.karmaRequirements[i].index == -1)
			{ self.karmaRequirements[i] = Req.OneKarma; }

			//alt art ig
			if (int.TryParse(self.karmaRequirements[i].value, out int v) && self.room?.game != null && DoesPlayerDeserveAltArt(self.room.game))
			{
				Req alt = new Req(v + ALT_POSTFIX, false);
				if (alt.index != -1) { self.karmaRequirements[i] = alt; }
			}
		}
	}

	/// <summary>
	/// Adds support for special gate types UwU
	/// </summary>
	private static void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
	{
		orig(self, eu);
		if (!self.room.game.IsStorySession) return; // Paranoid, just like in the base game

		switch (self.mode.ToString())
		{
		case nameof(RegionGate.Mode.ClosingAirLock):
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
	public static bool RegionGate_MeetRequirement(Func<RegionGate, bool> orig, RegionGate self)
	{
		Req req = self.karmaRequirements[self.letThroughDir ? 0 : 1];
		if (!ExLocks.ContainsKey(req)) return orig(self);

		return ExLocks[req].Requirement(self) || self.unlocked;
	}

	/// <summary>
	/// Image used in the gate room
	/// </summary>
	private static void GateKarmaGlyph_DrawSprites(On.GateKarmaGlyph.orig_DrawSprites orig, GateKarmaGlyph self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
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
					__logger.LogWarning($"[ExtendedGates] couldn't find gate atlas element [{element}] for lock [{self.requirement.value}], using default");
					sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("gateSymbol0");
				}
				self.symbolDirty = false;
			}
		}
		orig(self, sLeaser, rCam, timeStacker, camPos);
	}

	private static void GateMarker_ctor(On.HUD.Map.GateMarker.orig_ctor orig, HUD.Map.GateMarker self, HUD.Map map, int room, RegionGate.GateRequirement req, bool showAsOpen)
	{
		orig(self, map, room, !(req != null && ExLocks.ContainsKey(req)) ? req : null, showAsOpen);

		if (req != null && ExLocks.ContainsKey(req))
		{
			string element = ExLocks[req].MapElementName(self);
			if (Futile.atlasManager.DoesContainElementWithName(element))
			{
				self.symbolSprite.element = Futile.atlasManager.GetElementWithName(element);
			}
			else
			{
				__logger.LogWarning($"[ExtendedGates] couldn't find map atlas element [{element}] for lock [{req.value}], using default");
				self.symbolSprite.element = Futile.atlasManager.GetElementWithName("smallKarmaNoRing-1");
			}
		}
	}

	private static bool DoesPlayerDeserveAltArt(RainWorldGame game)
	{
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
