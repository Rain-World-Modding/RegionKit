namespace RegionKit.Modules.Machinery.V2;

public class SinglePistonControllerData : ManagedData
{
#pragma warning disable 1591
	[FloatField("1rot", -180f, 180f, 0f, increment: 1f, displayName: "Direction", control: ManagedFieldWithPanel.ControlType.slider)]
	public float rotation = 0f;
	[BooleanField("3align_rot", true, displayName: "45deg angle increments", control: ManagedFieldWithPanel.ControlType.button)]
	public bool align = false;
	[IntegerField("6visuals", -100, +100, 0, displayName: "Use visuals from tag")]
	public int visualsTag = 0;
	[IntegerField("7osc", -100, +100, 0, displayName: "Use oscillator from tag")]
	public int oscTag = 0;
#pragma warning restore 1591
	public SinglePistonControllerData(PlacedObject owner) : base(owner, null)
	{
		
	}
}

