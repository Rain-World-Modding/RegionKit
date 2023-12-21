namespace RegionKit.Modules.Machinery.V1;
/// <summary>
/// POM data for a single piston
/// </summary>
public class PistonData : BaseMachineryData
{
	#pragma warning disable 1591
	[EnumField<OscillationMode>("1opmode", OscillationMode.Sinal, displayName: "Operation mode")]
	public OscillationMode opmode;
	[FloatField("6rot", -180f, 180f, 0f, increment: 1f, displayName: "Direction", control: ManagedFieldWithPanel.ControlType.slider)]
	public float rotation = 0f;
	[FloatField("3amp", 0f, 120f, 20f, increment: 1f, displayName: "Amplitude", control: ManagedFieldWithPanel.ControlType.text)]
	public float amplitude = 20f;
	[BooleanField("2align_rot", true, displayName: "Straight angles only", control: ManagedFieldWithPanel.ControlType.button)]
	public bool align = false;
	[FloatField("5phase", -5f, 5f, 0f, displayName: "Phase", control: ManagedFieldWithPanel.ControlType.text)]
	public float phase = 0f;
	[FloatField("4frequency", 0.05f, 2f, 1f, displayName: "Frequency", increment: 0.05f, control: ManagedFieldWithPanel.ControlType.text)]
	public float frequency = 1f;
	public Vector2 forcePos;
	#pragma warning restore 1591
	public PistonData(PlacedObject? owner) : base(owner!, null)
	{

	}

	internal void BringToKin(PistonData other)
	{
		other.opmode = this.opmode;
		other.rotation = this.rotation;
		other.amplitude = this.amplitude;
		//other.sharpFac = this.sharpFac;
		other.align = this.align;
		other.phase = this.phase;
		other.frequency = this.frequency;
		other.forcePos = this.forcePos;
	}
}
