using System.Text.RegularExpressions;
using System.IO;
using DevInterface;
using static RegionKit.Modules.DevUIMisc.DevUIUtils;

namespace RegionKit.Modules.DevUIMisc;

internal class ModifySettingsGenerator
{
	public ModifySettingsGenerator(DevUI self, string modname)
	{
		if (SettingsSaveOptions.settingsSaveOptionsMenu is null) { return; }

		LogMessage("\n----Creating modify file----");
		string filePath = URoomSettings.DefaultSettingsLocation(self, self.room.roomSettings, true);

		if (File.Exists(filePath))
		{ originalS = new RoomSettingsStruct(File.ReadAllLines(filePath)); }
		else { LogMessage("Failed to find original settings, aborting"); return; }

		newS = new RoomSettingsStruct(SaveTemp(self));

		Compare();

		if (URoomSettings.PathToSpecificSettings(SettingsSaveOptions.settingsSaveOptionsMenu.modNames[modname], self.room.roomSettings.name, out filePath, false, URoomSettings.UsingSpecificSlugcatName(self)))
		{
			filePath = filePath.ToLower();

			bool modifyDirectory = false;

			if (UPath.TryInsertDirectory(filePath, "world", "modify", out filePath))
			{ modifyDirectory = true; }

			if (UPath.TryInsertDirectory(filePath, "levels", "modify", out filePath))
			{ modifyDirectory = true; }

			if (!modifyDirectory)
			{ LogMessage($"Failed to find modify directory, aborting\n{filePath}"); return; }
		}
		else { LogMessage("can't find new settings, aborting"); return; }

		try
		{
			if (!Directory.Exists(Path.GetDirectoryName(filePath)))
			{ Directory.CreateDirectory(Path.GetDirectoryName(filePath)); }
		}
		catch (Exception ex) { LogMessage($"Failed to create directory, aborting\n{filePath}\n{ex}"); return; }

		File.WriteAllText(filePath, mergeS.ToString());
	}

	string[] SaveTemp(DevUI self)
	{
		mergeS = new MergeSettingsStruct(true);
		try
		{
			string tempFilePath = UPath.AppendFileName(URoomSettings.DefaultSettingsLocation(self, self.room.roomSettings, true), "");

			int i = 1;
			while (File.Exists(tempFilePath + "_temp" + i + ".txt"))
			{ i++; }

			tempFilePath = tempFilePath + "_temp" + i + ".txt";

			self.room.roomSettings.Save(tempFilePath, false);

			string[] result = File.ReadAllLines(tempFilePath);

			File.Delete(tempFilePath);

			return result;
		}
		catch (Exception ex) { LogMessage(ex); return new string[0]; }
	}

	public void Compare()
	{
		foreach (KeyValuePair<string, string> settingsLine in newS.settingsLines)
		{
			if (!originalS.settingsLines.ContainsKey(settingsLine.Key) || originalS.settingsLines[settingsLine.Key] != settingsLine.Value)
			{ mergeS.mergeLine.Add(KeyValuePairToString(settingsLine)); }
		}

		foreach (KeyValuePair<string, string> settingsLine in originalS.settingsLines)
		{
			if (!newS.settingsLines.ContainsKey(settingsLine.Key))
			{ mergeS.removeLine.Add(KeyValuePairToString(settingsLine)); }
		}


		foreach (KeyValuePair<string, List<string>> keyValuePair in newS.listedLines)
		{
			foreach (string str in keyValuePair.Value)
			{
				if (!originalS.listedLines[keyValuePair.Key].Contains(str))
				{ mergeS.listMerge[keyValuePair.Key].Add(str); }
			}
		}

		foreach (KeyValuePair<string, List<string>> keyValuePair in originalS.listedLines)
		{
			foreach (string str in keyValuePair.Value)
			{
				if (!newS.listedLines[keyValuePair.Key].Contains(str))
				{ mergeS.removeLine.Add(str + ", "); }
			}
		}
	}


	public RoomSettingsStruct originalS;

	public RoomSettingsStruct newS;

	public MergeSettingsStruct mergeS;

	public string KeyValuePairToString(KeyValuePair<string, string> keyValuePair)
	{ return keyValuePair.Key + ": " + keyValuePair.Value; }

	public struct MergeSettingsStruct
	{
		public List<string> mergeLine;
		public Dictionary<string, string> findReplaceLine;
		public List<string> removeLine;
		public Dictionary<string, List<string>> listMerge;

		public MergeSettingsStruct(bool eeuh)
		{
			this.mergeLine = new List<string>();
			this.findReplaceLine = new Dictionary<string, string>();
			this.removeLine = new List<string>();

			this.listMerge = new Dictionary<string, List<string>>() {
					{"Effects",new List<string>() },
					{"PlacedObjects",new List<string>() },
					{"AmbientSounds",new List<string>() },
					{"Triggers",new List<string>() }
				};
		}

		public override string ToString()
		{
			List<string> list = new List<string>();

			foreach (string str in removeLine)
			{
				list.Add($"[FIND]{str}\n[REPLACE]");
			}

			foreach (KeyValuePair<string, string> keyValuePair in findReplaceLine)
			{
				list.Add($"[FIND]{keyValuePair.Key}\n[REPLACE]{keyValuePair.Value}");
			}

			//only merge if there's stuff to merge
			bool merge = mergeLine.Count > 0;

			if (!merge)
			{
				foreach (KeyValuePair<string, List<string>> keyValuePair in listMerge)
				{ if (keyValuePair.Value.Count > 0) { merge = true; break; } }
			}

			if (merge)
			{
				list.Add("[MERGE]");
				foreach (string str in mergeLine)
				{
					list.Add(str);
				}

				foreach (KeyValuePair<string, List<string>> keyValuePair in listMerge)
				{
					if (keyValuePair.Value.Count > 0)
					{ list.Add($"{keyValuePair.Key}: {string.Join(", ", keyValuePair.Value)}, "); }

				}

				list.Add("[ENDMERGE]");
			}

			return string.Join($"\n", list);
		}
	}

	public struct RoomSettingsStruct
	{
		public Dictionary<string, string> settingsLines;

		public Dictionary<string, List<string>> listedLines;


		public RoomSettingsStruct(string[] file)
		{
			this.settingsLines = new Dictionary<string, string>();

			this.listedLines = new Dictionary<string, List<string>>() {
					{"Effects",new List<string>() },
					{"PlacedObjects",new List<string>() },
					{"AmbientSounds",new List<string>() },
					{"Triggers",new List<string>() }
				};

			foreach (string line in file)
			{
				LogMessage($"reading line {line}");
				string[] splitLine = Regex.Split(line, ": ");
				if (splitLine.Length < 2)
				{ continue; }

				switch (splitLine[0])
				{
				case "Effects":
				case "PlacedObjects":
				case "AmbientSounds":
				case "Triggers":
					listedLines[splitLine[0]] = Regex.Split(splitLine[1], ", ").ToList();
					break;

				default:
					settingsLines.Add(splitLine[0], splitLine[1]);
					break;
				}
			}
		}
	}
}
