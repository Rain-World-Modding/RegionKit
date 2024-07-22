using RegionKit.Modules.Atmo.API;
using static RegionKit.Modules.Atmo.API.Backing;
using static RegionKit.Modules.Atmo.Atmod;
using System.Text.RegularExpressions;
//using static Atmo.API.V0;

using HarmonyLib;

namespace RegionKit.Modules.Atmo.Data;

public static partial class VarRegistry
{
	#region fields
	internal static readonly Regex __Metaf_Sub = new("(\\s.+$|$)");
	internal static readonly Regex __Metaf_Name = new("^\\w+(?=\\s|$)");
	#endregion;
	/// <summary>
	/// Attempts constructing a metafunction under specified name for specified saveslot and character, wrapped in an Arg
	/// </summary>
	/// <param name="text">Raw text (including the name)</param>
	/// <param name="world">Save slot to look at</param>
	/// <returns></returns>
	public static Arg? GetMetaFunction(string text, World world)
	{
		Match _is;
		if (!(_is = __Metaf_Sub.Match(text)).Success) return null;
		string name = __Metaf_Name.Match(text).Value;//text.Substring(0, Mathf.Max(_is.Index - 1, 0));
		VerboseLog($"Attempting to create metafun from {text} (name {name}, match {_is.Value})");
		if (__namedMetafuncs.TryGetValue(name, out var metaFunc))
		{
			Arg? res = metaFunc.Invoke(_is.Value.Trim(), world);
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
