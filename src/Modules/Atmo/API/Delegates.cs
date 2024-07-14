using RegionKit.Modules.Atmo.API;
using RegionKit.Modules.Atmo.Data;

using RegionKit.Modules.Atmo.Body;

namespace RegionKit.Modules.Atmo.API;

#pragma warning disable CS0419
#region V0
/// <summary>
/// Delegates for happens' abstract updates.
/// </summary>
/// <param name="absroom">Abstract room the update is happening in.</param>
/// <param name="time">Abstract update step, in frames.</param>
public delegate void V0_lc_AbstractUpdate(AbstractRoom absroom, int time);
/// <summary>
/// Delegate for happens' realized updates.
/// </summary>
/// <param name="room">The room update is happening in.</param>
public delegate void V0_lc_RealizedUpdate(Room room);
/// <summary>
/// Delegate for being called on first abstract update
/// </summary>
/// <param name="world"></param>
public delegate void V0_lc_Init(World world);
/// <summary>
/// Delegate for happens' init call.
/// </summary>
/// <param name="rwg">Current instance of <see cref="RainWorldGame"/>. Always a Story session.</param>
public delegate void V0_lc_CoreUpdate(RainWorldGame rwg);
/// <summary>
/// Callback for attaching custom behaviour to happens. Can be directly attached to <see cref="V0.EV_MakeNewHappen"/>
/// </summary>
/// <param name="happen">Happen that needs lifetime callbacks attached. Check its instance members to see if your code should affect it, and use its instance events to attach behaviour.</param>
public delegate void V0_Create_RawHappenBuilder(Happen happen);
/// <summary>
/// Delegate for registering named actions.
/// Used by <see cref="V0.AddNamedAction"/>.
/// </summary>
/// <param name="happen">Happen that needs lifetime callbacks attached. One of the its <see cref="Happen.actions"/> has a name you selected. Use its instance events to attach behaviour.</param>
/// <param name="args">The event's arguments, taking from a WHAT: clause.</param>
public delegate void V0_Create_NamedHappenBuilder(Happen happen, ArgSet args);
/// <summary>
/// Delegate for including custom triggers. Can be directly attached to <see cref="V0.EV_MakeNewTrigger"/>. Make sure to check the first parameter (name) and see if it is fitting.
/// </summary>
/// <param name="name">Trigger name (id).</param>
/// <param name="args">A set of (usually optional) arguments.</param>
/// <param name="game">Current game instance.</param>
/// <param name="happen">Happen to attach things to.</param>
/// <returns>Child of <see cref="HappenTrigger"/> if subscriber wishes to claim the trigger; null if not.</returns>
public delegate HappenTrigger? V0_Create_RawTriggerFactory(string name, ArgSet args, RainWorldGame game, Happen happen);
/// <summary>
/// Delegate for registering named triggers. Used by <see cref="V0.AddNamedTrigger"/> overloads.
/// </summary>
/// <param name="args">Trigger arguments.</param>
/// <param name="game">Current RainWorldGame instance.</param>
/// <param name="happen">Happen the trigger is to be attached to.</param>
public delegate HappenTrigger? V0_Create_NamedTriggerFactory(ArgSet args, RainWorldGame game, Happen happen);
/// <summary>
/// Delegate for registering metafunctions, for use in <see cref="VarRegistry.GetVar"/>. Can be directly attached to <see cref="V0.EV_ApplyMetafunctions"/>. Make sure to check the first parameter (name) and see if it is fitting.
/// </summary>
/// <param name="name">Supposed name of the metafun.</param>
/// <param name="value">Body text passed to the metafun.</param>
/// <param name="saveslot">Current saveslot number.</param>
/// <param name="character">Current character number.</param>
/// <returns><see cref="IArgPayload"/> object linking to metafun's output; null if name does not fit or there was an error.</returns>
public delegate IArgPayload? V0_Create_RawMetaFunction(string name, string value, int saveslot, SlugcatStats.Name character);
/// <summary>
/// Delegate for registering named metafunctions. Used by <see cref="V0.AddNamedMetafun"/> overloads.
/// </summary>
/// <param name="value">Body text passed to the metafun.</param>
/// <param name="saveslot">Current saveslot number.</param>
/// <param name="character">Current character number.</param>
/// <returns><see cref="IArgPayload"/> object linking to metafun's output; null if there was an error.</returns>
public delegate IArgPayload? V0_Create_NamedMetaFunction(string value, int saveslot, SlugcatStats.Name character);
#endregion
#pragma warning restore CS0419
