using System.IO;

namespace RegionKit.Modules.EchoExtender;

internal static class EchoParser
{
	// ConversationID to region acronym
	internal static readonly Dictionary<Conversation.ID, string> __echoConversations = [];
	internal static readonly HashSet<GhostWorldPresence.GhostID> __extendedEchoIDs = [];
	internal static readonly Dictionary<string, string> __echoLocations = [];
	internal static readonly Dictionary<GhostWorldPresence.GhostID, EchoSettings> __echoSettings = [];

	internal static readonly Dictionary<string, string> __echoSongs = new()
	{
		{ "CC", "NA_32 - Else1" },
		{ "SI", "NA_38 - Else7" },
		{ "LF", "NA_36 - Else5" },
		{ "SH", "NA_34 - Else3" },
		{ "UW", "NA_35 - Else4" },
		{ "SB", "NA_33 - Else2" },
		{ "UNUSED", "NA_37 - Else6" }
	};

	public static GhostWorldPresence.GhostID GetEchoID(string regionShort) => new(regionShort, false);
	public static Conversation.ID GetConversationID(string regionShort) => new($"Ghost_{regionShort}", false);

	public static bool EchoIDExists(string regionShort)
	{
		try
		{
			return GetEchoID(regionShort).Index >= 0;
			//return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static void LoadAllRegions(SlugcatStats.Name character)
	{
		foreach (var regionInitials in Region.GetFullRegionOrder())
		{
			string settingsPath = AssetManager.ResolveFilePath($"world/{regionInitials}/echoSettings.txt");
			if (File.Exists(settingsPath))
			{
				LogInfo($"[Echo Extender] Initializing echo in {regionInitials}.");
				EchoSettings settings = EchoSettings.FromFile(settingsPath, character);

				if (!EchoIDExists(regionInitials))
				{
					__extendedEchoIDs.Add(new GhostWorldPresence.GhostID(regionInitials, true));
					__echoConversations.Add(new Conversation.ID($"Ghost_{regionInitials}", true), regionInitials);
					LogInfo("[Echo Extender] Added conversation for echo in region " + regionInitials);
				}
				else
				{
					LogWarning("[Echo Extender] An echo for this region already exists, skipping.");
				}
				__echoSettings.SetKey(GetEchoID(regionInitials), settings);
			}
		}
	}

	static readonly string encryptedText = "###ENCRYPTED";
	static readonly string encryptedHeader = Custom.xorEncrypt(encryptedText, 54 + 1 + (int)InGameTranslator.LanguageID.English * 7);
	public static string? ManageXOREncryption(string path)
	{
		if (!File.Exists(path))
			return null;

		string text = File.ReadAllText(path);
		if (text.StartsWith(encryptedHeader))
		{
			string xor = Custom.xorEncrypt(text, 54 + 1 + (int)InGameTranslator.LanguageID.English * 7);
			if (xor.StartsWith(encryptedText))
			{
				xor = xor[encryptedText.Length..];
			}
			File.WriteAllText(path, xor);
			return xor;
		}
		return text;
	}
}
