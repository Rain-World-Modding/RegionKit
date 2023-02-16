using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RegionKit.Modules.Effects;

namespace RegionKit.Modules.DevUIMisc
{
	[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "DevUI")]
	public static class _Module
	{
		private static bool __appliedOnce = false;
		internal static void Enable()
		{
			if (!__appliedOnce)
			{
				GlowingSwimmersCI.Apply();
				PWMalfunction.Patch();
			}
			__appliedOnce = true;
			PaletteTextInput.Apply();
			SettingsPathDisplay.Apply();
			BackgroundBuilder.Apply();
		}
		internal static void Disable()
		{
			PaletteTextInput.Undo();
			SettingsPathDisplay.Undo();
			BackgroundBuilder.Undo();
		}




	}
}
