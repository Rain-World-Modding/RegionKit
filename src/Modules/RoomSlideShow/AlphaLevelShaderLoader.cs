using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Modules.RoomSlideShow
{
	internal class AlphaLevelShaderLoader
	{
		// Shader is made by Henpemaz, Loader is by Cactus
		// Allows slideshows to have a transparent background for all parts of the image that are completely transparent
		public static bool loaded = false;
		public static void AlphaLevelLoad(RainWorld rw)
		{
			if (!loaded)
			{
				loaded = true;
				var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/alphalevelcolorbundle"));
				rw.Shaders["AlphaLevelColor"] = FShader.CreateShader("AlphaLevelColor", bundle.LoadAsset<Shader>("Assets/shaders 1.9.03/RM_LeveIltem_3.shader"));
				rw.Shaders["WaterWarble"] = FShader.CreateShader("WaterWarble", bundle.LoadAsset<Shader>("Assets/shaders 1.9.03/WaterWarble.shader"));

			}
		}
	}
}
