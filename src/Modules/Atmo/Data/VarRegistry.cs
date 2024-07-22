using static RegionKit.Modules.Atmo.Atmod;
namespace RegionKit.Modules.Atmo.Data;
/// <summary>
/// Allows accessing a pool of variables, global or save-specific.
/// You can use <see cref="GetVar"/> to fetch variables by name.
/// Variables are returned as <see cref="NewArg"/>s (NOTE: they may be mutable!
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
	internal const string PREFIX_NORMAL = "n_";
	internal const string PREFIX_REGION = "r_";
	internal const string PREFIX_PERSISTENT = "p_";
	//internal const string PREFIX_FMT = "$FMT_";
	internal static NewArg __Defarg = new() { string.Empty };
	#endregion
	#region lifecycle
	internal static void __Clear()
	{
	}
	internal static void __Init()
	{
		LogDebug("Init VarRegistry hooks");
		try
		{
			//__FillSpecials();
		}
		catch (Exception ex)
		{
			LogFatal(__ErrorMessage(Site.Init, "Unhandled exception", ex));
		}

	}
	#endregion lifecycle
	#region methods
	public static NewArg ParseArg(string value, out string? name)
	{
		name = null;
		string raw = value;

		int splPoint = value.IndexOf('=');
		if (splPoint is not -1 && splPoint < value.Length - 1)
		{
			name = value.Substring(0, splPoint);
			raw = value.Substring(splPoint + 1);
		}
		if (raw.StartsWith("$"))
		{
			return VarRegistry.GetVar(raw.Substring(1), __temp_World);
		}
		return new(name, raw);
	}


	/// <summary>
	/// Fetches a stored variable. Creates a new one if does not exist. You can use prefixes to request death-persistent ("p_") and global ("g_") variables. Persistent variables follow the lifecycle of <see cref="DeathPersistentSaveData"/>; global variables are shared across the entire saveslot.
	/// </summary>
	/// <param name="name">Name of the variable, with prefix if needed. Must not be null.</param>
	/// <param name="world">Save slot to look up data from (<see cref="RainWorld"/>.options.saveSlot for current)</param>
	/// <returns>Variable requested; if there was no variable with given name before, GetVar creates a blank one from an empty string.</returns>
	public static NewArg GetVar(string name, World world)
	{
		BangBang(name, nameof(name));
		if (name.StartsWith("$") && GetMetaFunction(name.Substring(1), world) is NewArg meta)
		{
			return meta;
		}
		if (GetSpecial(name, world) is NewArg spec)
		{
			return spec;
		}
		if (name.StartsWith(PREFIX_GLOBAL))
		{
			name = name.Substring(PREFIX_GLOBAL.Length);
			LogDebug($"Reading global var {name}");
			return SaveVarRegistry.GetSaveData(world, SaveVarRegistry.DataSection.Global)[name] ?? new();
		}
		else if (name.StartsWith(PREFIX_VOLATILE))
		{
			name = name.Substring(PREFIX_VOLATILE.Length);
			return SaveVarRegistry.GetSaveData(world, SaveVarRegistry.DataSection.Volatile)[name] ?? new();
		}
		else if (name.StartsWith(PREFIX_PERSISTENT))
		{

			name = name.Substring(PREFIX_PERSISTENT.Length);
			return SaveVarRegistry.GetSaveData(world, SaveVarRegistry.DataSection.Persistent)[name] ?? new();
		}
		else if (name.StartsWith(PREFIX_REGION))
		{

			name = name.Substring(PREFIX_REGION.Length);
			return SaveVarRegistry.GetSaveData(world, SaveVarRegistry.DataSection.Region)[name] ?? new();
		}
		return new(null, name);
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
