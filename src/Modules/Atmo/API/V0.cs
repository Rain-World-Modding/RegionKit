using RegionKit.Modules.Atmo.Data;
using static RegionKit.Modules.Atmo.API.Backing;

using RegionKit.Modules.Atmo.Body;
using RegionKit.Modules.Atmo.Data;

namespace RegionKit.Modules.Atmo.API;
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
/// <summary>
/// Static class for user API. You will likely be interacting with the mod from here.
/// There are several ways you may interact with it:
/// <list type="bullet">
/// <item><see cref="AddNamedAction"/> overloads: attach behaviour to happens that have a specific action name in their WHAT clause;</item>
/// <item><see cref="AddNamedTrigger"/> overloads: attach a factory callback that creates a trigger that matches specified name(s);</item>
/// <item><see cref="EV_MakeNewHappen"/> and <see cref="EV_MakeNewTrigger"/> events: directly attach callbacks without builtin name checking.</item>
/// </list>
/// See also: <seealso cref="Happen"/> for core lifecycle logic, <seealso cref="HappenTrigger"/> for how conditions work, <seealso cref="HappenSet"/> for additional info on how happens are composed.
/// </summary>
public static class V0 {
	#region API proper
	/// <summary>
	/// Registers a named action. Multiple names. Up to one callback for every lifecycle event. No args support.
	/// </summary>
	/// <param name="names">Action's names. Case insensitive.</param>
	/// <param name="au">Abstract update callback.</param>
	/// <param name="ru">Realized update callback.</param>
	/// <param name="oi">Init callback.</param>
	/// <param name="cu">Core update callback.</param>
	/// <param name="ignoreCase">Whether action name matching should be case sensitive.</param>
	/// <returns>The number of name collisions encountered.</returns>
	public static int AddNamedAction(
		string[] names,
		V0_lc_AbstractUpdate? au = null,
		V0_lc_RealizedUpdate? ru = null,
		V0_lc_Init? oi = null,
		V0_lc_CoreUpdate? cu = null,
		bool ignoreCase = true) {
		return names
				.Select((name) => AddNamedAction(name, au, ru, oi, cu, ignoreCase) ? 0 : 1)
				.Aggregate((x, y) => x + y);
	}

	/// <summary>
	/// Registers a named action. One name. Up to one callback for every lifecycle event. No args support.
	/// </summary>
	/// <param name="name">Trigger name.</param>
	/// <param name="au">Abstract update callback.</param>
	/// <param name="ru">Realized update callback.</param>
	/// <param name="oi">Init callback.</param>
	/// <param name="cu">Core update callback.</param>
	/// <param name="ignoreCase">Whether action name matching should be case insensitive.</param>
	/// <returns>True if successfully registered; false if name already taken.</returns>
	public static bool AddNamedAction(
		string name,
		V0_lc_AbstractUpdate? au = null,
		V0_lc_RealizedUpdate? ru = null,
		V0_lc_Init? oi = null,
		V0_lc_CoreUpdate? cu = null,
		bool ignoreCase = true) {
		StringComparer? comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;
		if (__namedActions.ContainsKey(name)) { return false; }
		void newCb(Happen ha) {
			foreach (string? ac in ha.actions.Keys) {
				if (comp.Compare(ac, name) == 0) {
					ha.On_AbstUpdate += au;
					ha.On_RealUpdate += ru;
					ha.On_Init += oi;
					ha.On_CoreUpdate += cu;
					return;
				}
			}
		}
		__namedActions.Add(name, newCb);
		EV_MakeNewHappen += newCb;
		return true;
	}
	/// <summary>
	/// Registers a named action. Many names. Arbitrary Happen manipulation. Args support. 
	/// </summary>
	/// <param name="names">Action name(s). Case insensitive.</param>
	/// <param name="builder">User builder callback.</param>
	/// <param name="ignoreCase">Whether action name matching should be case sensitive.</param>
	/// <returns>Number of name collisions encountered.</returns>
	public static int AddNamedAction(
		string[] names,
		V0_Create_NamedHappenBuilder builder,
		bool ignoreCase = true) {
		return names
				.Select((name) => AddNamedAction(name, builder, ignoreCase) ? 0 : 1)
				.Aggregate((x, y) => x + y);
	}

