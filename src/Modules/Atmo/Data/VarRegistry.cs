using RegionKit.Modules.Atmo.Helpers;
using static RegionKit.Modules.Atmo.Atmod;
using IO = System.IO;

using NamedVars = System.Collections.Generic.Dictionary<string, RegionKit.Modules.Atmo.Data.Arg>;
using Save = RegionKit.Modules.Atmo.Helpers.VT<int, SlugcatStats.Name>;
using SerDict = System.Collections.Generic.Dictionary<string, object>;

namespace RegionKit.Modules.Atmo.Data;
/// <summary>
/// Allows accessing a pool of variables, global or save-specific.
/// You can use <see cref="GetVar"/> to fetch variables by name.
/// Variables are returned as <see cref="Arg"/>s (NOTE: they may be mutable!
/// Be careful not to mess the values up, especially if you are requesting
/// a variable made by someone else). Use 'p_' prefix to look for a death-persistent variable,
/// and 'g_' prefix to look for a global variable.
/// <seealso cref="GetVar"/> for more details on fetching, prefixes 
/// and some examples.
/// <para>
/// <see cref="VarRegistry"/>'s primary purpose is being used by arguments in .atmo files
/// written like '$varname'. Doing that will automatically call <see cref="GetVar"/> 
/// for given name, current slot and current character. 
/// </para>
/// </summary>
public static partial class VarRegistry
{
	#region fields / consts / props
	/// <summary>
	/// <see cref="DeathPersistentSaveData"/> hash to slugcat number.
	/// </summary>
	private static readonly Dictionary<int, SlugcatStats.Name> __DPSD_Slug = new();
	internal const string PREFIX_VOLATILE = "v_";
	internal const string PREFIX_GLOBAL = "g_";
	internal const string PREFIX_PERSISTENT = "p_";
	//internal const string PREFIX_FMT = "$FMT_";
	internal static Arg __Defarg = string.Empty;
	/// <summary>
	/// Var sets per save. Key is saveslot number + character index.
	/// </summary>
	public static readonly Dictionary<Save, VarSet> VarsPerSave = new();
	/// <summary>
	/// Global vars per saveslot. key is saveslot number
	/// </summary>
	public static readonly Dictionary<int, NamedVars> VarsGlobal = new();
	/// <summary>
	/// Volatile variables are not serialized.
	/// </summary>
	public static readonly NamedVars VarsVolatile = new();
	#endregion
	#region lifecycle
	internal static void __Clear()
	{
		LogDebug("Clear VarRegistry hooks");
		try
		{
			On.SaveState.LoadGame -= __ReadNormal;
			On.SaveState.SaveToString -= __WriteNormal;

			On.DeathPersistentSaveData.ctor -= __RegDPSD;
			On.DeathPersistentSaveData.FromString -= __ReadPers;
			On.DeathPersistentSaveData.SaveToString -= __WritePers;

			On.PlayerProgression.WipeAll -= __WipeAll;
			On.PlayerProgression.WipeSaveState -= __WipeSavestate;
			foreach (int slot in VarsGlobal.Keys)
			{
				__WriteGlobal(slot);
			}
		}
		catch (Exception ex)
		{
			LogFatal(__ErrorMessage(site: Site.Clear, message: "Unhandled exception", ex: ex));
		}
	}
	internal static void __Init()
	{
		LogDebug("Init VarRegistry hooks");
		try
		{
			__FillSpecials();

			On.SaveState.LoadGame += __ReadNormal;
			On.SaveState.SaveToString += __WriteNormal;

			On.DeathPersistentSaveData.ctor += __RegDPSD;
			On.DeathPersistentSaveData.FromString += __ReadPers;
			On.DeathPersistentSaveData.SaveToString += __WritePers;

			On.PlayerProgression.WipeAll += __WipeAll;
			On.PlayerProgression.WipeSaveState += __WipeSavestate;
		}
		catch (Exception ex)
		{
			LogFatal(__ErrorMessage(Site.Init, "Unhandled exception", ex));
		}

	}
	#region hooks
	private static void __TrackCycleLength(On.RainCycle.orig_ctor orig, RainCycle self, World world, float minutes)
	{
		orig(self, world, minutes);
		__SpecialVars[SpVar.cycletime].F32 = minutes * 60f;
		VerboseLog($"Setting $cycletime to {__SpecialVars[SpVar.cycletime]}");
	}

