using CoralBrain;

namespace RegionKit.Modules.Objects;

/// <summary>
/// By LB/M4rbleL1ne
/// Adds a projected circle, requires sunblock
/// </summary>
public class ProjectedCircleObject : UpdatableAndDeletable, IOwnProjectedCircles
{
	private readonly PlacedObject _pObj;

	///<inheritdoc/>
	public ProjectedCircleObject(Room room, PlacedObject pObj)
	{
		this.room = room;
		_pObj = pObj;
		room.AddObject(new ProjectedCircle(room, this, 0, 180f));
	}

	bool IOwnProjectedCircles.CanHostCircle() => true;

	Vector2 IOwnProjectedCircles.CircleCenter(int index, float timeStacker) => _pObj.pos;

	Room IOwnProjectedCircles.HostingCircleFromRoom() => room;
}
