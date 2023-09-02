namespace RegionKit.Modules.Slideshow;

public sealed class SlideShowMeshData : ManagedData
{
	[StringField("00id", "test", "Id")]
	public string id = "test";
	[Vector2ArrayField("01quad", 4, true, Vector2ArrayField.Vector2ArrayRepresentationType.Polygon, 0f, 0f, 100f, 0f, 100f, 100f, 0f, 100f)]
	public Vector2[] quad = { };
	public SlideShowMeshData(PlacedObject owner) : base(owner, null)
	{
	}
}

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
