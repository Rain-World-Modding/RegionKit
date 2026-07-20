namespace RegionKit.Modules.ShaderTools
{
	internal static class ShaderPatch
	{
		private static AssetBundle? bundle;

		internal static void Apply()
		{
			LoadShaders();
		}

		internal static void Undo()
		{
		}

		private static void LoadShaders()
		{
			bundle?.Unload(true);
			bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/rkterrainfix"));
			Custom.rainWorld.Shaders["SlopedTerrainSurface"].shader = bundle.LoadAsset<Shader>("Assets/Shaders/SlopedTerrainSurfaceFix.shader");
		}
	}
}
