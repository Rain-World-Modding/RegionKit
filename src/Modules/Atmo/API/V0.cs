using RegionKit.Modules.Atmo.Data;
using static RegionKit.Modules.Atmo.API.Backing;

using RegionKit.Modules.Atmo.Body;

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
	public static void AddNamedAction(string[] names, AbstractUpdate? au = null, RealizedUpdate? ru = null, Init? oi = null, CoreUpdate? cu = null, bool ignoreCase = true) 
	{
		foreach (string name in names) { AddNamedAction(name, au, ru, oi, cu, ignoreCase); }
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
	public static void AddNamedAction(string name, AbstractUpdate? au = null, RealizedUpdate? ru = null, Init? oi = null, CoreUpdate? cu = null, bool ignoreCase = true) 
	{
		StringComparer? comp = ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture;
		if (__namedActions.ContainsKey(name)) { return; }
		void newCb(Happen ha, ArgSet args) 
		{
			foreach (string? ac in ha.actions.Keys) 
			{
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
		return;
	}
	/// <summary>
	/// Registers a named action. Many names. Arbitrary Happen manipulation. Args support. 
	/// </summary>
	/// <param name="names">Action name(s). Case insensitive.</param>
	/// <param name="builder">User builder callback.</param>
	/// <param name="ignoreCase">Whether action name matching should be case sensitive.</param>
	/// <returns>Number of name collisions encountered.</returns>
	public static void AddNamedAction(string[] names, Create_NamedHappenBuilder builder, bool ignoreCase = true) 
	{
		foreach (string name in names) { AddNamedAction(name, builder, ignoreCase); }
	}

	/// <summary>
	/// Registers a named action. One name. Arbitrary Happen manipulation. Args support.
	/// </summary>
	/// <param name="name">Name of the action.</param>
	/// <param name="builder">User builder callback.</param>
	/// <param name="ignoreCase"></param>
	/// <returns>True if successfully added; false if already taken.</returns>
	public static void AddNamedAction(string name, Create_NamedHappenBuilder builder, bool ignoreCase = true) 
	{
		if (System.Text.RegularExpressions.Regex.Match(name, "\\w+").Length != name.Length) 
		{
			LogWarning($"Invalid action name: {name}");
			return;
		}
		if (__namedActions.ContainsKey(name)) { return; }
		__namedActions.Add(name, builder);
		return;
	}
	/// <summary>
	/// Removes a named callback.
	/// </summary>
	/// <param name="action"></param>
	public static void RemoveNamedAction(string action) 
	{
		if (!__namedActions.TryGetValue(action, out Create_NamedHappenBuilder? builder)) return;
		__namedActions.Remove(action);
	}
	/// <summary>
	/// Registers a named trigger. Multiple names.
	/// </summary>
	/// <param name="names">Trigger's name(s).</param>
	/// <param name="fac">User trigger factory callback.</param>
	/// <param name="ignoreCase">Whether trigger name should be case sensitive.</param>
	/// <returns>Number of name collisions encountered.</returns>
	public static void AddNamedTrigger(string[] names, Create_NamedTriggerFactory fac, bool ignoreCase = true) 
	{
		foreach (var name in names) { AddNamedTrigger(name, fac, ignoreCase); }
	}
	/// <summary>
	/// Registers a named trigger. Single name.
	/// </summary>
	/// <param name="name">Name of the trigger.</param>
	/// <param name="fac">User factory callback.</param>
	/// <param name="ignoreCase">Whether name matching should be case insensitive.</param>
	/// <returns></returns>
	public static void AddNamedTrigger(string name, Create_NamedTriggerFactory fac, bool ignoreCase = true) 
	{
		if (System.Text.RegularExpressions.Regex.Match(name, "\\w+").Length != name.Length) {
			LogWarning($"Invalid trigger name: {name}");
			return;
		}
		if (__namedTriggers.ContainsKey(name)) return;
		__namedTriggers.Add(name, fac);
		return;
	}
	/// <summary>
	/// Removes a registered trigger by name.
	/// </summary>
	/// <param name="name"></param>
	public static void RemoveNamedTrigger(string name)
	{
		if (!__namedTriggers.TryGetValue(name, out Create_NamedTriggerFactory? fac)) return;
		__namedTriggers.Remove(name);
	}
	/// <summary>
	/// Registers a metafunction with a given set of names.
	/// </summary>
	/// <param name="names">Array of names for the metafun.</param>
	/// <param name="handler">User handler callback.</param>
	/// <param name="ignoreCase">Whether name matching should be case insensitive.</param>
	/// <returns>Number of errors and name collisions encountered.</returns>
	public static void AddNamedMetafun(string[] names, Create_NamedMetaFunction handler, bool ignoreCase = true)
	{
		foreach (string name in names) { AddNamedMetafun(name, handler, ignoreCase); }
	}

	/// <summary>
	/// Registers a metafunction with a given single name.
	/// </summary>
	/// <param name="name">Name of the metafun.</param>
	/// <param name="handler">User handler callback.</param>
	/// <param name="ignoreCase">Whether macro name matching should be case insensitive.</param>
	/// <returns>True if successfully attached; false otherwise.</returns>
	public static bool AddNamedMetafun(string name, Create_NamedMetaFunction handler, bool ignoreCase = true) 
	{
		if (System.Text.RegularExpressions.Regex.Match(name, "\\w+").Length != name.Length) 
		{
			LogWarning($"Invalid metafun name: {name}");
			return false;
		}
		if (__namedMetafuncs.ContainsKey(name)) return false;
		__namedMetafuncs.Add(name, handler);
		return true;
	}
	/// <summary>
	/// Clears a metafunction name binding.
	/// </summary>
	/// <param name="name"></param>
	public static void RemoveNamedMetafun(string name) 
	{
		if (!__namedMetafuncs.TryGetValue(name, out Create_NamedMetaFunction? handler)) return;
		__namedMetafuncs.Remove(name);
	}

#pragma warning restore CS0419 // Ambiguous reference in cref attribute
	#endregion
}
