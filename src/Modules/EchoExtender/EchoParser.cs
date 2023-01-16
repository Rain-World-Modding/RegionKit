using System;
using System.Collections.Generic;
using System.IO;
using RWCustom;
using UnityEngine;


namespace RegionKit.Modules.EchoExtender;

public static class EchoParser
{
	public static readonly Dictionary<Conversation.ID, string> EchoConversations = new Dictionary<Conversation.ID, string>();
	public static readonly HashSet<GhostWorldPresence.GhostID> ExtendedEchoIDs = new HashSet<GhostWorldPresence.GhostID>();
	public static readonly Dictionary<string, string> EchoLocations = new Dictionary<string, string>();
	public static readonly Dictionary<GhostWorldPresence.GhostID, EchoSettings> EchoSettings = new Dictionary<GhostWorldPresence.GhostID, EchoSettings>();

	public static readonly Dictionary<string, string> EchoSongs = new Dictionary<string, string> {
		{ "CC", "NA_32 - Else1" },
		{ "SI", "NA_38 - Else7" },
		{ "LF", "NA_36 - Else5" },
		{ "SH", "NA_34 - Else3" },
		{ "UW", "NA_35 - Else4" },
		{ "SB", "NA_33 - Else2" },
		{ "UNUSED", "NA_37 - Else6" }
	};

	public static GhostWorldPresence.GhostID GetEchoID(string regionShort) => (GhostWorldPresence.GhostID)ExtEnumBase.Parse(typeof(GhostWorldPresence.GhostID), regionShort, false);
	public static Conversation.ID GetConversationID(string regionShort) => (Conversation.ID)ExtEnumBase.Parse(typeof(Conversation.ID), "Ghost_" + regionShort, false);

	public static bool EchoIDExists(string regionShort)
	{
		try
		{
			GetEchoID(regionShort);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}


	// ReSharper disable once InconsistentNaming
	public static void LoadAllRegions(SlugcatStats.Name character)
	{
		foreach (var region in Region.LoadAllRegions(character))
		{
			string regInitials = region.name;
			string convPath = AssetManager.ResolveFilePath($"world/{region.name}/echoconv.txt");
			__logger.LogInfo($"[Echo Extender] Checking region {region.name} for Echo.");
			if (File.Exists(convPath))
			{
				string convText = File.ReadAllText(convPath);
				convText = ManageXOREncryption(convText, convPath);
				string settingsPath = AssetManager.ResolveFilePath($"world/{region.name}/echoSettings.txt");
				var settings = File.Exists(settingsPath) ? RegionKit.Modules.EchoExtender.EchoSettings.FromFile(settingsPath) : RegionKit.Modules.EchoExtender.EchoSettings.Default;
				if (!EchoIDExists(regInitials))
				{
					ExtendedEchoIDs.Add(new GhostWorldPresence.GhostID(regInitials, true));
					EchoConversations.Add(new Conversation.ID($"Ghost_{regInitials}", true), convText);
					__logger.LogInfo("[Echo Extender] Added conversation for echo in region " + regInitials);
				}
				else
				{
					__logger.LogWarning("[Echo Extender] An echo for this region already exists, skipping.");
				}

				EchoSettings.SetKey(GetEchoID(regInitials), settings);
			}
			else
			{
				__logger.LogInfo("[Echo Extender] No conversation file found!");
			}
		}
	}


	public static string ManageXOREncryption(string text, string path)
	{
		__logger.LogInfo("[Echo Extender] Managing XOR Encryption, only supports English so far");
		string xor = Custom.xorEncrypt(text, 54 + 1 + (int)InGameTranslator.LanguageID.English * 7);
		if (xor.StartsWith("###ENCRYPTED")) return xor.Substring("###ENCRYPTED".Length);
		File.WriteAllText(path, Custom.xorEncrypt("###ENCRYPTED" + text, 54 + 1 + (int)InGameTranslator.LanguageID.English * 7));
		return text;
	}
}
