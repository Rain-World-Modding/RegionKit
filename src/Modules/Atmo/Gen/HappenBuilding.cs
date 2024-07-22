﻿using RegionKit.Modules.Atmo.API;
using RegionKit.Modules.Atmo.Helpers;
using static RegionKit.Modules.Atmo.API.Backing;

using RegionKit.Modules.Atmo.Body;
using static RegionKit.Modules.Atmo.API.V0;
using static RegionKit.Modules.Atmo.Body.HappenTrigger;
using static RegionKit.Modules.Atmo.Body.HappenAction;
using System.Reflection;
using RegionKit.Modules.Atmo.Data;

namespace RegionKit.Modules.Atmo.Gen;
/// <summary>
/// Manages happens' initialization and builtin behaviours.
/// </summary>
public static partial class HappenBuilding
{
	/// <summary>
	/// Populates a happen with callbacks. Called automatically by the constructor.
	/// </summary>
	/// <param name="happen"></param>
	internal static HappenAction __NewHappen(string id, string[] args, Happen happen)
	{
		ArgSet argSet = new(args.Select(x => x.ApplyEscapes()).ToArray(), happen.Set.world);
		HappenAction? res = null;
		if (__namedActions.TryGetValue(id, out var builder))
		{
			res = builder.Invoke(happen, argSet);
		}
		if (res is null)
		{
			res = new EventfulAction(happen, argSet);
		}
		return res;

		//API_MakeNewHappen?.Invoke(ha);
	}
	/// <summary>
	/// Creates a new trigger with given ID, arguments using provided <see cref="RainWorldGame"/>.
	/// </summary>
	/// <param name="id">Name or ID</param>
	/// <param name="args">Optional arguments</param>
	/// <param name="rwg">game instance</param>
	/// <param name="owner">Happen that requests the trigger.</param>
	/// <returns>Resulting trigger; an always-active trigger if something went wrong.</returns>
	internal static HappenTrigger __CreateTrigger(string id, string[] args, Happen owner)
	{
		ArgSet argSet = new(args.Select(x => x.ApplyEscapes()).ToArray(), owner.Set.world);
		HappenTrigger? res = null;

		if (__namedTriggers.TryGetValue(id, out var trigger))
		{
			res = trigger.Invoke(argSet, owner);
		}

		if (res is null)
		{
			LogWarning($"Failed to create a trigger! {id}, args: {args.Stitch()}. Replacing with a stub");
			res = new EventfulTrigger(owner, argSet) { Is_Active = (_) => true };
		}
		return res;
	}
}
