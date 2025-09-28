using System.Text.RegularExpressions;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Objects
{
	// Mostly re-adding compatibility from that which was lost during 1.10 (Watcher update) when some RegionKit things were merged in-game
	internal static class _CompatHooks
	{
		public static void Enable()
		{
			try
			{
				On.RainbowNoFade.RainbowNoFadeData.FromString += RainbowNoFadeData_FromString;
			}
			catch (Exception ex)
			{
				LogError(ex);
			}
		}

		public static void Disable()
		{
			try
			{
				On.RainbowNoFade.RainbowNoFadeData.FromString -= RainbowNoFadeData_FromString;
			}
			catch (Exception ex)
			{
				LogError(ex);
			}
		}

		private static void RainbowNoFadeData_FromString(On.RainbowNoFade.RainbowNoFadeData.orig_FromString orig, global::RainbowNoFade.RainbowNoFadeData self, string s)
		{
			if (s.Contains('|'))
			{
				string[] split = Regex.Split(s, "~");
				split[4] = Regex.Replace(split[4], "\\|", ",");
				s = string.Join("~", split);
			}
			orig(self, s);
		}
	}
}
