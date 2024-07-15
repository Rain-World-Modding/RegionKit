namespace RegionKit.Modules.Atmo.Body;

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
/// <see cref="EventfulTrigger.ShouldRunUpdates"/> defaults to false if callback is null.
/// </summary>
public sealed class EventfulTrigger : HappenTrigger {
	/// <summary>
	/// Attach to this to fill in the behaviour of <see cref="EvalResults(bool)"/>.
	/// </summary>
	public Action<bool>? On_EvalResults;
	/// <summary>
	/// Attach to this to fill in the behaviour of <see cref="Update"/>.
	/// </summary>
	public Action? On_Update;
	/// <summary>
	/// Attach to this to fill in the behaviour of <see cref="ShouldRunUpdates"/>. False if null.
	/// </summary>
	public Func<bool>? On_ShouldRunUpdates;
	/// <inheritdoc/>
	public override void EvalResults(bool res) {
		On_EvalResults?.Invoke(res);
	}

	/// <inheritdoc/>
	public override void Update() {
		On_Update?.Invoke();
	}

	/// <inheritdoc/>
	public override bool ShouldRunUpdates() {
		return On_ShouldRunUpdates?.Invoke() ?? false;
	}
}
