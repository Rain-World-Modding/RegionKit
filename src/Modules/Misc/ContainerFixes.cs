using MonoMod.Cil;

namespace RegionKit.Modules.Misc
{
	internal static class ContainerFixes
	{
		internal static void Apply()
		{
			On.GateKarmaGlyph.InitiateSprites += GateKarmaGlyph_InitiateSprites;
		}

		internal static void Undo()
		{
			On.GateKarmaGlyph.InitiateSprites -= GateKarmaGlyph_InitiateSprites;
		}

		private static void GateKarmaGlyph_InitiateSprites(On.GateKarmaGlyph.orig_InitiateSprites orig, GateKarmaGlyph self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			self.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("ForegroundLights"));
		}
	}
}
