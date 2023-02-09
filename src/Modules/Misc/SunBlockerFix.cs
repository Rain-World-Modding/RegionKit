namespace RegionKit.Modules.Misc;

/// <summary>
/// By LB/M4rbleL1ne.
/// </summary>
internal static class SunBlockerFix
{
	internal static void Apply() => On.SunBlocker.InitiateSprites += SunBlockerInitiateSprites;

	internal static void Undo() => On.SunBlocker.InitiateSprites -= SunBlockerInitiateSprites;

	private static void SunBlockerInitiateSprites(On.SunBlocker.orig_InitiateSprites orig, SunBlocker self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		orig(self, sLeaser, rCam);
		sLeaser.sprites[0].scaleX = 1500f;
		sLeaser.sprites[0].scaleY = 900f;
		self.AddToContainer(sLeaser, rCam, null);
	}
}
