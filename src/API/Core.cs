namespace RegionKit.API;

public static class Core
{
	public static IEnumerable<RegionKit.ModuleInfo>? TryGetLoadedModules()
	{
		ThrowIfModNotInitialized();
		return RegionKit.Mod.__inst._modules;
	}

}