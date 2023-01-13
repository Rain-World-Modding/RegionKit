
internal class RegionKitModuleAttribute : Attribute
{
	internal readonly string enableMethod;
	internal readonly string disableMethod;
	internal readonly string? moduleNameOverride;

	internal RegionKitModuleAttribute(string enableMethod, string disableMethod, string? moduleNameOverride = null)
	{
		this.enableMethod = enableMethod;
		this.disableMethod = disableMethod;
		this.moduleNameOverride = moduleNameOverride;
	}
}
