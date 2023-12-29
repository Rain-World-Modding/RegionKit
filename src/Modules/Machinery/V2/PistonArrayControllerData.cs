namespace RegionKit.Modules.Machinery.V2;

public class PistonArrayControllerData : ManagedData
{
	[IntegerField("00osc", -100, +100, 0, displayName: "Use oscillator from tag")]
	public int oscTag = 0;
	[StringField("01visuals", "0", "Use visualsf from tags (cycle)")]
	public string visualsTags = "0";
	[BooleanField("03alignAxis", false, displayName: "45deg increments (axis)", control: ManagedFieldWithPanel.ControlType.button)]
	public bool alignAxis = false;
	[BooleanField("04alignPistons", false, displayName: "45deg increments (pistons)", control: ManagedFieldWithPanel.ControlType.button)]
	public bool alignPistons = false;
	public int visualsTag = 0;
	[FloatField("08addrot", -90f, 90f, 0f, 1f, displayName: "Individual p. axis rot.")]
	public float addRot;
	[IntegerField("09count", 1, 50, 5, displayName: "Count")]
	public int count;
	[FloatField("10phasestep", -30f, 30f, 5f, displayName: "Phase step")]
	public float phaseStep;
	[Vector2Field("11p2", 40f, 20f)]
	public Vector2 p2;
	public PistonArrayControllerData(PlacedObject owner) : base(owner, null)
	{
	}
}
