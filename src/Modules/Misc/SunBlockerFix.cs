/// <summary>
/// By LB Gamer/M4rbleL1ne
/// </summary>

namespace RegionKit
{
	public class SunBlockerFix
	{
		public static void Apply()
		{
			On.SunBlocker.InitiateSprites += (orig, self, sLeaser, rCam) =>
            {
                orig(self, sLeaser, rCam);
                sLeaser.sprites[0].scaleX = 1500f;
                sLeaser.sprites[0].scaleY = 900f;
                self.AddToContainer(sLeaser, rCam, null);
            };
        }
	}
}
