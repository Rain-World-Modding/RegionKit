namespace RegionKit;
/// <summary>
/// By LB Gamer/M4rbleL1ne.
/// </summary>
public class SunBlockerFix
{
	private static On.SunBlocker.hook_InitiateSprites hook_sb = (orig, self, sLeaser, rCam) =>
	{
		orig(self, sLeaser, rCam);
		sLeaser.sprites[0].scaleX = 1500f;
		sLeaser.sprites[0].scaleY = 900f;
		self.AddToContainer(sLeaser, rCam, null);
	};

	internal static void Apply()
	{
		On.SunBlocker.InitiateSprites += hook_sb;
	}
	internal static void Undo()
	{
		On.SunBlocker.InitiateSprites -= hook_sb;
	}

}
