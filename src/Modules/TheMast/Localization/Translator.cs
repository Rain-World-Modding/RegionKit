using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast
{
	internal static class Translator
	{
		public static string GetString(string name, InGameTranslator.LanguageID language)
		{
			string langName;
			if (language == InGameTranslator.LanguageID.English) langName = "";
			else langName = LocalizationTranslator.LangShort(language) + "-";
			string? ret = _Assets.GetUTF8("TheMast", $"{name}_{language}.txt");
			//todo: better failure message
			ret ??= "THIS SPACE UNINTENTIONALLY LEFT BLANK";
			return ret;
		}
	}
}
