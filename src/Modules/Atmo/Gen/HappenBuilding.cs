using RegionKit.Modules.Atmo.API;
using RegionKit.Modules.Atmo.Helpers;
using static RegionKit.Modules.Atmo.API.Backing;

using RegionKit.Modules.Atmo.Body;
using static RegionKit.Modules.Atmo.API.V0;
using static RegionKit.Modules.Atmo.Body.HappenTrigger;
using System.Reflection;

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
	internal static void __NewHappen(Happen happen)
	{
		foreach (KeyValuePair<string, string[]> ac in happen.actions)
		{
			if (__namedActions.TryGetValue(ac.Key, out var builder))
			{
				builder.Invoke(happen, ac.Value);
			}
		}
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
	internal static HappenTrigger __CreateTrigger(string id, string[] args, RainWorldGame rwg, Happen owner)
	{
		HappenTrigger? res = null;

		if (__namedTriggers.TryGetValue(id, out var trigger))
		{
			res = trigger.Invoke(args.Select(x => x.ApplyEscapes()).ToArray(), rwg, owner);
		}

		if (res is null)
		{
			LogWarning($"Failed to create a trigger! {id}, args: {args.Stitch()}. Replacing with a stub");
			res = new EventfulTrigger() { On_ShouldRunUpdates = () => true };
		}
		return res;
	}
}
