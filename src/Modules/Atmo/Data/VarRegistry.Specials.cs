using RegionKit.Modules.Atmo.API;
using static RegionKit.Modules.Atmo.API.Backing;
using static RegionKit.Modules.Atmo.Atmod;
using System.Text.RegularExpressions;
//using static Atmo.API.V0;

using NamedVars = System.Collections.Generic.Dictionary<RegionKit.Modules.Atmo.Data.VarRegistry.SpVar, RegionKit.Modules.Atmo.Data.NewArg>;
using HarmonyLib;

namespace RegionKit.Modules.Atmo.Data;

public static partial class VarRegistry
{
	#region fields
	internal static readonly NamedVars __SpecialVars = new();
	internal static readonly Regex __Metaf_Sub = new("^\\w+(\\s.+$|$)");
	internal static readonly Regex __Metaf_Name = new("^\\w+(?=\\s|$)");
	#endregion;
	/// <summary>
	/// Attempts constructing a metafunction under specified name for specified saveslot and character, wrapped in an Arg
	/// </summary>
	/// <param name="text">Raw text (including the name)</param>
	/// <param name="world">Save slot to look at</param>
	/// <returns></returns>
	public static NewArg? GetMetaFunction(string text, World world)
	{
		Match _is;
		if (!(_is = __Metaf_Sub.Match(text)).Success) return null;
		string name = __Metaf_Name.Match(text).Value;//text.Substring(0, Mathf.Max(_is.Index - 1, 0));
		VerboseLog($"Attempting to create metafun from {text} (name {name}, match {_is.Value})");
		if (__namedMetafuncs.TryGetValue(name, out var metaFunc))
		{
			NewArg? res = metaFunc.Invoke(_is.Value, world);
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
	public static NewArg? GetSpecial(string name, World world)
	{
		SpVar tp = __SpecialForName(name);
		if (tp is SpVar.NONE) return null;
		return SpecialArg(tp, world);
	}
	internal static void __FillSpecials()
	{
		__SpecialVars.Clear();
		foreach (SpVar tp in Enum.GetValues(typeof(SpVar)))
		{
			World world = (rainWorld?.processManager?.FindSubProcess<RainWorldGame>()!.world)!;
			try
			{
				__SpecialVars[tp] = SpecialArg(tp, world);
				
			}
			catch (Exception ex)
			{
				LogError(__ErrorMessage(Site.InitSpecials, $"Error registering a special: {tp}", ex));
			}
		}
	}

	internal static NewArg SpecialArg(SpVar tp, World world)
	{
		return tp switch
		{
			SpVar.NONE => new() { 0 },
			SpVar.version => new() { Ver },
			SpVar.time => new() { new Callback<string>(getter: DateTime.Now.ToString) },
			SpVar.utctime => new() { new Callback<string>(getter: DateTime.UtcNow.ToString) },
			SpVar.cycletime => new()
					{
					new RWCallback<int>(world, (world) => world.rainCycle?.cycleLength ?? -1),
					new RWCallback<float>(world, (world) => (world.rainCycle?.cycleLength ?? -1f) / 40f),
					new RWCallback<string>(world, (world) => $"{(world.rainCycle?.cycleLength ?? -1f) / 40f} seconds / {(world.rainCycle?.cycleLength ?? -1f)} frames")
					},
			SpVar.root => new() { new Callback<string>(getter: Custom.RootFolderDirectory) },
			SpVar.realm => new() { new Callback<bool>(getter: () => FindAssemblies("Realm").Count() > 0) },
			SpVar.os => new() { new Callback<string>(getter: Environment.OSVersion.Platform.ToString) },
			SpVar.memused => new() { new Callback<string>(getter: GC.GetTotalMemory(false).ToString) },
			SpVar.memtotal => new() { new Callback<string>(getter: () => "???") },
			SpVar.username => new() { Environment.UserName },
			SpVar.machinename => new() { Environment.MachineName },
			SpVar.karma => new() { new RWCallback<int>(world, getter: (world) => world.game.GetStorySession?.saveState.deathPersistentSaveData.karma ?? -1) },
			SpVar.karmacap => new() { new RWCallback<int>(world, getter: (world) => world.game.GetStorySession?.saveState.deathPersistentSaveData.karmaCap ?? -1) },
			_ => new() { 0 },
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
