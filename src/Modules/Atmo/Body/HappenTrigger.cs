using RegionKit.Modules.Atmo.Data;
using UnityEngine.PlayerLoop;

namespace RegionKit.Modules.Atmo.Body;
/// <summary>
/// Base class for triggers. Triggers determine when happens are allowed to run; They are composed into a <see cref="PredicateInlay"/> instance, that acts as a logical expression tree. Derive from this class to define your custom trigger to be used in <see cref="API.V0.AddNamedTrigger"/> overloads, or use the event-driven child <see cref="EventfulTrigger"/> if you prefer composition with callbacks here as well.
/// <para>
/// This expression: <code> WHEN: (maybe 0.7 OR karma 1 2 3) AND fry 25 5 </code> will be turned into a set of happentrigger children that do the following:
/// <list type="number">
/// <item>A <see cref="Maybe"/>, that takes argument 0.7. Each cycle, this will either be active with 70% chance, or inactive with 30% chance.</item>
/// <item>A <see cref="OnKarma"/>, that is active when the player's current karma is 1, 2 or 3.</item>
/// <item>A <see cref="Fry"/>, that takes two arguments: activity limit and cooldown. It will always be active, but turn off (for 5 seconds) once the happen is active for long enough (25 seconds).</item>
/// </list>
/// These objects' collective response to <see cref="Active"/> will be checked up to once each frame. Expressions follow normal boolean logic stucture, operator precedence is NOT > AND > XOR > OR.
/// </para>
/// <para>
/// Note that <see cref="PredicateInlay.Eval"/> can short-circuit, so some of the triggers may not receive a call to their <see cref="Active"/> every frame. In this example, evaluation starts with Maybe, then goes on to Karma. If at least one of them is true, Fry gets evaluated; if they are both false, expression short-circuits, and Fry is not evaluated.
/// </para>
/// NOTICE: As of now, all trigger children mentioned in above example are deprecated and serve only as code samples. For builtins, <see cref="EventfulTrigger"/> is used instead.
/// </summary>
public abstract partial class HappenTrigger
{
	#region fields
	/// <summary>
	/// Happen that owns this instance. Some triggers, such as <see cref="AfterOther"/>, need this to check state of other triggers, the owning happen itself, or other happens in the current set.
	/// </summary>
	public Happen? owner;
	/// <summary>
	/// ArgSet for trigger.
	/// </summary>
	public ArgSet args;
	#endregion
	/// <summary>
	/// Provide an owning happen if you need to. You can provide null or omit the parameter if you do not require it.
	/// </summary>
	/// <param name="owner">The happen instance that owns this trigger. Can be null.</param>
	public HappenTrigger(Happen? owner = null, ArgSet? set = null) { this.owner = owner; this.args = set ?? new(); }
	/// <summary>
	/// Answers if a trigger is currently ready. This may be called up to once per frame.
	/// </summary>
	/// <returns></returns>
	public abstract bool Active();
	/// <summary>
	/// Called every <see cref="Happen.CoreUpdate"/>, *once* every frame. Override this to do your frame-persistent logic.
	/// </summary>
	public virtual void Update() { }
	/// <summary>
	/// Intermediary abstract class for triggers that require RainWorldGame state access (such as <see cref="OnKarma"/> or <see cref="AfterRain"/>). Make sure to provide non-null <see cref="RainWorldGame"/> instance to its constructor.
	/// </summary>
	public abstract class NeedsRWG : HappenTrigger
	{
		/// <summary>
		/// Intermediary constructor. Make sure you provide non-null RainWorldGame instance. Owner may be null.
		/// </summary>
		/// <param name="game">Current game instance for state access.</param>
		/// <param name="ow"></param>
		public NeedsRWG(RainWorldGame game, Happen? ow = null) : base(ow)
		{
			BangBang(game, nameof(game));
			this.game = game;
		}
		/// <summary>
		/// The required rain world instance.
		/// </summary>
		protected readonly RainWorldGame game;
	}

	/// <summary>
	/// An event-driven trigger. Does not inherit <see cref="HappenTrigger.NeedsRWG"/>, intended to be used with lambdas and local capture for state. The following example's factory uses that to create an instance that will, every frame, roll a number between 0 and 5, and the trigger will be active if it rolled 0 that frame:
	/// <code>
	/// <see cref="API.Create_NamedTriggerFactory"/> fac = (args, game, happ) =>
	///	{
	///		int x = 6;
	///		int c = 0;
	///		return new <see cref="EventfulTrigger"/>()
	///		{
	///			On_Update = 
	///				() => { c = <see cref="UnityEngine.Random"/>.Range(0, x); },
	///			On_ShouldRunUpdates =
	///				() => c is 0,
	///		};
	///	}
	/// </code>
	/// <see cref="EventfulTrigger.Active"/> defaults to false if callback is null.
	/// </summary>
	public sealed class EventfulTrigger : HappenTrigger
	{
		/// <summary>
		/// Attach to this to fill in the behaviour of <see cref="Update"/>.
		/// </summary>
		public Action<EventfulTrigger>? On_Update;
		/// <summary>
		/// Attach to this to fill in the behaviour of <see cref="Active"/>. False if null.
		/// </summary>
		public Func<EventfulTrigger, bool>? Is_Active;

		public EventfulTrigger(Happen? owner, ArgSet? set) : base(owner, set)
		{
		}

		/// <inheritdoc/>
		public override void Update()
		{
			On_Update?.Invoke(this);
		}

		/// <inheritdoc/>
		public override bool Active()
		{
			return Is_Active?.Invoke(this) ?? false;
		}
	}
}



