using RegionKit.Modules.Atmo.API;
using RegionKit.Modules.Atmo.Helpers;
using static RegionKit.Modules.Atmo.API.Backing;

using RegionKit.Modules.Atmo.Body;
using static RegionKit.Modules.Atmo.API.V0;
using static RegionKit.Modules.Atmo.Body.HappenTrigger;

namespace RegionKit.Modules.Atmo.Gen;
/// <summary>
/// Manages happens' initialization and builtin behaviours.
/// </summary>
public static partial class HappenBuilding {
	/// <summary>
	/// Populates a happen with callbacks. Called automatically by the constructor.
	/// </summary>
	/// <param name="happen"></param>
	internal static void __NewHappen(Happen happen) {
		if (__MNH_invl is null) return;
		foreach (V0_Create_RawHappenBuilder? cb in __MNH_invl) {
			try {
				cb?.Invoke(happen);
			}
			catch (Exception ex) {
				LogError(
					$"Happenbuild: NewEvent:" +
					$"Error invoking happen factory {cb}//{cb?.Method.Name} for {happen}:" +
					$"\n{ex}");
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
	internal static HappenTrigger __CreateTrigger(
		string id,
		string[] args,
		RainWorldGame rwg,
		Happen owner) {
		HappenTrigger? res = null;
		//res = DefaultTrigger(id, args, rwg, owner);

		if (__MNT_invl is null) goto finish;
		foreach (V0_Create_RawTriggerFactory? cb in __MNT_invl) {
			if (res is not null) break;
			try {
				res ??= cb?.Invoke(id, args.Select(x => x.ApplyEscapes()).ToArray(), rwg, owner);
			}
			catch (Exception ex) {
				LogError(
					$"Happenbuild: CreateTrigger: Error invoking trigger factory " +
					$"{cb}//{cb?.Method.Name} for {id}({args.Stitch()}):" +
					$"\n{ex}");
			}
		}
	finish:
		if (res is null) {
			LogWarning($"Failed to create a trigger! {id}, args: {args.Stitch()}. Replacing with a stub");
			res = new EventfulTrigger() { On_ShouldRunUpdates = () => true };
		}
		return res;
	}
}
