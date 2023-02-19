using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc;

internal class DevUIUtils
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

		public static string DefaultSettingsLocation(DevUI self, RoomSettings roomSettings, bool includeRoot)
		{
			//this is basically a copy-paste of the settings loading from the RoomSettings constructor
			//probably not the best way to do this, but hey, it seems to work

			SlugcatStats.Name? playerChar = self.game.GetStorySession.saveStateNumber;

			string result;
			if (roomSettings.isTemplate)
			{
				result = AssetManager.ResolveFilePath(string.Concat(new string[]
				{
				"World",
				Path.DirectorySeparatorChar.ToString(),
				self.game.world.region.name,
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

			if (includeRoot) return result;

			UPath.TryCropToSubstringLeft(result, "world", out result);

			UPath.TryCropToSubstringLeft(result, "levels", out result);

			return result;
		}

		public static bool PathToSpecificSettings(string modPackDirectory, string roomName, out string filePath, bool makeDirectory = true)
		{
			filePath = WorldLoader.FindRoomFile(roomName, false, ".txt").ToLower();

			UPath.TryCropToSubstringRight(filePath, "world", out filePath);

			UPath.TryCropToSubstringRight(filePath, "levels", out filePath);

			filePath = modPackDirectory + UPath.AppendFileName(filePath, "_settings.txt");

			if (!makeDirectory) { return true; }

			try
			{
				if (!Directory.Exists(Path.GetDirectoryName(filePath)))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				}
			}
			catch { Debug.Log("Couldn't create directory"); }

			if (Directory.Exists(Path.GetDirectoryName(filePath)))
			{
				return true;
			}

			return false;

		}

	}
}
