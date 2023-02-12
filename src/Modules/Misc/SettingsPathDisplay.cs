using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;

namespace RegionKit.Modules.Misc
{
	internal class SettingsPathDisplay
	{
		internal static void Apply()
		{
			On.DevInterface.Page.ctor += Page_ctor;
		}

		internal static void Undo()
		{
			On.DevInterface.Page.ctor -= Page_ctor;
		}

		private static void Page_ctor(On.DevInterface.Page.orig_ctor orig, DevInterface.Page self, DevInterface.DevUI owner, string IDstring, DevInterface.DevUINode parentNode, string name)
		{
			orig(self, owner, IDstring, parentNode, name);

			string settingsPath = "Settings Path: " + self.RoomSettings.filePath;
			self.subNodes.Add(new DevUILabel(owner, "Settings_Path", null, new Vector2(1330f - 6f * (float)settingsPath.Length, 20f), 20f + 6f * (float)settingsPath.Length, settingsPath));
		}

	}
}
