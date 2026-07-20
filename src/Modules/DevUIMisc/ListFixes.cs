using System.IO;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.DevUIMisc;

internal static class ListFixes
{
	public static void Apply()
	{
		IL.DevInterface.SoundPage.ctor += SoundPage_ctor;
		On.DevInterface.TriggersPage.ctor += TriggersPage_ctor;
	}
	public static void Undo()
	{
		IL.DevInterface.SoundPage.ctor -= SoundPage_ctor;
		On.DevInterface.TriggersPage.ctor -= TriggersPage_ctor;
	}
	private static void TriggersPage_ctor(On.DevInterface.TriggersPage.orig_ctor orig, DevInterface.TriggersPage self, DevInterface.DevUI owner, string IDstring, DevInterface.DevUINode parentNode, string name)
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
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After, x => x.MatchLdstr("soundeffects/ambient")))
		{
			c.Emit(OpCodes.Pop);
			c.Emit(OpCodes.Ldstr, "loadedsoundeffects/ambient");
		}
	}
}
