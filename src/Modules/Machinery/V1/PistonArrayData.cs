namespace RegionKit.Modules.Machinery.V1;
/// <summary>
/// POM data for a linear array of pistons
/// </summary>
public class PistonArrayData : BaseMachineryData
{
	#pragma warning disable 1591

	[IntegerField("count", 1, 35, 3, displayName: "Piston count")]
	public int pistonCount;
	[FloatField("relrot", -90f, 90f, 0f, increment: 0.5f, displayName: "Relative rotation", control: ManagedFieldWithPanel.ControlType.text)]
	public float relativeRotation;
	[Vector2Field("point2", 30f, 30f, Vector2Field.VectorReprType.line)]
	public Vector2 point2;
	[FloatField("amp", 0f, 120f, 20f, increment: 1f, displayName: "Amplitude", control: ManagedFieldWithPanel.ControlType.slider)]
	public float amplitude;
	[BooleanField("align_rot", true, displayName: "Straight angles only", control: ManagedFieldWithPanel.ControlType.button)]
	public bool align;
	[FloatField("phaseInc", -5f, 5f, 0f, displayName: "Phase increment", control: ManagedFieldWithPanel.ControlType.slider)]
	public float phaseIncrement;
	[FloatField("frequency", 0.05f, 2f, 1f, displayName: "Frequency", increment: 0.05f)]
	public float frequency;
	#pragma warning restore 1591
	/// <summary>
	/// pom constructor
	/// </summary>
	/// <param name="owner"></param>
	/// <returns></returns>
	public PistonArrayData(PlacedObject owner) : base(owner, null)
	{

	}
}
