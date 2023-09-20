namespace RegionKit.Modules.RoomSlideShow;

public sealed class SlideShowRectData : ManagedData
{
	[StringField("00id", "test", "Id")]
	public string id = "test";
	[Vector2Field("01p2", 100f, 100f, Vector2Field.VectorReprType.rect)]
	public Vector2 p2;
	public SlideShowRectData(PlacedObject owner) : base(owner, null)
	{

	}

}
