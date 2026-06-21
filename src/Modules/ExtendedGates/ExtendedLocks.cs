using System.Globalization;
using HUD;
using Gate = RegionGate;
using Req = RegionGate.GateRequirement;

namespace RegionKit.Modules.ExtendedGates
{
	public static class ExtendedLocks
	{

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
			public bool Requirement(Gate gate) => gate.room.game.Players[0].realizedCreature is Player p && p.Karma == 9 && p.KarmaIsReinforced;
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
			public bool Requirement(Gate gate) => ExtendedGates.uwu;
		}
		public class Construction : LockData
		{
			public string GateElementName(GateKarmaGlyph glyph) => "gateSymbolConstruction";
			public string MapElementName(Map.GateMarker gateMarker) => "smallKarmaConstruction";
			public bool Requirement(Gate gate) => false;

			public static bool RegionGateUnderConstruction(string gateName, string currentRegionName)
			{
				string[] arrayName = gateName.Split('_');

				string otherRegionName = "ERROR!";
				bool regionNameFound = false;
				if (arrayName.Length == 3)
				{
					for (int i = 1; i < 3; i++)
					{
						if (Region.EquivalentRegion(arrayName[i], currentRegionName))
						{
							regionNameFound = true;
						}
						else { otherRegionName = arrayName[i]; }
					}
				}
				return regionNameFound == true && otherRegionName != "ERROR!" && !Region.GetFullRegionOrder().Contains(otherRegionName);
			}
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
				if (gate.room.game.Players.Count == 0 || firstAlivePlayer == null || firstAlivePlayer.realizedCreature == null && ModManager.CoopAvailable)
				{ return false; }

				Player player;
				if (ModManager.CoopAvailable && gate.room.game.AlivePlayers.Count > 0)
				{ player = (firstAlivePlayer.realizedCreature as Player)!; }
				else
				{ player = (gate.room.game.Players[0].realizedCreature as Player)!; }

				int karma = player.Karma;
				if (ModManager.MSC && player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer && player.grasps.Length != 0)
				{
					for (int i = 0; i < player.grasps.Length; i++)
					{
						if (player.grasps[i] != null && player.grasps[i].grabbedChunk != null && player.grasps[i].grabbedChunk.owner is Scavenger scav)
						{
							karma = ((StoryGameSession)gate.room.game.session).saveState.deathPersistentSaveData.karma + scav.abstractCreature.karmicPotential;
							break;
						}
					}
				}

				return karma >= req.GetKarmaLevel();
			}
		}

		public class Ripple : LockData
		{
			private readonly float req;

			public Ripple(float req)
			{
				this.req = req;
			}

			public string GateElementName(GateKarmaGlyph glyph) => "gateSymbolRipple" + req.ToString("#.0", CultureInfo.InvariantCulture);
			public string MapElementName(Map.GateMarker gateMarker) => "smallRipple" + req.ToString("#.0", CultureInfo.InvariantCulture);

			public bool Requirement(Gate regionGate)
			{
				float rippleLevel = regionGate.room.game.GetStorySession.saveState.deathPersistentSaveData.rippleLevel;
				return rippleLevel >= req;
			}
		}

		public class Reinforced : LockData
		{
			protected LockData wrapped;
			public Reinforced(LockData wrapped) { this.wrapped = wrapped; }
			public string GateElementName(GateKarmaGlyph glyph) => wrapped.GateElementName(glyph) + ExtendedGates.REINFORCED_POSTFIX;

			public string MapElementName(Map.GateMarker gateMarker) => wrapped.MapElementName(gateMarker) + ExtendedGates.REINFORCED_POSTFIX;

			public bool Requirement(Gate regionGate) => wrapped.Requirement(regionGate) && regionGate.room.game.Players[0].realizedCreature is Player p && p.KarmaIsReinforced;
		}

		public class Alt : LockData
		{
			protected LockData wrapped;
			public Alt(LockData wrapped) { this.wrapped = wrapped; }
			public virtual string GateElementName(GateKarmaGlyph glyph) => wrapped.GateElementName(glyph) + ExtendedGates.ALT_POSTFIX;

			public string MapElementName(Map.GateMarker gateMarker) => wrapped.MapElementName(gateMarker);

			public bool Requirement(Gate regionGate) => wrapped.Requirement(regionGate);
		}
		public class Txt : Alt
		{
			public Txt(LockData wrapped) : base(wrapped) { }
			public override string GateElementName(GateKarmaGlyph glyph) => wrapped.GateElementName(glyph) + ExtendedGates.TXT_POSTFIX;
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
}
