namespace RegionKit.Modules.Machinery.V1;

/// <summary>
/// base data type for machinery objects
/// </summary>
public abstract class BaseMachineryData : ManagedData
{
	/// <summary>
	/// Interlay ctor
	/// </summary>
	public BaseMachineryData(PlacedObject? owner, ManagedField[]? fields) : base(owner, fields) { }

	internal MachineryCustomizer? assignedCustomizer;
}
