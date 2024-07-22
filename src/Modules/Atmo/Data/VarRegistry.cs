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
}
