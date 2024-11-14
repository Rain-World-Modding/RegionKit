using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Modules.Effects
{
	internal class LegacyColoredSprite2
	{
		// Shader for assets that rely on the 1.5 ColoredSprite2
		public static bool loaded = false;
		public static void LegacyColoredSprite2Load(RainWorld rw)
		{
			if (!loaded)
			{
				loaded = true;
				var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/legacycoloredspritebundle"));
				rw.Shaders["LegacyColoredSprite2"] = FShader.CreateShader("LegacyColoredSprite2", bundle.LoadAsset<Shader>("Assets/LegacyColoredSprite2.shader"));
			}
		}
	}
}
