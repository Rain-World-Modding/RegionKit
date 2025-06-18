using System.IO;
using System.Text.RegularExpressions;
using HUD;
using Watcher;

namespace RegionKit.Modules.EchoExtender
{
	public class EESpinningTopConversation : SpinningTop.SpinningTopConversation
	{
		protected string region;
		protected string room;

		public EESpinningTopConversation(Ghost ghost, DialogBox dialogBox) : base(_Enums.EESpinningTopConversation, ghost, dialogBox)
		{
			region = ghost.room.world.name;
			room = ghost.room.abstractRoom.name;
		}

		public override void AddEvents()
		{
			InGameTranslator.LanguageID lang = Custom.rainWorld.inGameTranslator.currentLanguage;
			string? text = ResolveSTConversation(lang);
			LogDebug("[Echo Extender] Printing echo conversation");
			LogDebug(text);
			if (text is null && lang != InGameTranslator.LanguageID.English)
			{
				text = ResolveSTConversation(InGameTranslator.LanguageID.English);
			}
			if (text is null)
			{
				LogDebug("[Echo Extender] Could not resolve Spinning Top conversation from file!");
				events.Add(new TextEvent(this, 0, "ECHO EXTENDER ERROR: Could not resolve Spinning Top conversation from file!", 0));
				return;
			}

			foreach (string line in Regex.Split(text, "(\r|\n)+"))
			{
				LogDebug($"[Echo Extender] Processing ST line {line}");
				if (line.All(c => char.IsSeparator(c) || c == '\n' || c == '\r')) continue;
				events.Add(new TextEvent(this, 0, line, 0));
			}
		}

		protected string ResolveSTConversation(InGameTranslator.LanguageID lang)
		{
			string langShort = LocalizationTranslator.LangShort(lang);
			string convPath = AssetManager.ResolveFilePath($"text/text_{langShort}/stConv_{room}.txt");
			bool english = lang == InGameTranslator.LanguageID.English;
			if (!File.Exists(convPath))
			{
				if (!english)
				{
					return null!;
				}
				convPath = AssetManager.ResolveFilePath($"world/{region}/stConv_{room}.txt");
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
