namespace RegionKit.Modules.Slideshow;

public sealed class SlideShowData : ManagedData
{
	[StringField("00id", "test", "Id")]
	public string id = "test";
	[Vector2ArrayField("01quad", 4, true, Vector2ArrayField.Vector2ArrayRepresentationType.Polygon, 0f, 0f, 100f, 0f, 100f, 100f, 0f, 100f)]
	public Vector2[] quad = { };
	// [EnumField<ContainerCodes>("02cont", ContainerCodes.Foreground, null, ManagedFieldWithPanel.ControlType.arrows, "Container")]
	// public ContainerCodes container;
	public SlideShowData(PlacedObject owner) : base(owner, null)
	{
	}
}

