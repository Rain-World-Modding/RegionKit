using System.IO;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc;

internal static class DevUIUtils
{
	public static class UPath
	{
		public static bool TryCropToSubstringLeft(string path, string directory, out string output)
		{
			path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			directory = Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar;

			if (path.Contains(directory))
			{
				output = path.Substring(0, path.LastIndexOf(directory));
				return true;
			}
			else
			{
				output = path;
				return false;
			}
		}

		public static bool TryCropToSubstringRight(string path, string directory, out string output)
		{
			path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			directory = Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar;

			if (path.Contains(directory))
			{
				output = path.Substring(path.LastIndexOf(directory));
				return true;
			}
			else
			{
				output = path;
				return false;
			}
		}

		public static bool TryInsertDirectory(string path, string directory, string newDirectoryName, out string output)
		{
			path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			directory = Path.DirectorySeparatorChar + directory + Path.DirectorySeparatorChar;

			if (path.Contains(directory))
			{
				output = path.Insert(path.LastIndexOf(directory), Path.DirectorySeparatorChar + newDirectoryName);
				return true;
			}
			else
			{
				output = path;
				return false;
			}
		}

		public static string AppendFileName(string path, string append)
		{
			return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path)) + append;
		}
	}

	public static class URoomSettings
	{

		public static string UsingSpecificSlugcatName(DevUI self)
		{
			bool usingSpecific = false;
			foreach (DevUINode node in self.activePage.subNodes)
			{
				if (node is DevUILabel label && label.Text == "Using Specific!")
				{
					usingSpecific = true;
					break;
				}
			}

			if (!usingSpecific) { return ""; }

			string? name = (self.activePage.owner.game.session as StoryGameSession)?.saveState.saveStateNumber.ToString();
			if (name == null)
			{ return ""; }

			return name;
		}

		public static string DefaultSettingsLocation(DevUI self, RoomSettings roomSettings, bool includeRoot)
		{
			//this is basically a copy-paste of the settings loading from the RoomSettings constructor
			//probably not the best way to do this, but hey, it seems to work

			SlugcatStats.Name? playerChar = self.game.GetStorySession?.saveStateNumber;

			string result;
			if (roomSettings.isTemplate && self.game.world.region?.name is string s)
			{
				result = AssetManager.ResolveFilePath(string.Concat(new string[]
				{
				"World",
				Path.DirectorySeparatorChar.ToString(),
				s,
				Path.DirectorySeparatorChar.ToString(),
				roomSettings.name,
				".txt"
				}));
			}
			else
			{
				string path = (playerChar == null) ? "" : WorldLoader.FindRoomFile(roomSettings.name, false, "_settings-" + playerChar.value + ".txt");
				result = path;
				if (!File.Exists(path))
				{
					if (ModManager.MSC && roomSettings.name.EndsWith("-2"))
					{
						path = WorldLoader.FindRoomFile(roomSettings.name.Substring(0, roomSettings.name.Length - 2), false, "-2_settings.txt");
					}
					else
					{
						path = WorldLoader.FindRoomFile(roomSettings.name, false, "_settings.txt");
					}
					if (File.Exists(path))
					{
						result = path;
					}
					else if (roomSettings.name.EndsWith("-2"))
					{
						result = WorldLoader.FindRoomFile(roomSettings.name.Substring(0, roomSettings.name.Length - 2), false, "_settings.txt");
					}
					else
					{
						result = path;
					}
				}
			}

			if (!File.Exists(result))
			{
				string text = WorldLoader.FindRoomFile(roomSettings.name, false, ".txt");
				if (File.Exists(text))
				{
					result = text.Substring(0, text.Length - 4) + "_settings.txt";
				}
			}

			if (!File.Exists(result))
			{ return ""; }

			if (includeRoot) return result;

			UPath.TryCropToSubstringLeft(result, "world", out result);

			UPath.TryCropToSubstringLeft(result, "levels", out result);

			return result;
		}

		public static bool PathToSpecificSettings(string modPackDirectory, string roomName, out string filePath, bool makeDirectory = true, string slugName = "")
		{
			filePath = WorldLoader.FindRoomFile(roomName, false, ".txt").ToLower();

			UPath.TryCropToSubstringRight(filePath, "world", out filePath);

			UPath.TryCropToSubstringRight(filePath, "levels", out filePath);

			if (slugName != "")
			{ slugName = "-" + slugName; }

			filePath = modPackDirectory + UPath.AppendFileName(filePath, "_settings" + slugName +".txt");

			if (!makeDirectory) { return true; }

			try
			{
				if (!Directory.Exists(Path.GetDirectoryName(filePath)))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				}
			}
			catch { LogMessage("Couldn't create directory"); }

			if (Directory.Exists(Path.GetDirectoryName(filePath)))
			{
				return true;
			}

			return false;

		}

	}

	public static void SendSignal(this DevUINode devUINode, DevUISignalType signalType, DevUINode sender, string message)
	{
		while (devUINode != null)
		{
			devUINode = devUINode.parentNode;
			if (devUINode is IDevUISignals signals)
			{
				signals.Signal(signalType, sender, message);
				break;
			}
		}
	}
}
