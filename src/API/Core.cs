namespace RegionKit.API;

/// <summary>
/// Some of the mod's base functionality.
/// </summary>
public static class Core
{
	/// <summary>
	/// Returns information about all loaded RegionKit modules.
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<RegionKit.ModuleInfo>? TryGetLoadedModules()
	{
		ThrowIfModNotInitialized();
		return RegionKit.Mod.__inst._modules;
	}
	public const string VERSION = MOD_VERSION;
	public const string GUID = MOD_GUID;

}