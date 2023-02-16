using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;
using System.Text.RegularExpressions;
using UnityEngine;
using System.IO;

namespace RegionKit.Modules.DevUIMisc
{
	internal class SettingsPathDisplay
	{
		internal static void Apply()
		{
			On.DevInterface.Page.ctor += Page_ctor;
			On.DevInterface.Page.Refresh += Page_Refresh;

			On.DevInterface.ObjectsPage.Signal += ObjectsPage_Signal;
			On.DevInterface.RoomSettingsPage.Signal += RoomSettingsPage_Signal;
			On.DevInterface.SoundPage.Signal += SoundPage_Signal;
			On.DevInterface.TriggersPage.Signal += TriggersPage_Signal;
		}

		internal static void Undo()
		{
			On.DevInterface.Page.ctor -= Page_ctor;
			On.DevInterface.Page.Refresh -= Page_Refresh;

			On.DevInterface.ObjectsPage.Signal -= ObjectsPage_Signal;
			On.DevInterface.RoomSettingsPage.Signal -= RoomSettingsPage_Signal;
			On.DevInterface.SoundPage.Signal -= SoundPage_Signal;
			On.DevInterface.TriggersPage.Signal -= TriggersPage_Signal;
		}

		#region Signal
		private static void Page_Refresh(On.DevInterface.Page.orig_Refresh orig, Page self)
		{
			modSelectPanel = null;
			orig(self);
		}

		private static void TriggersPage_Signal(On.DevInterface.TriggersPage.orig_Signal orig, TriggersPage self, DevUISignalType type, DevUINode sender, string message)
		{
			orig(self, type, sender, message);
			SaveSignal(self, type, sender, message);
		}

		private static void SoundPage_Signal(On.DevInterface.SoundPage.orig_Signal orig, SoundPage self, DevUISignalType type, DevUINode sender, string message)
		{
			orig(self, type, sender, message);
			SaveSignal(self, type, sender, message);
		}

		private static void RoomSettingsPage_Signal(On.DevInterface.RoomSettingsPage.orig_Signal orig, RoomSettingsPage self, DevUISignalType type, DevUINode sender, string message)
		{
			orig(self, type, sender, message);
			SaveSignal(self, type, sender, message);
		}

		private static void ObjectsPage_Signal(On.DevInterface.ObjectsPage.orig_Signal orig, ObjectsPage self, DevUISignalType type, DevUINode sender, string message)
		{
			orig(self, type, sender, message);
			SaveSignal(self, type, sender, message);
		}

		#endregion Signal

		private static void Page_ctor(On.DevInterface.Page.orig_ctor orig, Page self, DevUI owner, string IDstring, DevUINode parentNode, string name)
		{
			orig(self, owner, IDstring, parentNode, name);

			//move pages over to avoid collision with save buttons
			foreach (DevUINode node in self.subNodes)
			{
				if (node is SwitchPageButton switchPageButton)
				{ 
					switchPageButton.pos.x -= 20f;
					switchPageButton.Refresh();
				}
			}

			modNamesD = new Dictionary<string, string>() {
				{"Default", DefaultSettingsLocation(owner, self.RoomSettings, false)},
				{"vanilla", Custom.RootFolderDirectory()},
				{"mergedmods",Path.Combine(Custom.RootFolderDirectory(), "mergedmods") }
			};

			foreach (ModManager.Mod mod in ModManager.ActiveMods)
			{
				modNamesD.Add(mod.name, mod.path);
			}

			SettingsPathLabel = null;

			RefreshPathLabel(self);

			//SavedPath = new DevUILabel(owner, "Saved_Path", null, new Vector2(900f, 700f), 100f, "Default");

			//self.subNodes.Add(SavedPath);

			ChangePath = new Button(owner, "Change_Path", self, new Vector2(790f, 700f), 100f, "Default");

			self.subNodes.Add(ChangePath);


			CreateModify = new Button(owner, "Create_Modify", self, new Vector2(900f, 700f), 100f, "Create Modify");

			self.subNodes.Add(CreateModify);
		}

