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


[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "BackgroundBuilder")]
public static class _Module
{
	public const string BGPath = "Assets\\RegionKit\\Backgrounds";
	internal static void Enable()
	{
		Data.Apply();
		Init.Apply();
		BuilderPageHooks.Apply();
		ExceptionFixes.Apply();
		BackgroundUpdates.Apply();
	}
	internal static void Disable()
	{
		Data.Undo();
		Init.Undo();
		BuilderPageHooks.Undo();
		ExceptionFixes.Undo();
		BackgroundUpdates.Undo();
	}

}
