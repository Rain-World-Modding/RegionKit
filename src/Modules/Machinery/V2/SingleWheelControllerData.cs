namespace RegionKit.Modules.Machinery.V2;

public class SingleWheelControllerData : ManagedData
{
	[FloatField("1rot", -180f, 180f, 0f, increment: 1f, control: ManagedFieldWithPanel.ControlType.slider, displayName: "Initial rotation")]
	public float rotation = 0f;
	[IntegerField("6visuals", -100, +100, 0, displayName: "Use visuals from tag")]
	public int visualsTag = 0;
	[IntegerField("7osc", -100, +100, 0, displayName: "Use oscillator from tag")]
	public int oscTag = 0;

	public SingleWheelControllerData(PlacedObject owner) : base(owner, null)
	{
	}
}
