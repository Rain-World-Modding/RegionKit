#nullable disable

namespace RegionKit.Modules.MoonIO
{
	public class Trigger : IOObject
	{
		public Vector2 size => (placedObject.data as TriggerType.TriggerData).Size;
		public string affects => (placedObject.data as TriggerType.TriggerData).Affects;


		public List<Creature> CreaturesInZone;
		public Trigger(PlacedObject placedObject, Room room) : base(placedObject)
		{
			this.room = room;

			CreaturesInZone = new List<Creature>();
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			for (int i = 0; i < room.abstractRoom.creatures.Count; i++)
			{
				Creature creature = room.abstractRoom.creatures[i].realizedCreature;
				if (creature == null)
				{
					continue;
				}

				int num = 0;
				for (int b = 0; b < creature.bodyChunks.Length; b++)
				{
					if (Custom.DistLess(placedObject.pos, creature.bodyChunks[b].pos, size.magnitude))
					{
						if (!CreaturesInZone.Contains(creature))
						{
							if (affects == "Players")
							{
								if (creature is Player p && p.AI == null)
								{
									SendOutput(MoonIO._Enums.OutputType.Output_Trigger);
									CreaturesInZone.Add(creature);
								}
							}
							else if (affects == "Creatures")
							{
								if (!(creature is Player) || (creature is Player p && p.AI != null))
								{
									SendOutput(MoonIO._Enums.OutputType.Output_Trigger);
									CreaturesInZone.Add(creature);
								}
							}
							else
							{
								SendOutput(MoonIO._Enums.OutputType.Output_Trigger);
								CreaturesInZone.Add(creature);
							}

							break;
						}
					}
					else
					{
						num++;
					}
				}

				if (num == creature.bodyChunks.Length && CreaturesInZone.Contains(creature))
				{
					CreaturesInZone.Remove(creature);
				}
			}
		}
	}
}
