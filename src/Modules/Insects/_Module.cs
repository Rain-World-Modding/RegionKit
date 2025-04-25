using DevInterface;
using EffExt;

namespace RegionKit.Modules.Insects
{
	[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Insects")]
	public static class _Module
	{
		internal static void Setup()
		{
		}

		internal static void Enable()
		{
			GlowingSwimmersCI.Apply();
			ColoredCamoBeetlesCI.Apply();
			MosquitoInsectsCI.Apply();
			ButterfliesCI.Apply();
			ZippersCI.Apply();

			On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType += RoomSettingsPageDevEffectGetCategoryFromEffectType;
		}

		internal static void Disable()
		{
			GlowingSwimmersCI.Undo();
			ColoredCamoBeetlesCI.Undo();
			MosquitoInsectsCI.Undo();
			ButterfliesCI.Undo();
			ZippersCI.Undo();
			On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType -= RoomSettingsPageDevEffectGetCategoryFromEffectType;
		}

		private static RoomSettingsPage.DevEffectsCategories RoomSettingsPageDevEffectGetCategoryFromEffectType(On.DevInterface.RoomSettingsPage.orig_DevEffectGetCategoryFromEffectType orig, RoomSettingsPage self, RoomSettings.RoomEffect.Type type)
		{
			RoomSettingsPage.DevEffectsCategories res = orig(self, type);
			if (type == _Enums.GlowingSwimmers ||
				type == _Enums.ColoredCamoBeetles ||
				type == _Enums.MosquitoInsects ||
				type == _Enums.ButterfliesA ||
				type == _Enums.ButterfliesB ||
				type == _Enums.Zippers)
				res = _Enums.RegionKitInsects;
			return res;
		}
	}
}