	/// <summary>
	/// Registers a named action. One name. Arbitrary Happen manipulation. Args support.
	/// </summary>
	/// <param name="name">Name of the action.</param>
	/// <param name="builder">User builder callback.</param>
	/// <param name="ignoreCase"></param>
	/// <returns>True if successfully added; false if already taken.</returns>
	public static bool AddNamedAction(
		string name,
		V0_Create_NamedHappenBuilder builder,
		bool ignoreCase = true) {
		if (System.Text.RegularExpressions.Regex.Match(name, "\\w+").Length != name.Length) {
			LogWarning($"Invalid action name: {name}");
			return false;
		}
		if (__namedTriggers.ContainsKey(name)) { return false; }
		StringComparer? comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;
		void newCb(Happen ha) {
			foreach (KeyValuePair<string, string[]> ac in ha.actions) {
				if (comp.Compare(ac.Key, name) == 0) {
					builder?.Invoke(ha, ac.Value);
				}
			}
		}
		__namedActions.Add(name, newCb);
		EV_MakeNewHappen += newCb;
		return true;
	}
	/// <summary>
	/// Removes a named callback.
	/// </summary>
	/// <param name="action"></param>
	public static void RemoveNamedAction(string action) {
		if (!__namedActions.TryGetValue(action, out V0_Create_RawHappenBuilder? builder)) return;
		EV_MakeNewHappen -= builder;
		__namedActions.Remove(action);
	}
	/// <summary>
	/// Registers a named trigger. Multiple names.
	/// </summary>
	/// <param name="names">Trigger's name(s).</param>
	/// <param name="fac">User trigger factory callback.</param>
	/// <param name="ignoreCase">Whether trigger name should be case sensitive.</param>
	/// <returns>Number of name collisions encountered.</returns>
	public static int AddNamedTrigger(
		string[] names,
		V0_Create_NamedTriggerFactory fac,
		bool ignoreCase = true) {
		return names
				.Select((name) => AddNamedTrigger(name, fac, ignoreCase) ? 1 : 0)
				.Aggregate((x, y) => x + y);
	}
	/// <summary>
	/// Registers a named trigger. Single name.
	/// </summary>
	/// <param name="name">Name of the trigger.</param>
	/// <param name="fac">User factory callback.</param>
	/// <param name="ignoreCase">Whether name matching should be case insensitive.</param>
	/// <returns></returns>
	public static bool AddNamedTrigger(
		string name,
		V0_Create_NamedTriggerFactory fac,
		bool ignoreCase = true) {
		if (System.Text.RegularExpressions.Regex.Match(name, "\\w+").Length != name.Length) {
			LogWarning($"Invalid trigger name: {name}");
			return false;
		}
		if (__namedTriggers.ContainsKey(name)) { return false; }
		StringComparer? comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;

		HappenTrigger? newCb(string n, ArgSet args, RainWorldGame rwg, Happen ha) {
			if (comp.Compare(n, name) == 0) return fac(args, rwg, ha);
			return null;
		}
		__namedTriggers.Add(name, newCb);
		EV_MakeNewTrigger += newCb;
		return true;
	}
	/// <summary>
	/// Removes a registered trigger by name.
	/// </summary>
	/// <param name="name"></param>
	public static void RemoveNamedTrigger(string name) {
		if (!__namedTriggers.TryGetValue(name, out V0_Create_RawTriggerFactory? fac)) return;
		EV_MakeNewTrigger -= fac;
		__namedTriggers.Remove(name);
	}
	/// <summary>
	/// Registers a metafunction with a given set of names.
	/// </summary>
	/// <param name="names">Array of names for the metafun.</param>
	/// <param name="handler">User handler callback.</param>
	/// <param name="ignoreCase">Whether name matching should be case insensitive.</param>
	/// <returns>Number of errors and name collisions encountered.</returns>
	public static int AddNamedMetafun(
		string[] names,
		V0_Create_NamedMetaFunction handler,
		bool ignoreCase = true)
		=> names.Select((name) => AddNamedMetafun(name, handler, ignoreCase) ? 0 : 1).Aggregate((x, y) => x + y);
	/// <summary>
	/// Registers a metafunction with a given single name.
	/// </summary>
	/// <param name="name">Name of the metafun.</param>
	/// <param name="handler">User handler callback.</param>
	/// <param name="ignoreCase">Whether macro name matching should be case insensitive.</param>
	/// <returns>True if successfully attached; false otherwise.</returns>
	public static bool AddNamedMetafun(
		string name,
		V0_Create_NamedMetaFunction handler,
		bool ignoreCase = true) {
		if (System.Text.RegularExpressions.Regex.Match(name, "\\w+").Length != name.Length) {
			LogWarning($"Invalid metafun name: {name}");
			return false;
		}
		if (__namedMetafuncs.ContainsKey(name)) return false;
		StringComparer comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;
		IArgPayload? newCb(string n, string val, int ss, SlugcatStats.Name ch) {
			if (comp.Compare(n, name) == 0) return handler(val, ss, ch);
			return null;
		}
		EV_ApplyMetafunctions += newCb;
		__namedMetafuncs.Add(name, newCb);
		return true;
	}
	/// <summary>
	/// Clears a metafunction name binding.
	/// </summary>
	/// <param name="name"></param>
	public static void RemoveNamedMetafun(string name) {
		if (!__namedMetafuncs.TryGetValue(name, out V0_Create_RawMetaFunction? handler)) return;
		EV_ApplyMetafunctions -= handler;
		__namedMetafuncs.Remove(name);
	}
	/// <summary>
	/// Subscribe to this to attach your custom callbacks to newly created happen objects.
	/// You can also use <see cref="AddNamedAction"/> overloads as name-safe wrappers.
	/// </summary>
	public static event V0_Create_RawHappenBuilder? EV_MakeNewHappen {
		add {
			Backing.__EV_MakeNewHappen += value;
		}
		remove {
			Backing.__EV_MakeNewHappen -= value;
		}
	}

	/// <summary>
	/// Subscribe to this to dispense your custom triggers.
	/// You can also use <see cref="AddNamedTrigger"/> overloads as a name-safe wrappers.
	/// </summary>
	public static event V0_Create_RawTriggerFactory? EV_MakeNewTrigger {
		add {
			Backing.__EV_MakeNewTrigger += value;
		}
		remove {
			Backing.__EV_MakeNewTrigger -= value;
		}

	}

	/// <summary>
	/// Subscribe to this to register custom variables-macros. You can also use <see cref="AddNamedMetafun"/> overloads as name-safe wrappers.
	/// </summary>
	public static event V0_Create_RawMetaFunction? EV_ApplyMetafunctions {
		add {
			Backing.__EV_ApplyMetafunctions += value;
		}
		remove {
			Backing.__EV_ApplyMetafunctions -= value;
		}
	}

#pragma warning restore CS0419 // Ambiguous reference in cref attribute
	#endregion
}
