using DevInterface;

namespace RegionKit.Modules.Triggers
{
	public class ScavengerOutpostTrigger : EventTrigger, ICustomTrigger
	{
		public ScavengerOutpostTrigger() : base(_Enums.ScavengerOutpostTrigger)
		{
		}

		public bool PerformWait => false;

		public bool CheckCondition(Player player, Room room)
		{
			if (room.world.scavengersWorldAI == null || !room.abstractRoom.scavengerOutpost)
			{
				return false;
			}
			ScavengersWorldAI.Outpost thisOutpost = room.world.scavengersWorldAI.outPosts.FirstOrDefault(x => x.room == room.abstractRoom.index);
			return thisOutpost != null && thisOutpost.feePayed > 9;
		}

		public void InitAtPosition(Vector2 pos)
		{
		}

		public void InitDevUI(TriggerPanel triggerPanel)
		{
		}
	}
}
