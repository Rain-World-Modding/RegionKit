namespace RegionKit.Modules.Climbables;

public class ClimbablePoleH : ClimbablePoleG
{
	public ClimbablePoleH(PlacedObject placedObject, Room instance) : base(placedObject, instance)
	{

	}

	protected override int GetStartIndex()
	{
		return rect.left;
	}

	protected override int GetStopIndex()
	{
		return rect.right;
	}

	protected override Room.Tile GetTile(int i)
	{
		return room.GetTile(i, rect.bottom);
	}

	protected override bool getPole(Room.Tile tile)
	{
		return tile.horizontalBeam;
	}

	protected override void setPole(bool value, Room.Tile tile)
	{
		tile.horizontalBeam = value;
	}

	protected override Room.Tile lastTile(int i)
	{
		return room.GetTile(i, lastRect.bottom);
	}

	protected override int GetLastStopIndex()
	{
		return lastRect.right;
	}

	protected override int GetLastStartIndex()
	{
		return lastRect.left;
	}

	protected override Vector2 getWidth()
	{
		return new Vector2(0, 4);
	}

	protected override Vector2 getStart()
	{
		return new Vector2((float)rect.left * 20f, (float)rect.bottom * 20f + 8f);
	}

	protected override Vector2 getEnd()
	{
		return new Vector2((float)rect.right * 20f, (float)rect.bottom * 20f + 8f);
	}
}
