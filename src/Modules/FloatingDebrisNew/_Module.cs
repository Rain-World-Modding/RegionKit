using Watcher;

namespace RegionKit.Modules.FloatingDebrisNew;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Floating Debris")]
internal static class _Module
{
	internal static void Enable()
	{
		try
		{
			LoadShaders();

			FloatingDebris.types["RK Dust"] = new Dust.DustSpawner(false);
			FloatingDebris.types["RK White Dust"] = new Dust.DustSpawner(true);

			IFloaterExtraData.Implementation.Enable();
		}
		catch (Exception ex)
		{
			LogError(ex);
		}
	}

	internal static void Disable()
	{
		try
		{
			FloatingDebris.types.Remove("RK Dust");
			FloatingDebris.types.Remove("RK White Dust");

			IFloaterExtraData.Implementation.Disable();
		}
		catch (Exception ex)
		{
			LogError(ex);
		}
	}

	private static void LoadShaders()
	{
		AssetBundle bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/rkfloatingdebris"));
		Custom.rainWorld.Shaders["RKDust"] = FShader.CreateShader("RKDust", bundle.LoadAsset<Shader>("Assets/Shaders/FloatingDust.shader"));
		Custom.rainWorld.Shaders["RKWhiteDust"] = FShader.CreateShader("RKWhiteDust", bundle.LoadAsset<Shader>("Assets/Shaders/FloatingDust.shader"), ["lightdust"]);
	}
}
