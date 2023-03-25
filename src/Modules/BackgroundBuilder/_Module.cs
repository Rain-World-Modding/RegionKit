using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RegionKit.Modules.Effects;
using static RegionKit.Modules.DevUIMisc.SettingsSaveOptions;
using DevInterface;
using System.Diagnostics;
using RegionKit.Modules.BackgroundBuilder;
using MonoMod.RuntimeDetour;
using static AboveCloudsView;
using static RoofTopView;

namespace RegionKit.Modules.BackgroundBuilder;


[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "DevUI")]
public static class _Module
{
	private static bool __appliedOnce = false;
	internal static void Enable()
	{
		return; //still wip
		if (!__appliedOnce)
		{
			//what is this for
		}
		__appliedOnce = true;
		CloudBuilder.Apply();
		BuilderPage.Apply();
		Data.Apply();
		Init.Apply();
		ExceptionFixes.Apply();
		BackgroundUpdates.Apply();
	}
	internal static void Disable()
	{
		return; //still wip
		BuilderPage.Undo();
		ExceptionFixes.Undo();
	}

}
