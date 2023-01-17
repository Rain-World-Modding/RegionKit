//todo: support for named loggers
public class RegionKitModuleAttribute : Attribute
{
	internal readonly string enableMethod;
	internal readonly string disableMethod;
	internal readonly string? tickMethod;
	internal readonly int tickPeriod;
	internal readonly string? moduleName;

	public RegionKitModuleAttribute(
		string enableMethod,
		string disableMethod,
		string? tickMethod = null,
		int tickPeriod = 1,
		string? moduleName = null)
	{
		this.enableMethod = enableMethod;
		this.disableMethod = disableMethod;
		this.tickMethod = tickMethod;
		this.tickPeriod = tickPeriod;
		this.moduleName = moduleName;
	}
}
