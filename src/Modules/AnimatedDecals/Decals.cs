using System.IO;

namespace RegionKit.Modules.AnimatedDecals
{
	/// <summary>
	/// Allow animated textures as decals.
	/// </summary>
	public static class Decals
	{
		public static void Enable()
		{
			On.CustomDecal.LoadFile += CustomDecal_LoadFile;
		}

		public static void Disable()
		{
			On.CustomDecal.LoadFile -= CustomDecal_LoadFile;
		}

		// Video file adding hook was moved to DevUIMisc.ListFixes

		private static void CustomDecal_LoadFile(On.CustomDecal.orig_LoadFile orig, CustomDecal self, string fileName)
		{
			if (VideoManager.IsVideoFile(fileName))
			{
				string path = AssetManager.ResolveFilePath("Decals" + Path.DirectorySeparatorChar + fileName);
				VideoManager.LoadAndCacheVideo(fileName, path);
			}
			else
			{
				orig(self, fileName);
			}
		}
	}
}
