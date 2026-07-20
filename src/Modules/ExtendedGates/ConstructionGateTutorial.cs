namespace RegionKit.Modules.ExtendedGates
{
	public class ConstructionGateTutorial : UpdatableAndDeletable
	{
		public ConstructionGateTutorial(Room room)
		{
			this.room = room;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			if (ExtendedGates.hasSeenConstructionGateTutorial) Destroy();
			if (room.game.session is StoryGameSession && room.game.cameras[0].hud != null && room.game.Players.Count > 0 && room.game.Players[0].realizedCreature != null && room.game.Players[0].realizedCreature.room == room)
			{
				ExtendedGates.hasSeenConstructionGateTutorial = true;
				room.game.cameras[0].hud.textPrompt.AddMessage(room.game.rainWorld.inGameTranslator.Translate("You do not have the region this gate connects to."), 40, 160, true, true);
				if (room.game.cameras[0].hud.textPrompt.subregionTracker != null)
				{
					room.game.cameras[0].hud.textPrompt.subregionTracker.lastShownRegion = 1;
				}
				Destroy();
			}
		}
	}
}
