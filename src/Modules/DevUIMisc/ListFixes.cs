using System.IO;
using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.DevUIMisc;

internal static class ListFixes
{
	public static void Apply()
	{
		IL.DevInterface.SoundPage.ctor += SoundPage_ctor;
		On.DevInterface.TriggersPage.ctor += TriggersPage_ctor;
		On.DevInterface.CustomDecalRepresentation.ctor += CustomDecalRepresentation_ctor;
	}

	public static void Undo()
	{
		IL.DevInterface.SoundPage.ctor -= SoundPage_ctor;
		On.DevInterface.TriggersPage.ctor -= TriggersPage_ctor;
		On.DevInterface.CustomDecalRepresentation.ctor -= CustomDecalRepresentation_ctor;
	}

	private static void CustomDecalRepresentation_ctor(On.DevInterface.CustomDecalRepresentation.orig_ctor orig, CustomDecalRepresentation self, DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name)
	{
		orig(self, owner, IDstring, parentNode, pObj, name);

		List<string> decalsList = [];

		// Video files
		string[] files = AssetManager.ListDirectory("decals");
		for (int i = 0; i < files.Length; i++)
		{
			if (AnimatedDecals.VideoManager.IsVideoFile(files[i]))
			{
				decalsList.Add(Path.GetFileName(files[i]));
			}
		}

		// Decal subfolders
		List<string> foldersToSearch = [];
		for (int i = ModManager.ActiveMods.Count - 1; i >= 0; i--)
		{
			if (ModManager.ActiveMods[i].hasTargetedVersionFolder)
			{
				foldersToSearch.Add(ModManager.ActiveMods[i].TargetedPath);
			}
			if (ModManager.ActiveMods[i].hasNewestFolder)
			{
				foldersToSearch.Add(ModManager.ActiveMods[i].NewestPath);
			}
			foldersToSearch.Add(ModManager.ActiveMods[i].path);
		}

		foreach (string folder in foldersToSearch)
		{
			string decalFolder = Path.Combine(folder, "decals");
			if (Directory.Exists(decalFolder))
			{
				foreach (var subdir in Directory.EnumerateDirectories(decalFolder))
				{
					RecursivelyAddDecals(subdir, Path.GetFileName(subdir) + "/", decalsList);
				}
			}
		}

		// Sort by load order
		decalsList = decalsList.Concat(self.decalFiles).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); // filter out duplicates
		List<string> modLoadOrder = ModManager.ActiveMods.Select(mod => mod.name).Reverse().Append("Vanilla").ToList(); // we need to manually add Vanilla to use with DecalPreview
		Dictionary<string, List<string>> buckets = [];
		foreach (string decal in decalsList)
		{
			string modSource = DecalPreview.GetDecalSource(decal);
			if (!modLoadOrder.Contains(modSource))
			{
				modLoadOrder.Add(modSource); // this should only happen if the mod source is "UNKNOWN"
			}

			if (!buckets.TryGetValue(modSource, out List<string> bucket))
			{
				buckets.Add(modSource, bucket = new List<string>());
			}
			bucket.Add(decal);
		}

		decalsList.Clear();
		foreach (string mod in modLoadOrder)
		{
			if (buckets.TryGetValue(mod, out List<string> bucket))
			{
				decalsList.AddRange(bucket.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase));
			}
		}
		foreach (var key in buckets.Keys.Except(modLoadOrder))
		{
			decalsList.AddRange(buckets[key].OrderBy(static x => x, StringComparer.OrdinalIgnoreCase));
		}

		// Set decal files
		self.decalFiles = decalsList.ToArray();

		// Helper
		static void RecursivelyAddDecals(string directory, string basePath, List<string> decalsList)
		{
			foreach (var subdir in Directory.EnumerateDirectories(directory))
			{
				RecursivelyAddDecals(subdir, basePath + Path.GetFileName(subdir) + "/", decalsList);
			}
			foreach (var file in Directory.EnumerateFiles(directory))
			{
				if (file.EndsWith(".png"))
				{
					decalsList.Add(basePath + Path.GetFileNameWithoutExtension(file));
				}
				else if (AnimatedDecals.VideoManager.IsVideoFile(file))
				{
					decalsList.Add(basePath + Path.GetFileName(file));
				}
			}
		}
	}

	private static void TriggersPage_ctor(On.DevInterface.TriggersPage.orig_ctor orig, TriggersPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
	{
		orig(self, owner, IDstring, parentNode, name);

		//List<string> songs = self.songNames.ToList(); //nah, ListDirectory does this automatically
		List<string> songs = new();

		string[] files = AssetManager.ListDirectory("Music" + Path.DirectorySeparatorChar.ToString() + "Songs");
		foreach (string file in files)
		{
			string noExtension = Path.GetFileNameWithoutExtension(file);
			if (!songs.Contains(noExtension) && Path.GetExtension(file).ToLower() != ".meta")
			{ 
				songs.Add(noExtension); 
			}
		}

		self.songNames = songs.ToArray();
	}

	private static void SoundPage_ctor(ILContext il)
	{
		// Fix for sound page not reading from the correct folder
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After, x => x.MatchLdstr("soundeffects/ambient")))
		{
			c.Emit(OpCodes.Pop);
			c.Emit(OpCodes.Ldstr, "loadedsoundeffects/ambient");
		}
	}
}
