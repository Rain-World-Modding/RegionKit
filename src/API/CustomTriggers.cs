using RegionKit.Modules.Triggers;

namespace RegionKit.API
{
	/// <summary>
	/// Public API for adding custom triggers and events to the game through RegionKit.
	/// It is also recommended for your custom triggers and events to use <see cref="ICustomTrigger"/> and <see cref="ICustomEvent"/> so they can be invoked automatically.
	/// </summary>
	public static class CustomTriggers
	{
		/// <summary>
		/// Registers a custom event trigger.
		/// </summary>
		/// <param name="triggerType">Trigger type</param>
		/// <param name="triggerFactory">Function that creates an event trigger.</param>
		/// <remarks>
		/// It is recommended that your EventType implements <see cref="ICustomTrigger"/> so that RegionKit can handle additional related logic,
		/// including creating the dev tools interface and running checks for the custom condition.
		/// </remarks>
		public static void RegisterCustomEventTrigger(EventTrigger.TriggerType triggerType, Func<EventTrigger> triggerFactory)
		{
			InternalAPI.RegisterTrigger(triggerType, triggerFactory);
		}

		/// <summary>
		/// Registers a custom triggered event
		/// </summary>
		/// <param name="eventType">Event type</param>
		/// <param name="eventFactory">Function that creates a triggered event object</param>
		/// <remarks>
		/// It is recommended that your TriggeredEvent implements <see cref="ICustomEvent"/> so that RegionKit can handle additional related logic,
		/// including creating the dev tools interface and firing when completed.
		/// </remarks>
		public static void RegisterCustomTriggeredEvent(TriggeredEvent.EventType eventType, Func<TriggeredEvent> eventFactory)
		{
			InternalAPI.RegisterEvent(eventType, eventFactory);
		}

		/// <summary>
		/// Unregisters a custom event trigger if it is registered.
		/// </summary>
		/// <param name="triggerType">The trigger type to unregister</param>
		/// <returns>Whether or not the event trigger was successfully unregistered</returns>
		public static bool UnregisterCustomEventTrigger(EventTrigger.TriggerType triggerType)
		{
			return InternalAPI.UnregisterTrigger(triggerType);
		}

		/// <summary>
		/// Unregisters a custom triggered event if it is registered
		/// </summary>
		/// <param name="eventType">The event type to unregister</param>
		/// <returns>Whether or not the triggered event was successfully unregistered</returns>
		public static bool UnregisterCustomTriggeredEvent(TriggeredEvent.EventType eventType)
		{
			return InternalAPI.UnregisterEvent(eventType);
		}
	}
}
