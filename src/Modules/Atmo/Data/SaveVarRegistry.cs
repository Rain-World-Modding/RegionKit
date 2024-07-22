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
			Normal,
			Persistent,
			Global,
			Region,
			Volatile
		}

		public static void SetArg(World world, DataSection section, string? name, string value)
		{
			ArgSet set = GetSaveData(world, section);
			if (name != null) { set[name] = value; set[name]!.Name = name; }
			else set.Add(value);
		}
		public static Arg GetArg(World world, DataSection section, string? name, string? value = null)
		{
			ArgSet set = GetSaveData(world, section);
			if (name != null)
			{
				Arg? arg = set[name];
				if (arg == null)
				{
					arg = 0;
					set[name] = arg;
				}
				return arg;
			}

			return set.FirstOrDefault(n => n.Raw == value) ?? 0;
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
				DataSection.Volatile or _ => VolatileData(save)
			};
		}

		public static ArgSet VolatileData(SaveState p) => _volatileData.GetValue(p, _ => new());
		public static ArgSet GlobalData(SaveState p) => _globalData.GetValue(p.progression.miscProgressionData, _ => new());
		public static ArgSet DeathPersistentData(SaveState p) => _deathPersistentData.GetValue(p.deathPersistentSaveData, _ => new());
		public static ArgSet NormalData(SaveState p) => _normalData.GetValue(p.miscWorldSaveData, _ => new());
		public static ArgSet RegionData(SaveState p, string regionName)
		{
			RegionState? regionState = p.regionStates.Where(r => r.regionName == regionName).FirstOrDefault();
			return regionState is not null ? _regionData.GetValue(regionState, _ => new()) : new();
		}

		private static ConditionalWeakTable<SaveState, ArgSet> _volatileData = new();
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
					SetTable(table, self, new ArgSet(array[1].Split(','), null));
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
		public static void SaveArgSetToUnrecognized(ArgSet set, List<string> unrecognized)
		{
			UnityEngine.Debug.Log("saving set");
			for (int i = 0; i < unrecognized.Count; i++)
			{
				string[] array = Regex.Split(unrecognized[i], "<mwB>");
				if (array.Length >= 2 && array[0] == "AtmoSaveData")
				{
					unrecognized.RemoveAt(i);
				}
			}
			string? save = ArgSetString(set);
			if (save is string s)
			{
				UnityEngine.Debug.Log("saving set: " + save);
				unrecognized.Add("AtmoSaveData" + "<mwB>" + s);
			}
		}

		public static string? ArgSetString(ArgSet set)
		{
			if (set.Count == 0) return null;
			return string.Join(",", set.Select(a => a.Name != null ? a.Name + "=" + a.Raw : a.Raw));
		}

		#region hooks
		public static void ApplyHooks()
		{
			On.MiscWorldSaveData.FromString += MiscWorldSaveData_FromString;
			On.MiscWorldSaveData.ToString += MiscWorldSaveData_ToString;

			On.DeathPersistentSaveData.FromString += DeathPersistentSaveData_FromString;
			On.DeathPersistentSaveData.SaveToString += DeathPersistentSaveData_SaveToString; ;

			On.RegionState.ctor += RegionState_ctor;
			On.RegionState.SaveToString += RegionState_SaveToString;

			On.PlayerProgression.MiscProgressionData.FromString += MiscProgressionData_FromString;
			On.PlayerProgression.MiscProgressionData.ToString += MiscProgressionData_ToString;
		}


		private static string MiscProgressionData_ToString(On.PlayerProgression.MiscProgressionData.orig_ToString orig, PlayerProgression.MiscProgressionData self)
		{
			SaveArgSetToUnrecognized(_globalData.GetValue(self, _ => new()), self.unrecognizedSaveStrings);
			return orig(self);
		}

		private static void MiscProgressionData_FromString(On.PlayerProgression.MiscProgressionData.orig_FromString orig, PlayerProgression.MiscProgressionData self, string s)
		{
			orig(self, s);
			LoadTableFromUnrecognized(_globalData, self, self.unrecognizedSaveStrings);
		}

		private static string RegionState_SaveToString(On.RegionState.orig_SaveToString orig, RegionState self)
		{
			SaveArgSetToUnrecognized(_regionData.GetValue(self, _ => new()), self.unrecognizedSaveStrings);
			return orig(self);
		}

		private static void RegionState_ctor(On.RegionState.orig_ctor orig, RegionState self, SaveState saveState, World world)
		{
			orig(self, saveState, world);
			LoadTableFromUnrecognized(_regionData, self, self.unrecognizedSaveStrings);
		}

		private static string DeathPersistentSaveData_SaveToString(On.DeathPersistentSaveData.orig_SaveToString orig, DeathPersistentSaveData self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
		{
			SaveArgSetToUnrecognized(_deathPersistentData.GetValue(self, _ => new()), self.unrecognizedSaveStrings);
			return orig(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);
		}
		private static void DeathPersistentSaveData_FromString(On.DeathPersistentSaveData.orig_FromString orig, DeathPersistentSaveData self, string s)
		{
			orig(self, s);
			LoadTableFromUnrecognized(_deathPersistentData, self, self.unrecognizedSaveStrings);
		}

		private static string MiscWorldSaveData_ToString(On.MiscWorldSaveData.orig_ToString orig, MiscWorldSaveData self)
		{
			SaveArgSetToUnrecognized(_normalData.GetValue(self, _ => new()), self.unrecognizedSaveStrings);
			return orig(self);
		}

		private static void MiscWorldSaveData_FromString(On.MiscWorldSaveData.orig_FromString orig, MiscWorldSaveData self, string s)
		{
			orig(self, s);
			LoadTableFromUnrecognized(_normalData, self, self.unrecognizedSaveStrings);
		}
		#endregion
	}
}
