using Watcher;

namespace RegionKit.Modules.ShaderTools
{
	// Clearing the depth buffer is necessary due to enabling a depth buffer in ShaderBuffers.cs
	internal static class ClearDepthBuffer
	{
		public static void Apply()
		{
			Custom.rainWorld.Shaders["ClearDepthBuffer"] = FShader.CreateShader("ClearDepthBuffer", AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/cleardepthbuffer")).LoadAsset<Shader>("Assets/Shaders/ClearDepthBuffer.shader"));

			On.Watcher.MaskLayer.ctor += MaskLayer_ctor;
		}

		public static void Unapply()
		{
			On.Watcher.MaskLayer.ctor -= MaskLayer_ctor;
		}

		private static void MaskLayer_ctor(On.Watcher.MaskLayer.orig_ctor orig, MaskLayer self, string shader)
		{
			orig(self, shader);
			var source = new MaskSource(self.GetMaterial("ClearDepthBuffer", 3));
			self.maskSources.Add(source);
		}
	}
}
