using EffExt;
using RegionKit.Modules.Effects;

namespace RegionKit.Modules.TheMast;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "The Mast")]
internal static class _Module
{
	public static void Setup()
	{
		PearlChains.Apply();
		WindSystem.Apply();
		WormGrassFix.Apply();
		new EffectDefinitionBuilder("FullRoomWind")
			.AddBoolField("Vertical", false)
			.SetUADFactory((room, data, firstTimeRealized) => {
				var wind = new WindSystem.Wind(new PlacedObject(_Enums.PlacedWind, null));
				wind._placedObj.data = new WindSystem.WindData(data, wind._placedObj);
				return wind;
				})
			.SetCategory("RegionKit")
			.Register();
	}
	public static void Enable()
	{
		//ArenaBackgrounds.Apply(); //assigned with AboveCloudsView slider now
		//BetterClouds.Apply(); //replaced by BackgroundBuilder
		RainThreatFix.Apply();
		SkyDandelionBgFix.Apply();
	}
	public static void Disable()
	{
		//ArenaBackgrounds.Undo();
		//BetterClouds.Undo();
		RainThreatFix.Undo();
		SkyDandelionBgFix.Undo();
	}
}
