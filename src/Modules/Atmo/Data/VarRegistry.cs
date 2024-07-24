using RegionKit.Modules.Atmo.Gen;
using static RegionKit.Modules.Atmo.API.Backing;
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
	internal static Arg __Defarg = new StaticArg(string.Empty);
	public static Arg ParseArg(string value, out string? name, World? world)
	{
		name = null;
		string raw = value;
		Arg? result = null;

		int splPoint = value.IndexOf('=');
		if (splPoint is not -1 && splPoint < value.Length - 1)
		{
			name = value.Substring(0, splPoint);
			raw = value.Substring(splPoint + 1);
		}
		if (raw.StartsWith("$") && world != null)
		{
			result = VarRegistry.GetVar(raw.Substring(1), world);
		}
		result ??= new StaticArg(raw);
		if (name != null) result.Name = name;
		return result;
	}


	/// <summary>
	/// Fetches a stored variable. Creates a new one if does not exist. You can use prefixes to request death-persistent ("p_") and global ("g_") variables. Persistent variables follow the lifecycle of <see cref="DeathPersistentSaveData"/>; global variables are shared across the entire saveslot.
	/// </summary>
	/// <param name="name">Name of the variable, with prefix if needed. Must not be null.</param>
	/// <param name="world">Save slot to look up data from (<see cref="RainWorld"/>.options.saveSlot for current)</param>
	/// <returns>Variable requested; if there was no variable with given name before, GetVar creates a blank one from an empty string.</returns>
	public static Arg GetVar(string name, World world)
	{
		BangBang(name, nameof(name));
		if (name.StartsWith("$") && GetMetaFunction(name.Substring(1), world) is Arg meta)
		{
			return meta;
		}
		if (GetSpecial(name, world) is Arg spec)
		{
			return spec;
		}
		return new StaticArg(name);
	}


	/// <summary>
	/// Attempts constructing a metafunction under specified name for specified saveslot and character, wrapped in an Arg
	/// </summary>
	/// <param name="text">Raw text (including the name)</param>
	/// <param name="world">Save slot to look at</param>
	/// <returns></returns>
	public static Arg? GetMetaFunction(string text, World world)
	{
		string[] result = HappenParser.ConsolidateLiterals(text.Split(' '));
		if (result.Length == 0 || string.IsNullOrWhiteSpace(result[0])) return null;
		string name = result[0];
		string value = string.Join(" ", result.Skip(1));
		VerboseLog($"Attempting to create metafun from {text} (name {name}, match {value})");
		if (__namedMetafuncs.TryGetValue(name, out var metaFunc))
		{
			Arg? res = metaFunc.Invoke(value, world);
			if (res != null) return res;
		}
		VerboseLog($"No metafun {name}, variable lookup continues as normal");
		return null;
		//if (!(_is = FMT_Is.Match(text)).Success) return null;
		//text = _is.Groups[1].Value;

	}
	/// <summary>
	/// Attempts fetching a special variable by name.
	/// </summary>
	/// <param name="name">Supposed var name</param>
	/// <returns>null if not a special</returns>
	public static Arg? GetSpecial(string name, World world)
	{
		SpVar tp = __SpecialForName(name);
		if (tp is SpVar.NONE) return null;
		return SpecialArg(tp, world);
	}

	internal static Arg SpecialArg(SpVar tp, World world)
	{
		return tp switch
		{
			SpVar.NONE => new StaticArg(""),
			SpVar.version => new StaticArg(Ver),
			SpVar.time => new CallbackArg() { DateTime.Now.ToString },
			SpVar.utctime => new CallbackArg() { DateTime.UtcNow.ToString },
			SpVar.cycletime => new RWCallbackArg(world)
					{
					(world) => world.rainCycle?.cycleLength ?? -1,
					(world) => (world.rainCycle?.cycleLength ?? -1f) / 40f,
					(world) => $"{(world.rainCycle?.cycleLength ?? -1f) / 40f} seconds / {(world.rainCycle?.cycleLength ?? -1f)} frames"
					},
			SpVar.root => new CallbackArg() { Custom.RootFolderDirectory },
			SpVar.realm => new CallbackArg() { () => FindAssemblies("Realm").Count() > 0 },
			SpVar.os => new CallbackArg() { Environment.OSVersion.Platform.ToString },
			SpVar.memused => new CallbackArg() { GC.GetTotalMemory(false).ToString },
			SpVar.memtotal => new CallbackArg() { () => "???" },
			SpVar.username => new StaticArg(Environment.UserName),
			SpVar.machinename => new StaticArg(Environment.MachineName),
			SpVar.karma => new RWCallbackArg(world) { (world) => world.game.GetStorySession?.saveState.deathPersistentSaveData.karma ?? -1 },
			SpVar.karmacap => new RWCallbackArg(world) { (world) => world.game.GetStorySession?.saveState.deathPersistentSaveData.karmaCap ?? -1 },
			_ => new StaticArg(""),
		};
	}

	internal static SpVar __SpecialForName(string name)
	{
		return name.ToLower() switch
		{
			"root" or "rootfolder" => SpVar.root,
			"now" or "time" => SpVar.time,
			"utcnow" or "utctime" => SpVar.utctime,
			"version" or "atmover" or "atmoversion" => SpVar.version,
			"cycletime" or "cycle" => SpVar.cycletime,
			"realm" => SpVar.realm,
			"os" => SpVar.os,
			"memoryused" or "memused" => SpVar.memused,
			"memorytotal" or "memtotal" => SpVar.memtotal,
			"user" or "username" => SpVar.username,
			"machine" or "machinename" => SpVar.machinename,
			"karma" => SpVar.karma,
			"karmacap" or "maxkarma" => SpVar.karmacap,
			_ => SpVar.NONE,
		};
	}

	internal enum SpVar
	{
		NONE,
		time,
		utctime,
		os,
		root,
		realm,
		version,
		memused,
		memtotal,
		username,
		machinename,
		karma,
		karmacap,
		cycletime,
	}
}
