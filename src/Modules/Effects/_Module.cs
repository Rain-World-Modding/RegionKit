using DevInterface;

namespace RegionKit.Modules.Effects;

///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Effects")]
public static class _Module
{
	private static bool __appliedOnce = false;

	internal static void Enable()
	{
		if (!__appliedOnce)
			PWMalfunction.Patch();
		__appliedOnce = true;
		GlowingSwimmersCI.Apply();
		ColoredCamoBeetlesCI.Apply();
		ColorRoomEffect.Apply();
		ReplaceEffectColor.Apply();
		On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType += RoomSettingsPageDevEffectGetCategoryFromEffectType;
	}

	internal static void Disable()
	{
		GlowingSwimmersCI.Undo();
		ColoredCamoBeetlesCI.Undo();
		ColorRoomEffect.Undo();
		ReplaceEffectColor.Undo();
		On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType -= RoomSettingsPageDevEffectGetCategoryFromEffectType;
	}

	private static RoomSettingsPage.DevEffectsCategories RoomSettingsPageDevEffectGetCategoryFromEffectType(On.DevInterface.RoomSettingsPage.orig_DevEffectGetCategoryFromEffectType orig, RoomSettingsPage self, RoomSettings.RoomEffect.Type type)
	{
		RoomSettingsPage.DevEffectsCategories res = orig(self, type);
		if (type == _Enums.ReplaceEffectColorA ||
			type == _Enums.ReplaceEffectColorB ||
			type == _Enums.FogOfWarSolid ||
			type == _Enums.FogOfWarDarkened ||
			type == _Enums.CloudAdjustment ||
			type == _Enums.GlowingSwimmers ||
			type == _Enums.ColoredCamoBeetles ||
			type == _Enums.PWMalfunction)
			res = _Enums.RegionKit;
		return res;
	}
}
