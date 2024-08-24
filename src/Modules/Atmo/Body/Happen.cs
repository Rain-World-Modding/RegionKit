using RegionKit.Modules.Atmo.API;
using static RegionKit.Modules.Atmo.API.Backing;
using static RegionKit.Modules.Atmo.Atmod;

using RegionKit.Modules.Atmo.Gen;
using BepInEx.Logging;

namespace RegionKit.Modules.Atmo.Body;
/// <summary>
/// A "World event": sealed class that carries custom code in form of callbacks. Every happen is read from within a <c>HAPPEN:...END HAPPEN</c> block in an .ATMO file. The following example block:
/// <para>
/// <code>
/// HAPPEN: test
/// WHAT: palette 15
/// WHERE: first + SU_A41 SU_A42 - SU_A22
/// WHERE: SU_C04
/// WHEN: karma 1 2 10
/// END HAPPEN
/// </code>
/// Will result in a happen that has the following properties:
/// <list type="table">
/// <listheader><term>Property</term> <description>Contents and meaning</description></listheader>
/// <item>
///		<term><see cref="name"/></term> 
///		<description>
///			Unique string that identifies an instance. 
///			This happen will be called <c>test</c>.
///		</description>
///	</item>
/// <item>
///		<term>Behaviour</term> 
///		<description>
///			The instance's lifetime events are populated with callbacks taken from <see cref="__EV_MakeNewHappen"/>.
///			For registering your behaviours, see <seealso cref="Atmo.API"/>.
///			To see examples of how some of the builtin behaviours work, see <seealso cref="HappenBuilding.__InitBuiltins"/>.
///			This happen will change main palette of affected rooms to 15.
///		</description>
///	</item>
/// <item>
///		<term>Grouping</term> 
///		<description>
///			The set of rooms an instance is active in.
///			This Happen will activate in group called <c>first</c>, and additionally in rooms <c>SU_A41</c> and <c>SU_A42</c>, 
///			but will not be activated in <c>SU_A22</c> if <c>SU_A22</c> is present in the group. 
///			See <seealso cref="HappenSet"/> to see how Happens and Rooms are grouped together.
///		</description>
///	</item>
/// <item>
///		<term><see cref="condition"/></term> 
///		<description>
///			A <seealso cref="HappenTrigger"/> created from the WHEN expression,
///			which determines when the Happen should be active or not. 
///			This happen will be active when player's karma level is 1, 2 or 10. 
///			See code of <seealso cref="PredicateInlay"/> if you want to know how the expression is parsed.
///		</description>
///	</item>
/// </list>
/// </para>
/// </summary>
public sealed class Happen : IEquatable<Happen>, IComparable<Happen>
{
	internal const int PROFILER_CYCLE_COREUP = 200;
	internal const int PROFILER_CYCLE_REALUP = 400;
	internal const int STORE_CYCLES = 12;
	#region fields/props
	#region perfrec
	internal readonly LinkedList<double> realup_readings = new();
	internal readonly List<TimeSpan> realup_times = new(PROFILER_CYCLE_REALUP);
	internal readonly LinkedList<double> haeval_readings = new();
	internal readonly List<TimeSpan> haeval_times = new(PROFILER_CYCLE_COREUP);
	#endregion perfrec
	/// <summary>
	/// Displays whether a happen is active during the current frame. Updated on <see cref="Atmod.DoBodyUpdates(On.RainWorldGame.orig_Update, RainWorldGame)"/>.
	/// </summary>
	public bool Active { get; private set; }
	/// <summary>
	/// Whether the init callbacks have been invoked or not.
	/// </summary>
	public bool InitRan { get; internal set; }
	/// <summary>
	/// HappenSet this Happen is associated with.
	/// Ownership may change when merging atmo files from different regpacks.
	/// </summary>
	public HappenSet Set { get; internal set; }
	/// <summary>
	/// Used internally for sorting.
	/// </summary>
	internal readonly Guid _guid = Guid.NewGuid();
	/// <summary>
	/// Used for frame time profiling.
	/// </summary>
	internal readonly System.Diagnostics.Stopwatch _sw = new();
	#region fromcfg
	/// <summary>
	/// Activation expression. Populated by <see cref="HappenTrigger.Active"/> callbacks of items in <see cref="triggers"/>.
	/// </summary>
	public HappenTrigger condition;
	/// <summary>
	/// All triggers associated with the happen.
	/// </summary>
	public readonly List<HappenAction> actions = new();
	/// <summary>
	/// name of the happen.
	/// </summary>
	public readonly string name;
	/// <summary>
	/// A set of actions with their parameters.
	/// </summary>
	//public readonly Dictionary<string, string[]> actions;
	/// <summary>
	/// Current game instance.
	/// </summary>
	public readonly RainWorldGame game;
	#endregion fromcfg
	#endregion fields/props
	/// <summary>
	/// Creates a new instance from given config, set and game reference.
	/// </summary>
	/// <param name="cfg">A config containing basic setup info.
	/// Make sure it is properly instantiated, and none of the fields are unexpectedly null.</param>
	/// <param name="owner">HappenSet this happen will belong to. Must not be null.</param>
	/// <param name="game">Current game instance. Must not be null.</param>
	public Happen(string name, HappenSet owner, RainWorldGame game)
	{
		BangBang(owner, nameof(owner));
		BangBang(game, nameof(game));
		Set = owner;
		this.name = name;
		this.game = game;
	}

