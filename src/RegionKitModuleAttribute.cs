
public class RegionKitModuleAttribute : Attribute
{
	private readonly object enableMethod;
	private readonly string disableMethod;

	public RegionKitModuleAttribute(object enableMethod, string disableMethod)
	{
		this.enableMethod = enableMethod;
		this.disableMethod = disableMethod;
	}
}
