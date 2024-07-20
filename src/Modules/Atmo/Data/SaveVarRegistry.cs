using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RegionKit.Modules.Atmo.Data
{
	internal static class SaveVarRegistry
	{
		public enum DataSection
		{
			/// <summary>
			/// Normal data
			/// </summary>
			Normal,
			/// <summary>
			/// Death-persistent data
			/// </summary>
			Persistent,
			Global,
			Region,
			Volatile
		}

		public static ArgSet GetSaveData(World world, DataSection section)
		{
			if (!world.game.IsStorySession) return new();
			SaveState save = world.game.GetStorySession.saveState;
			return section switch
			{
				DataSection.Normal => NormalData(save),
				DataSection.Persistent => DeathPersistentData(save),
				DataSection.Global => GlobalData(save),
				DataSection.Region => RegionData(save, world.name),
				_ => new()
			};
		}

		public static ArgSet GlobalData(SaveState p) => _globalData.GetValue(p.progression.miscProgressionData, _ => new());
		public static ArgSet DeathPersistentData(SaveState p) => _deathPersistentData.GetValue(p.deathPersistentSaveData, _ => new());
		public static ArgSet NormalData(SaveState p) => _normalData.GetValue(p.miscWorldSaveData, _ => new());
		public static ArgSet RegionData(SaveState p, string regionName)
		{
			RegionState? regionState = p.regionStates.Where(r => r.regionName == regionName).FirstOrDefault();
			return regionState is not null ? _regionData.GetValue(regionState, _ => new()) : new();
		}

		private static ConditionalWeakTable<PlayerProgression.MiscProgressionData, ArgSet> _globalData = new();
		private static ConditionalWeakTable<DeathPersistentSaveData, ArgSet> _deathPersistentData = new();
		private static ConditionalWeakTable<RegionState, ArgSet> _regionData = new();
		private static ConditionalWeakTable<MiscWorldSaveData, ArgSet> _normalData = new();


		public static void LoadTableFromUnrecognized<T>(ConditionalWeakTable<T, ArgSet> table, T self, List<string> unrecognized) where T : class
		{
			for (int i = 0; i < unrecognized.Count; i++)
			{
				string[] array = Regex.Split(unrecognized[i], "<mwB>");
				if (array.Length >= 2 && array[0] == "AtmoSaveData")
				{
					SetTable(table, self, new ArgSet(array[1].Split(',')));
					unrecognized.RemoveAt(i);
					break;
				}
			}
		}
		public static void SetTable<T>(ConditionalWeakTable<T, ArgSet> table, T self, ArgSet set) where T : class
		{
			table.Remove(self);
			table.Add(self, set);
		}
		public static void SaveTableToUnrecognized<T>(ConditionalWeakTable<T, ArgSet> table, T self, List<string> unrecognized) where T : class
		{
			for (int i = 0; i < unrecognized.Count; i++)
			{
				string[] array = Regex.Split(unrecognized[i], "<mwB>");
				if (array.Length >= 2 && array[0] == "AtmoSaveData")
				{
					unrecognized.RemoveAt(i);
				}
			}
			string? save = TableToString(table, self);
			if (save is string s)
			{
				unrecognized.Add("AtmoSaveData" + "<mwB>" + s);
			}
		}
		public static string? TableToString<T>(ConditionalWeakTable<T, ArgSet> table, T self) where T : class
		{
			if (!table.TryGetValue(self, out var set) || set.Count == 0) return null;
			return string.Join(",", set.Select(a => a.Raw));
		}

		#region hooks
		public static void ApplyHooks()
		{
			On.MiscWorldSaveData.FromString += MiscWorldSaveData_FromString;
			On.MiscWorldSaveData.ToString += MiscWorldSaveData_ToString;

			On.DeathPersistentSaveData.FromString += DeathPersistentSaveData_FromString;
			On.DeathPersistentSaveData.ToString += DeathPersistentSaveData_ToString;

			On.RegionState.ctor += RegionState_ctor;
			On.RegionState.SaveToString += RegionState_SaveToString;

			On.PlayerProgression.MiscProgressionData.FromString += MiscProgressionData_FromString;
			On.PlayerProgression.MiscProgressionData.ToString += MiscProgressionData_ToString;
		}

		private static string MiscProgressionData_ToString(On.PlayerProgression.MiscProgressionData.orig_ToString orig, PlayerProgression.MiscProgressionData self)
		{
			SaveTableToUnrecognized(_globalData, self, self.unrecognizedSaveStrings);
			return orig(self);
		}

		private static void MiscProgressionData_FromString(On.PlayerProgression.MiscProgressionData.orig_FromString orig, PlayerProgression.MiscProgressionData self, string s)
		{
			LoadTableFromUnrecognized(_globalData, self, self.unrecognizedSaveStrings);
			orig(self, s); ;
		}

		private static string RegionState_SaveToString(On.RegionState.orig_SaveToString orig, RegionState self)
		{
			SaveTableToUnrecognized(_regionData, self, self.unrecognizedSaveStrings);
			return orig(self);
		}

		private static void RegionState_ctor(On.RegionState.orig_ctor orig, RegionState self, SaveState saveState, World world)
		{
			LoadTableFromUnrecognized(_regionData, self, self.unrecognizedSaveStrings);
			orig(self, saveState, world);
		}

		private static string DeathPersistentSaveData_ToString(On.DeathPersistentSaveData.orig_ToString orig, DeathPersistentSaveData self)
		{
			SaveTableToUnrecognized(_deathPersistentData, self, self.unrecognizedSaveStrings);
			return orig(self);
		}

		private static void DeathPersistentSaveData_FromString(On.DeathPersistentSaveData.orig_FromString orig, DeathPersistentSaveData self, string s)
		{
			LoadTableFromUnrecognized(_deathPersistentData, self, self.unrecognizedSaveStrings);
			orig(self, s);
		}

		private static string MiscWorldSaveData_ToString(On.MiscWorldSaveData.orig_ToString orig, MiscWorldSaveData self)
		{
			SaveTableToUnrecognized(_normalData, self, self.unrecognizedSaveStrings);
			return orig(self);
		}

		private static void MiscWorldSaveData_FromString(On.MiscWorldSaveData.orig_FromString orig, MiscWorldSaveData self, string s)
		{
			LoadTableFromUnrecognized(_normalData, self, self.unrecognizedSaveStrings);
			orig(self, s);
		}
		#endregion
	}
}
