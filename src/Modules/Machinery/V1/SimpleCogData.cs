namespace RegionKit.Modules.Machinery.V1;
/// <summary>
/// POM data for spinny thing
/// </summary>
public class SimpleCogData : BaseMachineryData
{
	#pragma warning disable 1591
	public Vector2 forcepos;
	[EnumField<OperationMode>("opmode", OperationMode.Cosinal, displayName: "Operation mode")]
	public OperationMode opmode;
	[FloatField("AVSamp", 0.1f, 15f, 1f, increment: 0.1f, displayName: "AV shift amplitude", control: ManagedFieldWithPanel.ControlType.text)]
	public float angVelShiftAmp;
	[FloatField("AVSfrq", 0.1f, 3f, 1f, increment: 0.05f, displayName: "AV shift frequency", control: ManagedFieldWithPanel.ControlType.text)]
	public float angVelShiftFrq;
	//[FloatField("AVphs", -5f, 5f, 0f, increment:0.1f, displayName: "AV shift phase")]
	//public float angVelShiftPhs;
	[FloatField("AVbase", -30f, 30f, 10f, increment: 0.2f, displayName: "Angular velocity (AV)", control: ManagedFieldWithPanel.ControlType.text)]
	public float baseAngVel;
	#pragma warning restore 1591
	//public float rad;
	/// <summary>
	/// POM ctor
	/// </summary>
	public SimpleCogData(PlacedObject? owner) : base(owner!, null)
	{

	}
}
