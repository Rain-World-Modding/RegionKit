using System.Text.RegularExpressions;
using MonoMod.Cil;
using Menu.Remix;

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
		}

		public static void Disable()
		{
			On.SoundLoader.LoadSounds -= SoundLoader_LoadSounds;
			On.Menu.Remix.ConfigContainer._ChangeActiveTab -= ClippySecret;
		}

		public static int count = 4;

		private static void ClippySecret(On.Menu.Remix.ConfigContainer.orig__ChangeActiveTab orig, int newIndex)
		{
			if (ConfigContainer.ActiveInterface.Tabs[newIndex] is ClippyTab && count > 0)
			{	
				count--;
			}
			else
			{
				orig(newIndex);
			}
		}

		private static void SoundLoader_LoadSounds(On.SoundLoader.orig_LoadSounds orig, SoundLoader self)
		{
			_ = _Enums.Clippy_Highscore;
			_ = _Enums.Clippy_Milestone;
			//_ = _Enums.Clippy_Hurt;
			//_ = _Enums.Clippy_Talk;
			_ = _Enums.CatCube_Meow;
			_ = _Enums.Joar_Death;
			orig(self);
		}
	}
}
