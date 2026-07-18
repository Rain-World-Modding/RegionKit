using DevInterface;

namespace RegionKit.Modules.Triggers
{
	/// <summary>
	/// Custom code for event triggers
	/// </summary>
	public interface ICustomTrigger
	{
		/// <summary>
		/// Whether to perform the initial wait countdown (equivalent to 0.25 seconds).
		/// By default, this is only used by Spot triggers.
		/// </summary>
		public bool PerformWait { get; }

		/// <summary>
		/// Whether the condition for the event passes
		/// </summary>
		/// <param name="player">Player being checked</param>
		/// <param name="room">Room the event is in</param>
		/// <returns>Whether the custom condition passes</returns>
		public bool CheckCondition(Player player, Room room);

		/// <summary>
		/// Initializes the trigger at the position if additional positions need to be created
		/// </summary>
		/// <param name="pos">The position the trigger is being created at in room coordinates</param>
		public void InitAtPosition(Vector2 pos);

		/// <summary>
		/// Init custom dev UI for this trigger.
		/// </summary>
		/// <param name="triggerPanel">Panel owner</param>
		public void InitDevUI(TriggerPanel triggerPanel);
	}
}
