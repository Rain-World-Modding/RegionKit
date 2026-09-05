using System.Text.RegularExpressions;
using MonoMod.Cil;
using Menu.Remix;
using System.IO;

namespace RegionKit.OptionsMenu.ClippyGame
{
	[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Keybinds")]

	internal static class _Module
	{
		public static void Setup()
		{

		}

		public static void Enable()
		{
			On.SoundLoader.LoadSounds += SoundLoader_LoadSounds;

			On.Menu.Remix.ConfigContainer._ChangeActiveTab += ClippySecret;
			On.Menu.Remix.ConfigTabController.TabSelectButton.GrafUpdate += UpdateTabName;
			On.Menu.Remix.ConfigTabController.TabSelectButton.DisplayDescription += UpdateDesc;

			On.OptionInterface._TriggerOnUnload += ClippySave;
			On.OptionInterface._TriggerOnDeactivate += ClippyStopMusicWhenNotSelected;
		}

		public static void Disable()
		{
			On.SoundLoader.LoadSounds -= SoundLoader_LoadSounds;

			On.Menu.Remix.ConfigContainer._ChangeActiveTab -= ClippySecret;
			On.Menu.Remix.ConfigTabController.TabSelectButton.GrafUpdate -= UpdateTabName;
			On.Menu.Remix.ConfigTabController.TabSelectButton.DisplayDescription -= UpdateDesc;

			On.OptionInterface._TriggerOnUnload -= ClippySave;
			On.OptionInterface._TriggerOnDeactivate -= ClippyStopMusicWhenNotSelected;
		}

		private static void ClippyStopMusicWhenNotSelected(On.OptionInterface.orig__TriggerOnDeactivate orig, OptionInterface self)
		{
			orig(self);

			if (self is ModOptions)
			{
				(self.Tabs[ModOptions.KB_INDEX] as ClippyTab).music?.Destroy();
				(self.Tabs[ModOptions.KB_INDEX] as ClippyTab).music = null;
			}
		}

		private static void ClippySave(On.OptionInterface.orig__TriggerOnUnload orig, OptionInterface self)
		{
			orig(self);

			if (self is ModOptions)
			{
				(self.Tabs[ModOptions.KB_INDEX] as ClippyTab).music?.Destroy();
				(self.Tabs[ModOptions.KB_INDEX] as ClippyTab).music = null;
				(self.Tabs[ModOptions.KB_INDEX] as ClippyTab).saver.Save();
			}
		}

		public static int count = File.Exists(ClippySaver.path) ? -1 : 20;

		private static void ClippySecret(On.Menu.Remix.ConfigContainer.orig__ChangeActiveTab orig, int newIndex)
		{
			if (ConfigContainer.ActiveInterface.Tabs[newIndex] is ClippyTab)
			{	
				if (count > 0)
				{
					count--;
				}
				else
				{
					count = -1;
					orig(newIndex);
				}
			}
			else
			{
				orig(newIndex);
			}
		}

		private static void UpdateTabName(On.Menu.Remix.ConfigTabController.TabSelectButton.orig_GrafUpdate orig, ConfigTabController.TabSelectButton self, float timeStacker)
		{
			orig(self, timeStacker);

			if (self.RepresentingIndex < ConfigContainer.ActiveInterface.Tabs.Length && self.RepresentingTab is ClippyTab)
			{
				string t = "";

				if (count != -1)
				{
					for (int i = 0; i < count + 1; i++)
					{
						t += "?";
					}
				}
				else
				{
					t = "Clippy";
				}

				self._label.text = t;
			}
		}

		private static string UpdateDesc(On.Menu.Remix.ConfigTabController.TabSelectButton.orig_DisplayDescription orig, ConfigTabController.TabSelectButton self)
		{
			if (self.RepresentingIndex < ConfigContainer.ActiveInterface.Tabs.Length && self.RepresentingTab is ClippyTab && count != -1)
			{
				return "Switch to tab " + self._label.text;
			}

			return orig(self);
		}

		private static void SoundLoader_LoadSounds(On.SoundLoader.orig_LoadSounds orig, SoundLoader self)
		{
			_ = _Enums.Clippy_Highscore;
			_ = _Enums.Clippy_Milestone;
			_ = _Enums.Clippy_Hurt;
			_ = _Enums.Clippy_Talk;
			_ = _Enums.CatCube_Meow;
			_ = _Enums.Joar_Death;
			_ = _Enums.Clippy_Song;
			orig(self);
		}
	}
}
