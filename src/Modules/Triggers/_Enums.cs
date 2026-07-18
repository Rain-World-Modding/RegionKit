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
		public static readonly TriggeredEvent.EventType RegionBumpEvent = new("RegionBump", true);
		public static readonly TriggeredEvent.EventType ExplodeEvent = new("Explode", true);
		
		/* Future trigger ideas:
		 *   - Trading with a scav
		 *   - Killing a creature (including potentially a specific kind)
		 *   - Eating food (including a specific food)
		 *   - Time in cycle (e.g. mid-cycle, after dusk, etc)
		 * 
		 * Future event ideas:
		 *   - Lock/unlock shortcut
		 *   - Summon an existing vulture to room (like vulture grub)
		 *       - Allow chances for specific kinds of vultures if applicable
		 *   - Power enable/disable
		 *   - Text popup and/or dialogue, customizable via string control instead of text file
		 *   - Lights turn on/off
		 */
	}
}
