using Music;
using System.IO;

namespace RegionKit.Modules
{
	[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "CustomSSSong")]
	// MADE BY @.k0r1 / Korii1
	public class CustomSSSong
	{
		private static readonly Dictionary<string, string> SongMap = new();
		public static void LoadCustomSongs()
		{
			var modDirectories = new HashSet<string>();

			// Mods folder
			string modsDirectoryPath = Path.Combine(Application.streamingAssetsPath, "mods");
			if (Directory.Exists(modsDirectoryPath))
			{
				foreach (string dir in Directory.GetDirectories(modsDirectoryPath))
					modDirectories.Add(dir);
			}

			// Workshop folder
			string steamWorkshopPath = Path.Combine(Path.GetFullPath("."), @"..\..\workshop\content\312520");
			if (Directory.Exists(steamWorkshopPath))
			{
				foreach (string dir in Directory.GetDirectories(steamWorkshopPath))
					modDirectories.Add(dir);
			}

			////////////////////////////////////////////////////////////////////////////////////////////////////
			
			foreach (string modDirectory in modDirectories)
			{
				string customSSMusicFilePath = Path.Combine(modDirectory, "customssmusic.txt");

				if (File.Exists(customSSMusicFilePath))
				{
					foreach (string line in File.ReadAllLines(customSSMusicFilePath))
					{
						string[] parts = line.Split(new[] { " : " }, StringSplitOptions.RemoveEmptyEntries);
						if (parts.Length == 2)
						{
							SongMap.TryAdd(parts[0].Trim(), parts[1].Trim());
						}
					}
				}
			}

			Log($"Custom SS Music Loaded:\n{string.Join("\n", SongMap.Select(entry => $"{entry.Key} : {entry.Value}"))}");
		}

		public static void SSSong(On.Music.MusicPlayer.orig_RequestSSSong orig, MusicPlayer self)
		{
			ProcessManager manager = self.manager;
			if (manager?.rainWorld.setup.playMusic != true)
			{
				orig(self);
				return;
			}

			if (manager.currentMainLoop is RainWorldGame rainWorldGame && rainWorldGame.IsStorySession && SongMap.TryGetValue(rainWorldGame.world.name, out string songToPlay))
			{
				if (self.song is not SSSong sss || sss.name != songToPlay)
				{
					Log($"Playing custom SSSong for {rainWorldGame.world.name}: {songToPlay}");
					self.song = new SSSong(self, songToPlay) { playWhenReady = true };
					return;
				}
			}

			orig(self);
		}

		public static void Enable()
		{
			LoadCustomSongs();
			On.Music.MusicPlayer.RequestSSSong += SSSong;
		}

		public static void Disable()
		{
			On.Music.MusicPlayer.RequestSSSong -= SSSong;
		}
	}
}