	private static void __WipeSavestate(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name @char)
	{
		Save save = MakeSD(__CurrentSaveslot ?? -1, @char);
		try
		{
			LogDebug($"Wiping data for save {save}");
			__EraseData(save);
		}
		catch (Exception ex)
		{
			LogError(__ErrorMessage(Site.HookWipe, $"Failed to wipe saveslot {save}", ex));
		}
		orig(self, @char);
	}
	private static void __WipeAll(On.PlayerProgression.orig_WipeAll orig, PlayerProgression self)
	{
		int ss = __CurrentSaveslot ?? -1;
		try
		{
			LogDebug($"Wiping data for slot {ss}");
			foreach ((Save save, VarSet set) in VarsPerSave)
			{
				if (save.a != ss) continue;
				__EraseData(save);
				foreach (DataSection sec in Enum.GetValues(typeof(DataSection)))
				{
					set._FillFrom(null, sec);
				}
			}
		}
		catch (Exception ex)
		{
			LogError(__ErrorMessage(Site.HookWipe, $"Failed to wipe all saves for slot {ss}", ex));
		}
		orig(self);
	}

	private static void __RegDPSD(On.DeathPersistentSaveData.orig_ctor orig, DeathPersistentSaveData self, SlugcatStats.Name slugcat)
	{
		orig(self, slugcat);
		__DPSD_Slug.Set(self.GetHashCode(), slugcat);
	}
	private static void __ReadPers(On.DeathPersistentSaveData.orig_FromString orig, DeathPersistentSaveData self, string s)
	{
		try
		{
			int? ss = __CurrentSaveslot;
			//int ch = CurrentCharacter ?? -1;
			if (ss is null)
			{
				LogError(__ErrorMessage(Site.HookPersistent, "Could not find current saveslot", null));
				return;
			}
			Save save = MakeSD(ss.Value, __DPSD_Slug[self.GetHashCode()]);
			LogDebug($"Attempting to load persistent vars for save {save}");
			SerDict? data = __TryReadData(save, DataSection.Persistent);
			if (data is null) LogDebug("Could not load file, varset will be empty");
			VarsPerSave
				.EnsureAndGet(save, () => new(save))
				._FillFrom(data ?? new(), DataSection.Persistent);
		}
		catch (Exception ex)
		{
			LogError(__ErrorMessage(Site.HookPersistent, "Error on read", ex));
		}
		orig(self, s);
	}
	private static string __WritePers(On.DeathPersistentSaveData.orig_SaveToString orig, DeathPersistentSaveData self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
	{
		try
		{
			int? ss = __CurrentSaveslot;
			if (ss is null)
			{
				LogError(__ErrorMessage(Site.HookPersistent, "Could not find current saveslot", null));
				goto done;
			}
			Save save = MakeSD(ss.Value, __DPSD_Slug[self.GetHashCode()]);
			LogDebug($"Attempting to write persistent vars for {save}");
			SerDict? data = VarsPerSave
				.EnsureAndGet(save, () => new(save))
				._GetSer(DataSection.Normal);
			__TryWriteData(save, DataSection.Persistent, data);
		}
		catch (Exception ex)
		{
			LogError(__ErrorMessage(Site.HookPersistent, "Error on write", ex));
		}
	done:
		return orig(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);
	}

	private static void __ReadNormal(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
	{
		orig(self, str, game);
		try
		{
			int? ss = __CurrentSaveslot;
			if (ss is null)
			{
				LogError(__ErrorMessage(Site.HookNormal, "Could not find current saveslot", null));
				return;
			}
			Save save = MakeSD(ss.Value, self.saveStateNumber);
			LogDebug($"Attempting to load non-persistent vars for save {save}");
			SerDict? data = __TryReadData(save, DataSection.Normal);
			if (data is null) LogDebug("Could not load file, varset will be empty");
			VarsPerSave
				.EnsureAndGet(save, () => new(save))
				._FillFrom(data ?? new(), DataSection.Normal);
		}
		catch (Exception ex)
		{
			LogError(__ErrorMessage(Site.HookNormal, "Error on read", ex));
		}
	}
	private static string __WriteNormal(On.SaveState.orig_SaveToString orig, SaveState self)
	{
		try
		{
			int? ss = __CurrentSaveslot;
			if (ss is null)
			{
				LogError(__ErrorMessage(Site.HookNormal, "Could not find current saveslot", null));
				goto done;
			}
			Save save = MakeSD(ss.Value, self.saveStateNumber);
			SerDict? data = VarsPerSave
				.EnsureAndGet(save, () => new(save))
				._GetSer(DataSection.Normal);
			__TryWriteData(save, DataSection.Normal, data);
		}
		catch (Exception ex)
		{
			LogError(__ErrorMessage(Site.HookNormal, "Error on write", ex));
		}
	done:
		return orig(self);
	}
	#endregion hooks
	#endregion lifecycle
	#region methods
	/// <summary>
	/// Fetches a stored variable. Creates a new one if does not exist. You can use prefixes to request death-persistent ("p_") and global ("g_") variables. Persistent variables follow the lifecycle of <see cref="DeathPersistentSaveData"/>; global variables are shared across the entire saveslot.
	/// </summary>
	/// <param name="name">Name of the variable, with prefix if needed. Must not be null.</param>
	/// <param name="saveslot">Save slot to look up data from (<see cref="RainWorld"/>.options.saveSlot for current)</param>
	/// <param name="character">Current character. 0 for survivor, 1 for monk, 2 for hunter.</param>
	/// <returns>Variable requested; if there was no variable with given name before, GetVar creates a blank one from an empty string.</returns>
	public static Arg GetVar(string name, int saveslot, SlugcatStats.Name character = null!)
	{
		character ??= __slugnameNotFound;
		BangBang(name, nameof(name));
		if (name.StartsWith("$") && GetMetaFunction(name.Substring(1), saveslot, character) is Arg meta)
		{
			return meta;
		}
		if (GetSpecial(name) is Arg spec)
		{
			return spec;
		}
		if (name.StartsWith(PREFIX_GLOBAL))
		{
			name = name.Substring(PREFIX_GLOBAL.Length);
			LogDebug($"Reading global var {name} for slot {saveslot}");
			return VarsGlobal
				.EnsureAndGet(saveslot, () =>
				{
					LogDebug("No global record found, creating");
					return __ReadGlobal(saveslot);
				})
				.EnsureAndGet(name, static () => __Defarg);
		}
		else if (name.StartsWith(PREFIX_VOLATILE))
		{
			name = name.Substring(PREFIX_VOLATILE.Length);
			return VarsVolatile
				.EnsureAndGet(name, static () => __Defarg);
		}
		Save save = MakeSD(saveslot, character);
		return VarsPerSave
			.EnsureAndGet(save, () => new(save))
			.GetVar(name);
	}
	#region filemanip
	internal static NamedVars __ReadGlobal(int slot)
	{
		NamedVars res = new();
		IO.FileInfo fi = new(GlobalFile(slot));
		if (!fi.Exists) return res;
		try
		{
			using IO.StreamReader reader = fi.OpenText();
			SerDict json = reader.ReadToEnd().dictionaryFromJson();
			foreach ((string name, object val) in json)
			{
				res.Add(name, val?.ToString() ?? string.Empty);
			}
		}
		catch (IO.IOException ex)
		{
			LogError(__ErrorMessage(Site.ReadData, $"Could not read global vars for slot {slot}", ex));
		}
		return res;
	}
	internal static void __WriteGlobal(int slot)
	{
		IO.DirectoryInfo dir = new(SaveFolder(new(slot, __slugnameNotFound)));
		IO.FileInfo fi = new(GlobalFile(slot));
		try
		{
			if (!dir.Exists) dir.Create();
			fi.Refresh();
			NamedVars dict = VarsGlobal.EnsureAndGet(slot, static () => new());

			using IO.StreamWriter writer = fi.CreateText();
			LogDebug($"Writing global vars for slot {slot}, {fi.FullName}");
			writer.Write(Json.Serialize(dict));
		}
		catch (IO.IOException ex)
		{
			LogError(__ErrorMessage(Site.WriteData, $"Could not write global vars for slot {slot}", ex));
		}
	}
	internal static SerDict? __TryReadData(Save save, DataSection section)
	{
		IO.FileInfo fi = new(SaveFile(save, section));
		if (!fi.Exists) return null;
		try
		{
			using IO.StreamReader reader = fi.OpenText();
			return reader.ReadToEnd().dictionaryFromJson();
		}
		catch (Exception ex)
		{
			LogError(__ErrorMessage(Site.ReadData, $"error reading {section} for slot {save} ({fi.FullName})", ex));
			return null;
		}
	}
	internal static bool __TryWriteData(Save save, DataSection section, SerDict dict)
	{
		IO.DirectoryInfo dir = new(SaveFolder(save));
		IO.FileInfo file = new(SaveFile(save, section));
		try
		{
			if (!dir.Exists) dir.Create();
			file.Refresh();
			using IO.StreamWriter writer = file.CreateText();
			writer.Write(Json.Serialize(dict));
			return true;
		}
		catch (Exception ex)
		{
			LogError(__ErrorMessage(Site.WriteData, $"error writing {section} for slot {save} ({file.FullName})", ex));
			return false;
		}
	}
	internal static void __EraseData(in Save save)
	{
		foreach (DataSection sec in Enum.GetValues(typeof(DataSection)))
		{
			try
			{
				IO.FileInfo fi = new(SaveFile(save, sec));
				if (fi.Exists) fi.Delete();
			}
			catch (IO.IOException ex)
			{
				LogError(__ErrorMessage(Site.WipeData, $"Error erasing file for {save}", ex));
			}
		}
	}
	#endregion filemanip
	#region pathbuild
	/// <summary>
	/// Returns a VarSet for a given save.
	/// </summary>
	public static VarSet VarsForSave(Save save)
	{
		return VarsPerSave.EnsureAndGet(save, () => new(save));
	}
	/// <summary>
	/// Returns the folder a given save should reside in.
	/// </summary>
	public static string SaveFolder(in Save save)
	{
		return IO.Path.Combine(Application.persistentDataPath, "Atmo", $"{save.a}");
	}

	/// <summary>
	/// Returns final file path the save should reside in.
	/// </summary>
	public static string SaveFile(in Save save, DataSection section)
	{
		return IO.Path.Combine(SaveFolder(save), $"{save.b.ToString()}_{section}.json");
	}

	/// <summary>
	/// Returns filepath for global variables of a given slot.
	/// </summary>
	public static string GlobalFile(int slot)
	{
		return IO.Path.Combine(SaveFolder(new(slot, __slugnameNotFound)), "global.json");
	}
	#endregion pathbuild
	/// <summary>
	/// Creates a valid <see cref="VT{T1, T2}"/> instance for use in 
	/// </summary>
	/// <param name="slot"></param>
	/// <param name="char"></param>
	/// <returns></returns>
	public static Save MakeSD(int slot, SlugcatStats.Name @char)
	{
		return new(slot, @char, "SaveData", "slot", "char");
	}

	private static string __ErrorMessage(Site site, string message, Exception? ex)
	{
		return $"{nameof(VarRegistry)}: {site}: {message}\nException: {ex?.ToString() ?? "NULL"}";
	}
	#endregion methods
	#region nested
	/// <summary>
	/// Variable kinds
	/// </summary>
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
		//global
	}
	private enum Site
	{
		ReadData,
		WipeData,
		WriteData,
		HookWipe,
		HookNormal,
		HookPersistent,
		Init,
		InitSpecials,
		Clear
	}
	#endregion
}
