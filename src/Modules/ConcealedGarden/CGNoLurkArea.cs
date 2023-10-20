using System;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;

internal static class CGNoLurkArea
{
	public static ManagedObjectType? noLurkType;

	public static void Apply()
	{
		On.LizardAI.LurkTracker.LurkPosScore += LurkPosScore_Hk;
	}
	public static void Undo()
	{
		On.LizardAI.LurkTracker.LurkPosScore -= LurkPosScore_Hk;
	}

	private static float LurkPosScore_Hk(On.LizardAI.LurkTracker.orig_LurkPosScore orig, LizardAI.LurkTracker self, WorldCoordinate testLurkPos)
	{
		float retval = orig(self, testLurkPos);
		if (testLurkPos.room == self.lizard.abstractCreature.pos.room)
		{
			Vector2 lurkPos = self.lizard.room.MiddleOfTile(testLurkPos);
			PlacedObject.Type? nolurktype = noLurkType?.GetObjectType();
			foreach (var item in self.lizard.room.roomSettings.placedObjects)
			{
				if (item.active && item.type == nolurktype)
				{
					if (RWCustom.Custom.DistLess(lurkPos, item.pos, ((CGNoLurkAreaData)item.data).handle.magnitude))
					{
						//LogMessageError("NO LURK");
						return -100000f;
					}
				}
			}
		}
		return retval;
	}

	public class CGNoLurkAreaData : ManagedData
	{
		private static ManagedField[] paramFields = new ManagedField[]
		{
				new Vector2Field("handle", new UnityEngine.Vector2(-100f, 40f), Vector2Field.VectorReprType.circle)
		};
		[BackedByField("handle")]
		public Vector2 handle;
		public CGNoLurkAreaData(PlacedObject owner) : base(owner, paramFields) { }
	}

	//private PlacedObject pObj;

	//public NoLurkArea(Room room, PlacedObject pObj)
	//{
	//    this.room = room;
	//    this.pObj = pObj;
	//}
}
