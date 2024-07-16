using System.Runtime.Serialization;
using static RegionKit.Modules.Atmo.Data.VarRegistry;
using NamedVars = System.Collections.Generic.Dictionary<string, RegionKit.Modules.Atmo.Data.Arg>;
using SerDict = System.Collections.Generic.Dictionary<string, object>;
using RegionKit.Modules.Atmo.Helpers;

namespace RegionKit.Modules.Atmo.Data;
/// <summary>
/// Represents variable storage for a single character save on a single slot.
/// </summary>
public class VarSet
{
	#region fields
	/// <summary>
	/// Death-persistent data
	/// </summary>
	public readonly NamedVars persistent = new();
	/// <summary>
	/// Normal data
	/// </summary>
	public readonly NamedVars normal = new();
	#endregion fields
	/// <summary>
	/// Normal ctor
	/// </summary>
	/// <param name="save"></param>
	public VarSet(VT<int, SlugcatStats.Name> save)
	{

	}
	/// <summary>
	/// Fetches a variable by name (may contain persistent prefix)
	/// </summary>
	public Arg GetVar(string name)
	{
		DataSection sec = DataSection.Normal;
		if (name.StartsWith(PREFIX_PERSISTENT))
		{
			sec = DataSection.Persistent;
			name = name.Substring(PREFIX_PERSISTENT.Length);
		}
		return GetVar(name, sec);
	}
	/// <summary>
	/// Gets variable by name in a specified section (no prefixes)
	/// </summary>
	/// <returns></returns>
	public Arg GetVar(string name, DataSection section)
	{
		NamedVars vars = _DictForSection(section);
		Arg _def = __Defarg;
		return vars.EnsureAndGet(name, () => _def);
	}
	internal SerDict _GetSer(DataSection section)
	{
		SerDict res = new();
		Arg _def = __Defarg;
		NamedVars tdict = _DictForSection(section);
		foreach ((string name, Arg var) in tdict)
		{
			if (var.Equals(_def)) continue;
			res.Add(name, var.Str);
		}
		return res;
	}
	internal void _FillFrom(SerDict? dict, DataSection section)
	{
		NamedVars tdict = _DictForSection(section);
		tdict.Clear();
		if (dict is null) return;
		foreach ((string name, object val) in dict)
		{
			tdict.Add(name, val.ToString());
		}
	}
	internal NamedVars _DictForSection(DataSection sec)
	{
		return sec switch
		{
			DataSection.Normal => normal,
			DataSection.Persistent => persistent,
			_ => throw new ArgumentException("Invalid data section"),
		};
	}
}
