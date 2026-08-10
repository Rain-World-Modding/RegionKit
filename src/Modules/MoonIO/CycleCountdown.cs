namespace RegionKit.Modules.MoonIO
{
	public class CycleCooldown : IOObject
	{
		public CycleCooldown(PlacedObject pObj, Room room) : base(pObj)
		{
			this.room = room;
		}

		public override void ReciveInput(_Enums.InputType type)
		{
			base.ReciveInput(type);

			if (type == _Enums.InputType.Input_Trigger)
			{
				SendOutput(_Enums.OutputType.Output_Trigger);
				if (room.world.game.session is StoryGameSession sgs)
				{
					sgs.saveState.ReportConsumedItem(room.world, false, room.abstractRoom.index, room.roomSettings.placedObjects.IndexOf(placedObject), (placedObject.data as CycleCooldownType.CycleCooldownData)!.Cooldown);
				}
			}
		}
	}
}
