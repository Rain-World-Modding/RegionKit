namespace RegionKit.Modules.Effects;

internal static class DenseFogHooks
{
    internal static void Apply()
    {
		//Don't delete this, it's needed to prevent a crash
		On.SoundLoader.LoadSounds += SoundLoader_LoadSounds;
		On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
		On.RoomRain.AddToContainer += RoomRain_AddToContainer;
		On.DeathFallGraphic.AddToContainer += DeathFallGraphic_AddToContainer;
		On.Room.Loaded += Room_Loaded;
    }

	internal static void Undo()
	{
		On.SoundLoader.LoadSounds -= SoundLoader_LoadSounds;
		On.RoomCamera.ApplyPalette -= RoomCamera_ApplyPalette;
		On.RoomRain.AddToContainer -= RoomRain_AddToContainer;
		On.DeathFallGraphic.AddToContainer -= DeathFallGraphic_AddToContainer;
		On.Room.Loaded -= Room_Loaded;
	}

	private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera self)
	{
		orig(self);
		if (self.room is Room rm && rm.roomSettings.GetEffectAmount(_Enums.DenseFog) > 0f)
		{
			self.SetUpFullScreenEffect("Foreground");
			self.fullScreenEffect.shader = self.game.rainWorld.Shaders["Fog"];
			self.lightBloomAlphaEffect = _Enums.DenseFog;
			self.lightBloomAlpha = (rm.roomSettings.GetEffectAmount(_Enums.DenseFog) + (rm.world.rainCycle.timer / 100000f)) * 2f;
		}
	}

	private static void RoomRain_AddToContainer(On.RoomRain.orig_AddToContainer orig, RoomRain self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		orig(self, sLeaser, rCam, newContatiner);
		if (self.room?.roomSettings?.GetEffect(_Enums.DenseFog) is not null)
			sLeaser.sprites[0].MoveToBack();
	}

	private static void DeathFallGraphic_AddToContainer(On.DeathFallGraphic.orig_AddToContainer orig, DeathFallGraphic self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		orig(self, sLeaser, rCam, newContatiner);
		if (rCam.room is Room rm && rm.roomSettings.GetEffect(_Enums.DenseFog) is not null && (rm.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.AboveCloudsView) is not null || rm.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.RoofTopView) is not null))
		{
			FSprite spr = sLeaser.sprites[0];
			spr.RemoveFromContainer();
			rCam.ReturnFContainer("GrabShaders").AddChild(spr);
			spr.MoveToBack();
		}
	}

	private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
	{
		orig(self);
		if (self.game is not null)
		{
			List<RoomSettings.RoomEffect> efs = self.roomSettings.effects;
			for (var k = 0; k < efs.Count; k++)
			{
				if (efs[k].type == _Enums.DenseFog)
				{
					self.AddObject(new DenseFogGradient(self));
					break;
				}
			}
		}
	}

	private static void SoundLoader_LoadSounds(On.SoundLoader.orig_LoadSounds orig, SoundLoader self)
	{
		_ = _Enums.FT_Fog_PreDeath;
		orig(self);
	}
}
