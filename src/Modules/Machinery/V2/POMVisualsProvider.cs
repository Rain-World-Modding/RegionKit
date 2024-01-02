namespace RegionKit.Modules.Machinery.V2;

public class POMVisualsProvider : ManagedData, IVisualsProvider
{
	[IntegerField("00tag", -100, 100, 0, displayName: "tag")]
	public int tag;
	[StringField("01element", "pixel", "Atlas element")]
	public string element = "pixel";
	[StringField("02shader", "Basic", displayName: "Shader")]
	public string shader = "Basic";
	[EnumField<ContainerCodes>("03container", ContainerCodes.Items, displayName: "Container")]
	public ContainerCodes container;
	[FloatField("04scaleX", 0f, 10f, 1f, increment: 0.1f, ManagedFieldWithPanel.ControlType.slider, displayName: "X scale")]
	public float scaleX = 1f;
	[FloatField("05scaleY", 0f, 10f, 1f, increment: 0.1f, ManagedFieldWithPanel.ControlType.slider, displayName: "Y scale")]
	public float scaleY = 1f;
	[FloatField("06addRot", -90f, 90f, 0f, increment: 0.5f, displayName: "Additional rotation")]
	public float addedRotation = 0f;
	[ColorField("07color", 1f, 0f, 0f, 1f, DisplayName: "Color")]
	public Color color;
	[FloatField("08alpha", 0f, 1f, 1f, increment: 0.01f, ManagedFieldWithPanel.ControlType.slider, "Alpha")]
	public float alpha;
	[FloatField("09ancorhX", -10f, 10f, 0.5f, control: ManagedFieldWithPanel.ControlType.text, displayName: "X anchor")]
	public float anchorX;
	[FloatField("10anchorY", -10f, 10f, 0.5f, control: ManagedFieldWithPanel.ControlType.text, displayName: "Y anchor")]
	public float anchorY;
	public POMVisualsProvider(PlacedObject owner) : base(owner, null)
	{
	}

	public int Tag => tag;

	public PartVisuals VisualsForNew()
	{
		return new PartVisuals(
			element,
			shader,
			container,
			color,
			alpha,
			scaleX,
			scaleY,
			anchorX,
			anchorY,
			addedRotation);
	}
}
