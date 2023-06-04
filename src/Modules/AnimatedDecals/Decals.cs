using System.IO;
using UnityEngine.Video;

namespace RegionKit.Modules.AnimatedDecals
{
	/// <summary>
	/// Allow animated textures as decals.
	/// </summary>
	public static class Decals
	{
		public static void Enable()
		{
			On.DevInterface.CustomDecalRepresentation.ctor += CustomDecalRepresentation_ctor;
			On.CustomDecal.LoadFile += CustomDecal_LoadFile;
		}

		public static void Disable()
		{
			On.DevInterface.CustomDecalRepresentation.ctor -= CustomDecalRepresentation_ctor;
			On.CustomDecal.LoadFile -= CustomDecal_LoadFile;
		}

		private static void CustomDecalRepresentation_ctor(On.DevInterface.CustomDecalRepresentation.orig_ctor orig, DevInterface.CustomDecalRepresentation self, DevInterface.DevUI owner, string IDstring, DevInterface.DevUINode parentNode, PlacedObject pObj, string name)
		{
			orig(self, owner, IDstring, parentNode, pObj, name);

			string[] files = AssetManager.ListDirectory("decals");
			var videos = new List<string>();
			for (int i = 0; i < files.Length; i++)
			{
				if (VideoManager.IsVideoFile(files[i]))
				{
					videos.Add(Path.GetFileName(files[i]));
				}
			}

			if (videos.Count > 0)
			{
				Array.Resize(ref self.decalFiles, self.decalFiles.Length + videos.Count);
				videos.CopyTo(self.decalFiles, self.decalFiles.Length - videos.Count);
			}
		}

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
