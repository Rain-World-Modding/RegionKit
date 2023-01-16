using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RWCustom;

namespace RegionKit.Modules.Objects
{
	//todo: apply
	//Made By LeeMoriya
	public class Enums_ARKillRect
	{
		public static PlacedObject.Type ARKillRect = new(nameof(ARKillRect), true);
	}

	public class ARKillRect : UpdatableAndDeletable
	{
		public PlacedObject po;
		public IntRect rect;

		public ARKillRect(Room room, PlacedObject pObj)
		{
			this.room = room;
			po = pObj;
			rect = (po.data as PlacedObject.GridRectObjectData)!.Rect;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			for (int i = 0; i < room.physicalObjects.Length; i++)
			{
				for (int j = 0; j < room.physicalObjects[i].Count; j++)
				{
					for (int k = 0; k < room.physicalObjects[i][j].bodyChunks.Length; k++)
					{
						if (Custom.InsideRect(room.GetTilePosition(room.physicalObjects[i][j].bodyChunks[k].pos), rect))
						{
							if (room.physicalObjects[i][j] is Creature crit)
							{
								if (!crit.dead)
								{
									crit.Die();
								}
							}
						}
					}
				}
			}
		}
	}
}
