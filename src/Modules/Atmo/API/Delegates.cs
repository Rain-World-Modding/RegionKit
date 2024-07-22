using RegionKit.Modules.Atmo.Data;

using RegionKit.Modules.Atmo.Body;

namespace RegionKit.Modules.Atmo.API;

#pragma warning disable CS0419
/// <summary>
/// Delegates for happens' abstract updates.
/// </summary>
/// <param name="absroom">Abstract room the update is happening in.</param>
/// <param name="time">Abstract update step, in frames.</param>
public delegate void AbstractUpdate(AbstractRoom absroom, int time);
/// <summary>
/// Delegate for happens' realized updates.
/// </summary>
/// <param name="room">The room update is happening in.</param>
public delegate void RealizedUpdate(Room room);
/// <summary>
/// Delegate for being called on first abstract update
/// </summary>
/// <param name="world"></param>
public delegate void Init(World world);
/// <summary>
/// Delegate for happens' init call.
/// </summary>
/// <param name="rwg">Current instance of <see cref="RainWorldGame"/>. Always a Story session.</param>
public delegate void CoreUpdate(RainWorldGame rwg);
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
public delegate HappenTrigger? Create_NamedTriggerFactory(ArgSet args, RainWorldGame game, Happen happen);
/// <summary>
/// Delegate for registering named metafunctions. Used by <see cref="V0.AddNamedMetafun"/> overloads.
/// </summary>
/// <param name="value">Body text passed to the metafun.</param>
/// <param name="saveslot">Current saveslot number.</param>
/// <param name="character">Current character number.</param>
/// <returns><see cref="IArgPayload"/> object linking to metafun's output; null if there was an error.</returns>
public delegate NewArg? Create_NamedMetaFunction(string value, World world);
#pragma warning restore CS0419
