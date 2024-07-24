using RegionKit.Modules.Atmo.Body;
using RegionKit.Modules.Atmo.Data;

namespace RegionKit.Modules.Atmo.API;

public static class Backing 
{
	#region fields
	internal static readonly Dictionary<string, Create_NamedHappenBuilder> __namedActions = new();
	internal static readonly Dictionary<string, Create_NamedTriggerFactory> __namedTriggers = new();
	internal static readonly Dictionary<string, Create_NamedMetaFunction> __namedMetafuncs = new();
	#endregion
	/// <summary>
	/// Delegate for registering named actions.
	/// Used by <see cref="V0.AddNamedAction"/>.
	/// </summary>
	/// <param name="happen">Happen that needs lifetime callbacks attached. One of the its <see cref="Happen.actions"/> has a name you selected. Use its instance events to attach behaviour.</param>
	/// <param name="args">The event's arguments, taking from a WHAT: clause.</param>
	public delegate HappenAction? Create_NamedHappenBuilder(Happen happen, ArgSet args);
	/// <summary>
	/// Delegate for registering named triggers. Used by <see cref="V0.AddNamedTrigger"/> overloads.
	/// </summary>
	/// <param name="args">Trigger arguments.</param>
	/// <param name="game">Current RainWorldGame instance.</param>
	/// <param name="happen">Happen the trigger is to be attached to.</param>
	public delegate HappenTrigger? Create_NamedTriggerFactory(ArgSet args, Happen happen);
	/// <summary>
	/// Delegate for registering named metafunctions. Used by <see cref="V0.AddNamedMetafun"/> overloads.
	/// </summary>
	/// <param name="value">Body text passed to the metafun.</param>
	/// <param name="world">Current saveslot number.</param>
	public delegate Arg? Create_NamedMetaFunction(string value, World world);
}
