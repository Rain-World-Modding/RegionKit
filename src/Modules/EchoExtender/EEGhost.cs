namespace RegionKit.Modules.EchoExtender
{
	public class EEGhost : Ghost
	{
		public EchoSettings settings;
		public GhostWorldPresence.GhostID ghostID;
		public Conversation.ID conversationID;
		public string conversation;

		public EEGhost(Room room, PlacedObject placedObject, GhostWorldPresence worldGhost) : base(room, placedObject, worldGhost)
		{
			ghostID = worldGhost.ghostID;
			conversationID = EchoParser.GetConversationID(ghostID.value);
			EchoParser.__echoConversations.TryGetValue(conversationID, out conversation);

			settings = EchoParser.__echoSettings[ghostID];
			scale = settings.EchoSizeMultiplier * 0.75f;
			rags.conRad = 30f * scale;
			defaultFlip = settings.DefaultFlip;
		}

		public override void StartConversation()
		{
			if (room.game.cameras[0].hud.dialogBox == null)
			{
				room.game.cameras[0].hud.InitDialogBox();
			}
			currentConversation = new EEGhostConversation(conversationID, this, room.game.cameras[0].hud.dialogBox);
			conversationActive = true;
		}
	}
}
