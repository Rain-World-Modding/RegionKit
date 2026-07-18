namespace RegionKit.Modules.Triggers
{
	/// <summary>
	/// For <see cref="TriggeredEvent"/>s that require access to the room before being triggered.
	/// </summary>
	public interface IBeforeTriggerUpdate
	{
		/// <summary>
		/// Update method called before this event has been fired.
		/// </summary>
		/// <param name="room">Room</param>
		public void PreTriggerUpdate(Room room);
	}
}
