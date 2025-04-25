using System.IO;
using System.Text.RegularExpressions;
using HUD;

namespace RegionKit.Modules.EchoExtender
{
	public class EEGhostConversation : GhostConversation
	{
		public EEGhostConversation(ID id, Ghost ghost, DialogBox dialogBox) : base(id, ghost, dialogBox)
		{
		}

		public override void AddEvents()
		{
			bool foundConvo = EchoParser.__echoConversations.TryGetValue(id, out string region);
			if (!foundConvo)
			{
				LogDebug("[Echo Extender] Could not find echo conversation for region!");
				events.Add(new TextEvent(this, 0, "ECHO EXTENDER ERROR: could not find echo conversation for region!", 0));
				return;
			}

			InGameTranslator.LanguageID lang = Custom.rainWorld.inGameTranslator.currentLanguage;
			string? text = ResolveEchoConversation(lang, region!);
			LogDebug("[Echo Extender] Printing echo conversation");
			LogDebug(text);
			if (text is null && lang != InGameTranslator.LanguageID.English)
			{
				text = ResolveEchoConversation(InGameTranslator.LanguageID.English, region!);
			}
			if (text is null)
			{
				LogDebug("[Echo Extender] Could not resolve echo conversation from file!");
				events.Add(new TextEvent(this, 0, "ECHO EXTENDER ERROR: Could not resolve echo conversation from file!", 0));
				return;
			}

			foreach (string line in ProcessTimelineConditions(Regex.Split(text, "(\r|\n)+"), ghost.room.game.TimelinePoint))
			{
				LogDebug($"[Echo Extender] Processing line {line}");
				if (line.All(c => char.IsSeparator(c) || c == '\n' || c == '\r')) continue;
				events.Add(new TextEvent(this, 0, line, 0));
			}
		}

		protected string ResolveEchoConversation(InGameTranslator.LanguageID lang, string region)
		{
			string langShort = LocalizationTranslator.LangShort(lang);
			string convPath = AssetManager.ResolveFilePath($"text/text_{langShort}/echoConv_{region}.txt");
			bool english = lang == InGameTranslator.LanguageID.English;
			if (!File.Exists(convPath))
			{
				if (!english)
				{
					return null!;
				}
				convPath = AssetManager.ResolveFilePath($"world/{region}/echoConv.txt");
				if (!File.Exists(convPath))
				{
					return null!;
				}
			}

			if (english)
			{
				return EchoParser.ManageXOREncryption(convPath)!;
			}
			else
			{
				return File.ReadAllText(convPath);
			}
		}
	}
}
