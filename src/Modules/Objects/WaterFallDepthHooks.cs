using MonoMod.RuntimeDetour;

namespace RegionKit.Modules.Objects
{
    // In hindsight I should have realized the original methods weren't able to be overridden and just made a completely new one. Oh well! A bit late for that now~
    // (well actually it's not too late I'm just stubborn)
    static class WaterFallDepthHooks
    {
		private static readonly List<IDetour> mhooks = [];
        public static void Apply()
        {
            On.WaterFall.InitiateSprites += WaterFall_InitiateSprites;
            On.WaterFall.AddToContainer += WaterFall_AddToContainer;
            On.WaterFall.DrawSprites += WaterFall_DrawSprites;
            mhooks.Add(new Hook(typeof(WaterFall).GetProperty(nameof(WaterFall.strikeLevel)).GetGetMethod(), WaterFall_get_strikeLevel));
        }

		public static void Undo()
		{
			On.WaterFall.InitiateSprites -= WaterFall_InitiateSprites;
			On.WaterFall.AddToContainer -= WaterFall_AddToContainer;
			On.WaterFall.DrawSprites -= WaterFall_DrawSprites;
			foreach (IDetour hook in mhooks)
			{
				hook.Undo();
			}
		}

        private static void WaterFall_InitiateSprites(On.WaterFall.orig_InitiateSprites orig, WaterFall self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (self is WaterFallDepth)
            {
                sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["WaterFallDepth"];
            }
        }

        private static void WaterFall_AddToContainer(On.WaterFall.orig_AddToContainer orig, WaterFall self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            bool wasNull = newContatiner is null;
            orig(self, sLeaser, rCam, newContatiner);
            if (wasNull && self is WaterFallDepth)
            {
                sLeaser.sprites[0].RemoveFromContainer();
                rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[0]);
            }
        }

        private static void WaterFall_DrawSprites(On.WaterFall.orig_DrawSprites orig, WaterFall self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (!self.slatedForDeletetion && self.room == rCam.room && self is WaterFallDepth wfd)
            {
                sLeaser.sprites[0].alpha = 1f - wfd.Data.depth;
				sLeaser.sprites[0].scaleX = wfd.Data.width * 20f / 16f;
			}
        }

        private static float WaterFall_get_strikeLevel(Func<WaterFall, float> orig, WaterFall self)
        {
            return orig(self) + (self is WaterFallDepth wfd ? wfd.HeightOffset * (self.room.waterInverted ? -1f : 1f) : 0f);
        }
    }
}
