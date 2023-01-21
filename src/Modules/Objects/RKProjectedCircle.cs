using CoralBrain;
using UnityEngine;

namespace RegionKit.Modules.Objects
{
	public class RKProjectedCircle
	{
		public static class Enums_ProjectedCircle
		{
			public static PlacedObject.Type ProjectedCircle = new("ProjectedCircle", true);
		}
	}

	public class ProjectedCircleObject : UpdatableAndDeletable, IOwnProjectedCircles
	{
		private readonly PlacedObject _pObj;

		public ProjectedCircleObject(Room room, PlacedObject pObj)
		{
			this.room = room;
			this._pObj = pObj;
			room.AddObject(new ProjectedCircle(room, this, 0, 180f));
		}

		public bool CanHostCircle() => true;

		public Vector2 CircleCenter(int index, float timeStacker) => _pObj.pos;

		public Room HostingCircleFromRoom() => this.room;
	}
}
