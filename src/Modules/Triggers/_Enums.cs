namespace RegionKit.Modules.Triggers
{
	public static class _Enums
	{
		// Trigger types
		public static readonly EventTrigger.TriggerType RectTrigger = new("Rect", true);
		public static readonly EventTrigger.TriggerType QuadTrigger = new("Quad", true);
		public static readonly EventTrigger.TriggerType ScavengerOutpostTrigger = new("ScavengerOutpost", true);
		public static readonly EventTrigger.TriggerType PickUpObjectTrigger = new("PickUpObject", true);

		// Triggered events
		public static readonly TriggeredEvent.EventType SpawnCreatureEvent = new("SpawnCreature", true);
		public static readonly TriggeredEvent.EventType AddFadePaletteEvent = new("AddFadePalette", true);
		public static readonly TriggeredEvent.EventType PlaySoundEvent = new("PlaySound", true);
		public static readonly TriggeredEvent.EventType ExplodeEvent = new("Explode", true);
	}
}
