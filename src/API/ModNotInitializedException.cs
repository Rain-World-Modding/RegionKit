namespace RegionKit.API;

public class ModNotInitializedException : Exception
{
	public ModNotInitializedException(string? message = null) : base(message ?? "RegionKit has not been initialized; add a BepInDependency attributeto your plugin to make sure RegionKit loads first.")
	{

	}
	public static void ThrowIfModNotInitialized()
	{
		if (__inst is null) throw new ModNotInitializedException();
	}
}
