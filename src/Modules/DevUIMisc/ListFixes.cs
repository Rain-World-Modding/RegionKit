using System.IO;
using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ModPathPair = (string modName, string basePath);

namespace RegionKit.Modules.DevUIMisc;

internal static class ListFixes
{
	private static Dictionary<string, string> decalSources = new Dictionary<string, string>();
	private static List<string> decalOrdering = [];

	public static string GetDecalSource(string decal) => decalSources.TryGetValue(decal.ToLowerInvariant().Replace('/', Path.DirectorySeparatorChar), out string source) ? source : "UNKNOWN";

	public static void Apply()
	{
		IL.DevInterface.SoundPage.ctor += SoundPage_ctor;
		On.DevInterface.TriggersPage.ctor += TriggersPage_ctor;
		On.DevInterface.ObjectsPage.ctor += ObjectsPage_ctor;
		IL.DevInterface.CustomDecalRepresentation.ctor += CustomDecalRepresentation_ctor;
	}

	public static void Undo()
	{
		IL.DevInterface.SoundPage.ctor -= SoundPage_ctor;
		On.DevInterface.TriggersPage.ctor -= TriggersPage_ctor;
		On.DevInterface.ObjectsPage.ctor -= ObjectsPage_ctor;
		IL.DevInterface.CustomDecalRepresentation.ctor -= CustomDecalRepresentation_ctor;
	}

	#region Decals


	private static void CustomDecalRepresentation_ctor(ILContext il)
	{
		// Goal: save a shit ton of file reads and just read from the static list.
		// This is probably really bad for mod compatibility if they happen to use IL hooks in this range since it breaks over the whole thing
		// BUT it does save a lot of syscalls

		ILCursor c = new ILCursor(il);
		ILLabel l = c.DefineLabel();

		c.GotoNext(MoveType.AfterLabel, x => x.MatchLdstr("decals"));
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate((CustomDecalRepresentation self) =>
		{
			self.decalFiles = [.. decalOrdering];
		});
		c.Emit(OpCodes.Br, l);

		c.GotoNext(MoveType.After, x => x.MatchStfld<CustomDecalRepresentation>(nameof(CustomDecalRepresentation.decalFiles)));
		c.MarkLabel(l);
	}

	private static void ObjectsPage_ctor(On.DevInterface.ObjectsPage.orig_ctor orig, ObjectsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
	{
		orig(self, owner, IDstring, parentNode, name);

		decalSources.Clear();
		decalOrdering.Clear();

		List<ModPathPair> modsToFolders = [];
		for (int i = ModManager.ActiveMods.Count - 1; i >= 0; i--)
		{
			string modName = ModManager.ActiveMods[i].name;
			if (ModManager.ActiveMods[i].hasTargetedVersionFolder)
			{
				modsToFolders.Add((modName, ModManager.ActiveMods[i].TargetedPath));
			}
			if (ModManager.ActiveMods[i].hasNewestFolder)
			{
				modsToFolders.Add((modName, ModManager.ActiveMods[i].NewestPath));
			}
			modsToFolders.Add((modName, ModManager.ActiveMods[i].path));
		}
		modsToFolders.Add(("Vanilla", Custom.RootFolderDirectory()));

		foreach ((string modName, string basePath) in modsToFolders)
		{
			string decalFolder = Path.Combine(basePath, "decals");
			if (Directory.Exists(decalFolder))
			{
				RecursivelyAddDecals(decalFolder, "", "", modName);
			}
		}

		// Sort by load order and then alphabetically
		decalOrdering = [.. decalOrdering.Distinct(StringComparer.OrdinalIgnoreCase)]; // filter out duplicates
		List<string> modLoadOrder = [.. ModManager.ActiveMods.Select(mod => mod.name).Reverse(), "Vanilla"]; // we need to manually add Vanilla to use with DecalPreview
		Dictionary<string, List<string>> buckets = [];
		foreach (string decal in decalOrdering)
		{
			string modSource = GetDecalSource(decal);
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

		decalOrdering.Clear();
		foreach (string mod in modLoadOrder)
		{
			if (buckets.TryGetValue(mod, out List<string> bucket))
			{
				decalOrdering.AddRange(bucket.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase));
			}
		}
		foreach (var key in buckets.Keys.Except(modLoadOrder))
		{
			decalOrdering.AddRange(buckets[key].OrderBy(static x => x, StringComparer.OrdinalIgnoreCase));
		}
	}

	private static void RecursivelyAddDecals(string directory, string sourcePath, string namePath, string modName)
	{
		sourcePath = sourcePath.ToLowerInvariant();
		foreach (var subdir in Directory.EnumerateDirectories(directory))
		{
			RecursivelyAddDecals(subdir, sourcePath + Path.GetFileName(subdir) + Path.DirectorySeparatorChar.ToString(), namePath + Path.GetFileName(subdir) + "/", modName);
		}
		foreach (var file in Directory.EnumerateFiles(directory))
		{
			if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
			{
				decalSources[(sourcePath + Path.GetFileNameWithoutExtension(file)).ToLowerInvariant()] = modName;
				decalOrdering.Add(namePath + Path.GetFileNameWithoutExtension(file));
			}
			else if (AnimatedDecals.VideoManager.IsVideoFile(file))
			{
				decalSources[(sourcePath + Path.GetFileName(file)).ToLowerInvariant()] = modName;
				decalOrdering.Add(namePath + Path.GetFileName(file));
			}
		}
	}

	#endregion // Decals

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
