namespace RegionKit.Modules.Triggers
{
	public static class _Enums
	{
		// Trigger types
		public static readonly EventTrigger.TriggerType RectTrigger = new("Rect", true);
		public static readonly EventTrigger.TriggerType QuadTrigger = new("Quad", true);

		// Triggered events
		public static readonly TriggeredEvent.EventType ExplodeEvent = new("Explode", true);
	}
}
