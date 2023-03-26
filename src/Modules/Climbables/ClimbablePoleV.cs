namespace RegionKit.Modules.Climbables;

public class ClimbablePoleV : ClimbablePoleG
{
	public ClimbablePoleV(PlacedObject placedObject, Room instance) : base(placedObject, instance)
	{

	}

	protected override int GetStartIndex()
	{
		return rect.bottom;
	}

	protected override int GetStopIndex()
	{
		return rect.top;
	}

	protected override Room.Tile GetTile(int i)
	{
		return room.GetTile(rect.left, i);
	}

	protected override bool getPole(Room.Tile tile)
	{
		return tile.verticalBeam;
	}

	protected override void setPole(bool value, Room.Tile tile)
	{
		tile.verticalBeam = value;
	}

	protected override Room.Tile lastTile(int i)
	{
		return room.GetTile(lastRect.left, i);
	}

	protected override int GetLastStopIndex()
	{
		return lastRect.top;
	}

	protected override int GetLastStartIndex()
	{
		return lastRect.bottom;
	}

	protected override Vector2 getWidth()
	{
		return new Vector2(4, 0);
	}

	protected override Vector2 getStart()
	{
		return new Vector2((float)rect.left * 20f + 10f, (float)rect.bottom * 20f);
	}

	protected override Vector2 getEnd()
	{
		return new Vector2((float)rect.left * 20f + 10f, (float)rect.top * 20f);
	}
}