		public static void SaveSignal(Page self, DevUISignalType type, DevUINode sender, string message)
		{
			if (sender.IDstring == "Change_Path")
			{
				//remove panel with no changes when button clicked
				if (modSelectPanel != null)
				{
					self.subNodes.Remove(modSelectPanel);
					modSelectPanel.ClearSprites();
					modSelectPanel = null;
					return;
				}

				//create new panel when button clicked
				modSelectPanel = new ModSelectPanel(self.owner, self, new Vector2(420f, 250f), modNamesD.Keys.ToArray());
				self.subNodes.Add(modSelectPanel);
				return;
			}

			else if (sender.IDstring.StartsWith("ModPanelButton99289_"))
			{
				string subbuttonid = sender.IDstring.Remove(0, "ModPanelButton99289_".Length);

				if (SavedPath != null)
				{ SavedPath.Text = subbuttonid; }

				if (ChangePath != null)
				{ ChangePath.Text = subbuttonid; }


				if (PathToSpecificSettings(modNamesD[subbuttonid],self.RoomSettings.name, out string filePath))
				{
					self.RoomSettings.filePath = filePath;
					Debug.Log($"new filepath is [{filePath}]");
					RefreshPathLabel(self);
				}

				if (modSelectPanel != null)
				{
					self.subNodes.Remove(modSelectPanel);
					modSelectPanel.ClearSprites();
					modSelectPanel = null;
				}
			}

			else if (sender.IDstring == "Create_Modify" && ChangePath != null)
			{
				Debug.Log("Modify");
				ModifySettingsGenerator.Main(self.owner, ChangePath.Text);
			}
		}

		public static string DefaultSettingsLocation(DevUI self, RoomSettings roomSettings, bool includeRoot)
		{
			string result = "";

			SlugcatStats.Name? playerChar = self.game.GetStorySession.saveStateNumber;

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

			if (result.Contains($"{Path.DirectorySeparatorChar}world{Path.DirectorySeparatorChar}"))
			{ result = result.Substring(0, result.LastIndexOf($"{Path.DirectorySeparatorChar}world{Path.DirectorySeparatorChar}")); }

			if (result.Contains($"{Path.DirectorySeparatorChar}levels{Path.DirectorySeparatorChar}"))
			{ result = result.Substring(0, result.LastIndexOf($"{Path.DirectorySeparatorChar}levels{Path.DirectorySeparatorChar}")); }


			return result;
		}
		public static bool PathToSpecificSettings(string modPackDirectory, string roomName, out string filePath)
		{
			filePath = WorldLoader.FindRoomFile(roomName, false, ".txt").ToLower();

			Debug.Log($"path is {modPackDirectory}");


			if (filePath.Contains($"{Path.DirectorySeparatorChar}world{Path.DirectorySeparatorChar}"))
			{ filePath = filePath.Substring(filePath.LastIndexOf($"{Path.DirectorySeparatorChar}world{Path.DirectorySeparatorChar}") + 1); }

			if (filePath.Contains($"{Path.DirectorySeparatorChar}levels{Path.DirectorySeparatorChar}"))
			{ filePath = filePath.Substring(filePath.LastIndexOf($"{Path.DirectorySeparatorChar}levels{Path.DirectorySeparatorChar}") + 1); }

			Debug.Log($"path 2 is {filePath}");

			filePath = Path.Combine(modPackDirectory, Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))) + "_settings.txt";

			Debug.Log($"path 3 is {filePath}");

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


		public static void RefreshPathLabel(Page self)
		{
			if (SettingsPathLabel != null && self.subNodes.Contains(SettingsPathLabel))
			{ 
				self.subNodes.Remove(SettingsPathLabel);
				SettingsPathLabel.ClearSprites();
				SettingsPathLabel = null;
			}

			string settingsPath = "Settings Path: " + self.RoomSettings.filePath.ToLower();

			if (settingsPath.ToLower().Contains("workshop"))
			{ settingsPath = settingsPath.Substring(settingsPath.IndexOf("workshop")); }

			if (settingsPath.ToLower().Contains("streamingassets"))
			{ settingsPath = settingsPath.Substring(settingsPath.IndexOf("streamingassets")); }

			SettingsPathLabel = new DevUILabel(self.owner, "Settings_Path", null, new Vector2(1330f - 6f * settingsPath.Length, 20f), 20f + 6f * settingsPath.Length, settingsPath);

			self.subNodes.Add(SettingsPathLabel);

		}

		public static ModSelectPanel? modSelectPanel;

		public static DevUILabel? SettingsPathLabel = null;

		public static DevUILabel? SavedPath;

		public static Button? ChangePath;

		public static Button? CreateModify;

		public static Dictionary<string, string> modNamesD;


	}
}
