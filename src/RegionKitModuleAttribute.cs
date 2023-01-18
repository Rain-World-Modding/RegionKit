//todo: support for named loggers
public class RegionKitModuleAttribute : Attribute
{
	internal readonly string _enableMethod;
	internal readonly string _disableMethod;
	internal readonly string? _tickMethod;
	internal readonly int _tickPeriod;
	internal readonly string? _moduleName;

	public RegionKitModuleAttribute(
		string enableMethod,
		string disableMethod,
		string? tickMethod = null,
		int tickPeriod = 1,
		string? moduleName = null)
	{
		this._enableMethod = enableMethod;
		this._disableMethod = disableMethod;
		this._tickMethod = tickMethod;
		this._tickPeriod = tickPeriod;
		this._moduleName = moduleName;
	}
}
