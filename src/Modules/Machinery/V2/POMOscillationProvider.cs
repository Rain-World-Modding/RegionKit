namespace RegionKit.Modules.Machinery.V2;

public class POMOscillationProvider : ManagedData, IOscillationProvider
{

	[IntegerField("00tag", -100, 100, 0, displayName: "tag")]
	public int tag;
	[EnumField<OperationMode>("01mode", OperationMode.Sinal, displayName: "Operation mode")]
	public OperationMode opmode;

	[FloatField("02amp", 0f, 120f, 20f, increment: 1f, displayName: "Amplitude", control: ManagedFieldWithPanel.ControlType.text)]
	public float amplitude = 20f;
	[FloatField("03phase", -5f, 5f, 0f, displayName: "Phase", control: ManagedFieldWithPanel.ControlType.text)]
	public float phase = 0f;
	[FloatField("04frq", 0.05f, 2f, 1f, displayName: "Frequency", increment: 0.05f, control: ManagedFieldWithPanel.ControlType.text)]
	public float frequency = 1f;
	public POMOscillationProvider(PlacedObject owner) : base(owner, null)
	{
	}

	public int Tag => tag;

	public OscillationParams OscillationForNew()
	{
		return new(0f, amplitude, frequency, phase, opmode switch { OperationMode.Sinal => Mathf.Sin, OperationMode.Cosinal => Mathf.Cos, _ => throw new ArgumentException($"Invalid enum value {(int)opmode}") });
	}
}