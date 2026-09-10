using DevInterface;

namespace RegionKit.Modules.Objects;

///<inheritdoc/>
public class _Enums
{
	// Categories
	public static ObjectsPage.DevObjectCategories GameplayCategory = new(RK_POM_CATEGORY + "-Gameplay", true);
	public static ObjectsPage.DevObjectCategories DecorationsCategory = new(RK_POM_CATEGORY + "-Decorations", true);
	public static ObjectsPage.DevObjectCategories MiscObjectsCategory = new(RK_POM_CATEGORY + "-MiscObjects", true);

	// Moon dialogue
	public static SLOracleBehaviorHasMark.MiscItemType EvilDangleFruitDialogue = new(nameof(EvilDangleFruitDialogue), true);

	// Abstract object types
	public static AbstractPhysicalObject.AbstractObjectType SpikeTip = new(nameof(SpikeTip), true);

	// Placed object types
	public static PlacedObject.Type FreeformDecalOrSprite = new(nameof(FreeformDecalOrSprite), true);
	public static PlacedObject.Type ColouredLightSource = new(nameof(ColouredLightSource), true);
	public static PlacedObject.Type Shroud = new(nameof(Shroud), true);
	public static PlacedObject.Type SpinningFan = new(nameof(SpinningFan), true);
	public static PlacedObject.Type SteamHazard = new(nameof(SteamHazard), true);
	public static PlacedObject.Type Spike = new(nameof(Spike), true);
	public static PlacedObject.Type RoomBorderTP = new(nameof(RoomBorderTP), true);
	public static PlacedObject.Type WormgrassRect = new(nameof(WormgrassRect), true);
	public static PlacedObject.Type PlacedWaterfall = new(nameof(PlacedWaterfall), true);
	public static PlacedObject.Type ShortcutColor = new(nameof(ShortcutColor), true);
	public static PlacedObject.Type ShortcutCannon = new(nameof(ShortcutCannon), true);
	public static PlacedObject.Type CameraNoise = new(nameof(CameraNoise), true);
	public static PlacedObject.Type SlugcatEyeSelector = new(nameof(SlugcatEyeSelector), true);
	public static PlacedObject.Type BigKarmaShrine = new(nameof(BigKarmaShrine), true);
	public static PlacedObject.Type KarmaShrineSprite = new(nameof(KarmaShrineSprite), true);
	public static PlacedObject.Type CustomWallMycelia = new(nameof(CustomWallMycelia), true);
	public static PlacedObject.Type GuardProtectNode = new(nameof(GuardProtectNode), true);
	public static PlacedObject.Type SlipperyZone = new(nameof(SlipperyZone), true);
	public static PlacedObject.Type WaterSpout = new(nameof(WaterSpout), true);
	public static PlacedObject.Type RoomPopupTrigger = new(nameof(RoomPopupTrigger), true);
	public static PlacedObject.Type ResizeablePopupTrigger = new(nameof(ResizeablePopupTrigger), true);
	public static PlacedObject.Type RectanglePopupTrigger = new(nameof(RectanglePopupTrigger), true);
	public static PlacedObject.Type ClimbableWire = new(nameof(ClimbableWire), true);
	public static PlacedObject.Type ClimbablePole = new(nameof(ClimbablePole), true);
	public static PlacedObject.Type PWLightrod = new(nameof(PWLightrod), true);
	public static PlacedObject.Type CustomEntranceSymbol = new(nameof(CustomEntranceSymbol), true);
	public static PlacedObject.Type NoWallSlideZone = new(nameof(NoWallSlideZone), true);
	public static PlacedObject.Type LittlePlanet = new(nameof(LittlePlanet), true);
	public static PlacedObject.Type ProjectedCircle = new(nameof(ProjectedCircle), true);
	public static PlacedObject.Type UpsideDownWaterFall = new(nameof(UpsideDownWaterFall), true);
	public static PlacedObject.Type ColoredLightBeam = new(nameof(ColoredLightBeam), true);
	public static PlacedObject.Type FanLight = new(nameof(FanLight), true);
	public static PlacedObject.Type NoBatflyLurkZone = new(nameof(NoBatflyLurkZone), true);
	public static PlacedObject.Type PCPlayerSensitiveLightSource = new(nameof(PCPlayerSensitiveLightSource), true);
	public static PlacedObject.Type WaterFallDepth = new(nameof(WaterFallDepth), true);
	public static PlacedObject.Type NoDropwigPerchZone = new(nameof(NoDropwigPerchZone), true);
	public static PlacedObject.Type EvilDangleFruit = new(nameof(EvilDangleFruit), true);
	public static PlacedObject.Type BGFlatLight = new(nameof(BGFlatLight), true);
	public static PlacedObject.Type AdvancedShader = new(nameof(AdvancedShader), true);
	public static PlacedObject.Type BigWaterWheel = new(nameof(BigWaterWheel), true);
	public static PlacedObject.Type ColoredSSFuses = new(nameof(ColoredSSFuses), true);
	public static PlacedObject.Type ColoredMudPit = new(nameof(ColoredMudPit), true);
	public static PlacedObject.Type GreenSparksDir = new(nameof(GreenSparksDir), true);
	public static PlacedObject.Type ColoredLocalBlizzard = new(nameof(ColoredLocalBlizzard), true);
}
