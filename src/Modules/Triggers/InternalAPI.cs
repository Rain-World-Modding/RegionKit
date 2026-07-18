namespace RegionKit.Modules.Triggers
{
	internal static class InternalAPI
	{
		private static Dictionary<EventTrigger.TriggerType, Func<EventTrigger>> _eventTriggerMap = [];
		private static Dictionary<TriggeredEvent.EventType, Func<TriggeredEvent>> _triggeredEventMap = [];

		public static void RegisterTrigger(EventTrigger.TriggerType type, Func<EventTrigger> factory) => _eventTriggerMap.Add(type, factory);
		public static void RegisterEvent(TriggeredEvent.EventType type, Func<TriggeredEvent> factory) => _triggeredEventMap.Add(type, factory);

		public static bool UnregisterTrigger(EventTrigger.TriggerType type) => _eventTriggerMap.Remove(type);
		public static bool UnregisterEvent(TriggeredEvent.EventType type) => _triggeredEventMap.Remove(type);

		public static EventTrigger? MaybeGetTriggerFromAPI(EventTrigger.TriggerType triggerType)
		{
			if (_eventTriggerMap.Count == 0) return null;
			return _eventTriggerMap.TryGetValue(triggerType, out Func<EventTrigger>? factory) ? factory.Invoke() : null;
		}

		public static TriggeredEvent? MaybeGetEventFromAPI(TriggeredEvent.EventType eventType)
		{
			if (_triggeredEventMap.Count == 0) return null;
			return _triggeredEventMap.TryGetValue(eventType, out Func<TriggeredEvent>? factory) ? factory.Invoke() : null;
		}
	}
}