	public void AddActions(List<HappenAction> actions)
	{
		this.actions.AddRange(actions);
	}

	public void AddTrigger(HappenTrigger? trigger)
	{
		if (condition is not null)
		{
			LogfixWarning("HappenParse: Duplicate WHEN clause! Skipping! (Did you forget to close a previous Happen with END HAPPEN?)");
			return;
		}
		condition = trigger!;
	}

	public bool Finalize()
	{
		if (actions.Count is 0) LogfixWarning($"Happen {this}: no actions! Possible missing 'WHAT:' clause");
		if (condition is null) LogfixWarning($"Happen {this}: did not receive conditions! Possible missing 'WHEN:' clause");
		return condition is not null;
	}

	#region lifecycle cbs
	internal void AbstUpdate(AbstractRoom absroom, int time)
	{
	}
	internal void RealizedUpdate(Room room)
	{
		_sw.Start();
		LogMessage("action count is " + actions.Count);
		foreach (HappenAction action in actions)
		{
			LogMessage("action realized update");
			try
			{
				action.RealizedUpdate(room);
			}
			catch (Exception e) { LogError("exception in action\n" + e); }
		}
		LogFrameTime(realup_times, _sw.Elapsed, realup_readings, STORE_CYCLES);
		_sw.Reset();
	}

	internal void Init(World world)
	{
		InitRan = true;
		foreach (HappenAction action in actions)
		{
			try
			{
				action.Init();
			}
			catch (Exception ex)
			{
				LogError(ErrorMessage(
					where: Site.init,
					cb: action.Init,
					ex: ex,
					resp: Response.none
					));
			}
		}
	}
	internal void CoreUpdate()
	{
		_sw.Start();
		try
		{
			condition?.Update();
		}
		catch (Exception ex)
		{
			LogError(ErrorMessage(where: Site.triggerupdate, cb: condition.Update, ex: ex, resp: Response.void_trigger));
		}
		try
		{
			Active = condition?.Active() ?? true;
		}
		catch (Exception ex)
		{
			LogError(ErrorMessage( where: Site.eval, cb: condition.Active, ex: ex, resp: Response.none));
		}
		foreach (HappenAction action in actions)
		{ action.AbstractUpdate(); }

		LogFrameTime(haeval_times, _sw.Elapsed, haeval_readings, STORE_CYCLES);
		_sw.Reset();
	}
	#endregion
	public bool AffectsRoom(AbstractRoom? room) => room is not null ? this.Set.GetRoomsForHappen(this).Contains(room.name) : false;
	/// <summary>
	/// Returns a performance report struct.
	/// </summary>
	/// <returns></returns>
	public Perf PerfRecord()
	{
		Perf perf = new()
		{
			name = name,
			samples_eval = haeval_readings.Count,
			samples_realup = realup_readings.Count
		};
		double
			realuptotal = 0d,
			evaltotal = 0d;
		if (perf.samples_realup is not 0)
		{
			foreach (double rec in realup_readings) realuptotal += rec;
			perf.avg_realup = realuptotal / realup_readings.Count;
		}
		else
		{
			perf.avg_realup = double.NaN;
		}
		if (perf.samples_eval is not 0)
		{
			foreach (double rec in haeval_readings) evaltotal += rec;
			perf.avg_eval = evaltotal / haeval_readings.Count;
		}
		else
		{
			perf.avg_eval = double.NaN;
		}
		return perf;
	}
	#region general
	/// <summary>
	/// Compares to another happen using GUIDs.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public int CompareTo(Happen other)
	{
		return _guid.CompareTo(other._guid);
	}
	/// <summary>
	/// Compares to another happen using GUIDs.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(Happen other)
	{
		return _guid.Equals(other._guid);
	}
	/// <summary>
	/// Returns a string representation of the happen.
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return $"{name}" +
			$"[{(actions.Count == 0 ? string.Empty : actions.Select(x => $"{x}").Aggregate(JoinWithComma))}]" +
			$"({condition} triggers)";
	}
	#endregion
	#region nested
	/// <summary>
	/// Carries performance report from the happen.
	/// </summary>
	public record struct Perf
	{
		/// <summary>
		/// Happen name
		/// </summary>
		public string name;
		/// <summary>
		/// Average real update frame time
		/// </summary>
		public double avg_realup;
		/// <summary>
		/// Number of recorded real update frame time samples
		/// </summary>
		public int samples_realup;
		/// <summary>
		/// Average eval invocation time
		/// </summary>
		public double avg_eval;
		/// <summary>
		/// Number of recorded eval frame time samples
		/// </summary>
		public int samples_eval;
	}
	private enum Site
	{
		abstup,
		realup,
		coreup,
		init,
		eval,
		eval_res,
		triggerupdate,
	}
	private enum Response
	{
		none,
		remove_cb,
		void_trigger
	}
	#endregion
	private string ErrorMessage(
		Site where,
		Delegate? cb,
		Exception ex,
		Response resp = Response.remove_cb)
	{
		return $"Happen {this}: {where}: " +
			$"Error on invoke {cb}//{cb?.Method}:" +
			$"\n{ex}" +
			$"\nAction taken: " + resp switch
			{
				Response.none => "none.",
				Response.remove_cb => "removing problematic callback.",
				Response.void_trigger => "voiding trigger.",
				_ => "???",
			};
	}
}
