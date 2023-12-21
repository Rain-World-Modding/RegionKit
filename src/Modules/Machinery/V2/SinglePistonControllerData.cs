namespace RegionKit.Modules.Machinery.V2;

public class SinglePistonControllerData : ManagedData, IOscillationProvider
{
#pragma warning disable 1591
	[EnumField<OperationMode>("0mode", OperationMode.Sinal, displayName: "Operation mode")]
	public OperationMode opmode;
	[FloatField("1rot", -180f, 180f, 0f, increment: 1f, displayName: "Direction", control: ManagedFieldWithPanel.ControlType.slider)]
	public float rotation = 0f;
	[FloatField("2amp", 0f, 120f, 20f, increment: 1f, displayName: "Amplitude", control: ManagedFieldWithPanel.ControlType.text)]
	public float amplitude = 20f;
	[BooleanField("3align_rot", true, displayName: "45deg angle increments", control: ManagedFieldWithPanel.ControlType.button)]
	public bool align = false;
	[FloatField("4phase", -5f, 5f, 0f, displayName: "Phase", control: ManagedFieldWithPanel.ControlType.text)]
	public float phase = 0f;
	[FloatField("5frequency", 0.05f, 2f, 1f, displayName: "Frequency", increment: 0.05f, control: ManagedFieldWithPanel.ControlType.text)]
	public float frequency = 1f;
	[IntegerField("6visuals", -100, +100, 0, displayName: "Use visuals from tag")]
	public int visualsTag = 0;
#pragma warning restore 1591
	public SinglePistonControllerData(PlacedObject owner) : base(owner, null)
	{

	}

	public OscillationParams OscillationForNew()
	{
		return new(0f, amplitude, frequency, phase, opmode switch { OperationMode.Sinal => Mathf.Sin, OperationMode.Cosinal => Mathf.Cos, _ => throw new ArgumentException($"Invalid enum value {(int)opmode}") });
	}
}

