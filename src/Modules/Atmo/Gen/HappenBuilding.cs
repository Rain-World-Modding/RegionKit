using RegionKit.Modules.Atmo.API;
using RegionKit.Modules.Atmo.Helpers;
using static RegionKit.Modules.Atmo.API.Backing;

using RegionKit.Modules.Atmo.Body;
using static RegionKit.Modules.Atmo.API.V0;
using static RegionKit.Modules.Atmo.Body.HappenTrigger;
using static RegionKit.Modules.Atmo.Body.HappenAction;
using System.Reflection;
using RegionKit.Modules.Atmo.Data;
using static RegionKit.Modules.Atmo.Body.OperatorTrigger;
using System;

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

	internal static HappenTrigger BuildHappenTrigger(string[] array, Happen owner)
	{
		if (array.Length == 0 || array[0].Length == 0) return new EventfulTrigger(owner, null);

		try
		{
			if (array[0].StartsWith("(") && array[^1].EndsWith(")"))
			{ array[0] = array[0][1..]; array[^1] = array[^1][..^1]; }

			int layers = 0;
			for (int i = 0; i < array.Length; i++)
			{
				string str = array[i];

				bool remove = false;
				foreach (char c in str)
				{
					if (c != '(') break;
					if (layers == 0) remove = true;
					layers++;
				}

				if (remove) array[i] = str[1..];

				remove = false;
				foreach (char c in str.Reverse())
				{
					if (c != ')') break;
					layers--;
					if (layers == 0) remove = true;
				}

				if (remove) array[i] = str[..^1];

				if (layers != 0) continue;

				if (OperatorTrigger.args.TryGetValue(str, out var op))
				{
					Atmod.VerboseLog($"operator is [{str}]");
					Atmod.VerboseLog($"left is [{array[0]}], [{string.Join(", ", array.Skip(1).Take(i - 1))}]");
					Atmod.VerboseLog($"right is [{array[i + 1]}], [{string.Join(", ", array.Skip(i + 2))}]");
					HappenTrigger left = BuildHappenTrigger(array.Take(i).ToArray(), owner);
					HappenTrigger right = BuildHappenTrigger(array.Skip(i + 1).ToArray(), owner);
					return op(left, right);
				}
			}

			return __CreateTrigger(array[0], array.Skip(1).ToArray(), owner);
		}
		catch (Exception e) 
		{
			Atmod.VerboseLog($"exception when parsing trigger [{string.Join(" ", array)}]\n{e}");
			return new EventfulTrigger(owner, null);
		}
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
		LogWarning($"creating trigger {id}, args: {args.Stitch()}.");
		ArgSet argSet = new(args.Select(x => x.ApplyEscapes()).ToArray(), owner.Set.world);
		HappenTrigger? res = null;

		if (__namedTriggers.TryGetValue(id, out var trigger))
		{
			res = trigger.Invoke(argSet, owner);
		}

		if (res is null)
		{
			LogWarning($"Failed to create a trigger! {id}, args: {args.Stitch()}. Replacing with a stub");
			res = new EventfulTrigger(owner, argSet);
		}
		return res;
	}
}
