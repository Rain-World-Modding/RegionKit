namespace RegionKit.Modules.EchoExtender;
///<inheritdoc/>
public static class _Enums
{
	/// <summary>
	/// EchoExtender's variant of GhostSpot
	/// </summary>
	/// <returns></returns>
	public static PlacedObject.Type EEGhostSpot = new("EEGhostSpot", true);

	/// <summary>
	/// manually overrides the EchoPresence for a room
	/// </summary>
	public static RoomSettings.RoomEffect.Type EchoPresenceOverride = new(nameof(EchoPresenceOverride), true);

	/// <summary>
	/// EchoExtender's Spinning Top conversation
	/// </summary>
	public static Conversation.ID EESpinningTopConversation = new(nameof(EESpinningTopConversation), true);
}
