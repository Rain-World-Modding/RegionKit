namespace RegionKit.Modules.Machinery.V1;
/// <summary>
/// POM data for spinny thing
/// </summary>
public class SimpleCogData : BaseMachineryData
{
	internal Vector2 forcepos;
	internal OperationMode opmode => GetValue<OperationMode>("opmode");
	[FloatField("AVSamp", 0.1f, 15f, 1f, increment: 0.1f, displayName: "AV shift amplitude", control: ManagedFieldWithPanel.ControlType.text)]
	internal float angVelShiftAmp;
	[FloatField("AVSfrq", 0.1f, 3f, 1f, increment: 0.05f, displayName: "AV shift frequency", control: ManagedFieldWithPanel.ControlType.text)]
	internal float angVelShiftFrq;
	//[FloatField("AVphs", -5f, 5f, 0f, increment:0.1f, displayName: "AV shift phase")]
	//internal float angVelShiftPhs;
	[FloatField("AVbase", -30f, 30f, 10f, increment: 0.2f, displayName: "Angular velocity (AV)", control: ManagedFieldWithPanel.ControlType.text)]
	internal float baseAngVel;
	internal float rad;

	public SimpleCogData(PlacedObject? owner) : base(owner, new ManagedField[]
	{
			new EnumField<OperationMode>("opmode", OperationMode.Cosinal, displayName:"Operation mode")
	})
	{

	}
}
