using DevInterface;

namespace RegionKit.Modules.Triggers
{
	/// <summary>
	/// Custom code for triggered events
	/// </summary>
	public interface ICustomEvent
	{
		/// <summary>
		/// Default value to use for multi-use setting when initialized in dev tools
		/// </summary>
		public bool DefaultMultiUse { get; }

		/// <summary>
		/// Called when the event gets fired.
		/// </summary>
		/// <param name="trigger">The event trigger that got fired</param>
		/// <param name="room">The room the event trigger is in</param>
		public void Fire(EventTrigger trigger, Room room);

		/// <summary>
		/// Init custom dev UI panel for this event. Return <c>null</c> if no custom panel is needed (it will use the default event panel)
		/// </summary>
		/// <param name="triggerPanel">Parent to attach to</param>
		/// <returns>Instance of a dev UI panel</returns>
		public StandardEventPanel? InitDevUIPanel(TriggerPanel triggerPanel);
	}
}
