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
			FloatingDebris.types["RK Colored Dust"] = new ColoredDust.ColoredDustSpawner();
			FloatingDebris.types["RK Ripple Ring"] = new RKRippleSpawner("RippleRingMask", false);
			FloatingDebris.types["RK Ripple Ring Watcher"] = new RKRippleSpawner("RippleRingMask", true);
			FloatingDebris.types["RK Ripple Smooth"] = new RKRippleSpawner("RKSmoothRippleMask", false);
			FloatingDebris.types["RK Ripple Smooth Watcher"] = new RKRippleSpawner("RKSmoothRippleMask", true);

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
			FloatingDebris.types.Remove("RK Colored Dust");
			FloatingDebris.types.Remove("RK Ripple Ring");
			FloatingDebris.types.Remove("RK Ripple Ring Watcher");
			FloatingDebris.types.Remove("RK Ripple Smooth");
			FloatingDebris.types.Remove("RK Ripple Smooth Watcher");

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
		Custom.rainWorld.Shaders["RKDust"] = FShader.CreateShader("RKDust", bundle.LoadAsset<Shader>("Assets/Shaders/RKFloatingDust.shader"));
		Custom.rainWorld.Shaders["RKWhiteDust"] = FShader.CreateShader("RKWhiteDust", bundle.LoadAsset<Shader>("Assets/Shaders/RKFloatingDust.shader"), ["lightdust"]);
		Custom.rainWorld.Shaders["RKColoredDust"] = FShader.CreateShader("RKColoredDust", bundle.LoadAsset<Shader>("Assets/Shaders/RKColoredDust.shader"));
		Custom.rainWorld.Shaders["RKSmoothRippleMask"] = FShader.CreateShader("RKSmoothRippleMask", bundle.LoadAsset<Shader>("Assets/Shaders/RKSmoothRippleMask.shader"));
	}
}
