using static RegionKit.Modules.Effects._Enums;

namespace RegionKit.Modules.Effects;

/// <summary>
/// Replaces the effect colors of the current room with custom colors.
/// By LB/M4rbleL1ne
/// </summary>
public class ReplaceEffectColor : UpdatableAndDeletable
{
	private readonly bool _colorB;

	/// <inheritdoc cref="ReplaceEffectColor"/>
	public ReplaceEffectColor(Room room) => this.room = room;

	/// <inheritdoc cref="ReplaceEffectColor"/>
	public ReplaceEffectColor(Room room, bool colorB)
	{
		this.room = room;
		_colorB = colorB;
	}

	internal static void Apply()
	{
		ColorRoomEffect.colorEffectTypes.Add(ReplaceEffectColorA);
		ColorRoomEffect.colorEffectTypes.Add(ReplaceEffectColorB);
		_CommonHooks.PostRoomLoad += PostRoomLoad;
	}

	/*private static void RoomCamera_ApplyEffectColorsToPaletteTexture(On.RoomCamera.orig_ApplyEffectColorsToPaletteTexture orig, RoomCamera self, ref Texture2D texture, int color1, int color2)
	{
		//unused attempt to make this into a sane hook
		//unfortunately, when palettes are applied, RoomCamera.room is actually the previous room for some stupid reason
		//so we'll just leave the jank UAD for now until I decide to set up some CWTs or smthn

		orig(self, ref texture, color1, color2);
		if (self.room?.roomSettings == null) return;
		RoomSettings rs = self.room.roomSettings;
		for (int i = 0; i < 2; i++)
		{
			bool colorA = i == 0;
			RoomSettings.RoomEffect.Type type = colorA ? ReplaceEffectColorA : ReplaceEffectColorB;
			
			if (rs.IsEffectInRoom(type))
			{
				float a = rs.GetEffectAmount(type), clrar = rs.GetRedAmount(type), clrag = rs.GetGreenAmount(type), clrab = rs.GetBlueAmount(type);
				var clrArA = new Color[] { new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a), new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a) };
				texture.SetPixels(30, colorA ? 2 : 4, 2, 2, clrArA, 0);
				texture.SetPixels(30, colorA ? 10 : 12, 2, 2, clrArA, 0);
			}
		}
	}*/

	internal static void Undo()
	{
		ColorRoomEffect.colorEffectTypes.Remove(ReplaceEffectColorA);
		ColorRoomEffect.colorEffectTypes.Remove(ReplaceEffectColorB);
		_CommonHooks.PostRoomLoad -= PostRoomLoad;
	}

	private static void PostRoomLoad(Room self)
	{
		List<RoomSettings.RoomEffect> efs = self.roomSettings.effects;
		for (var k = 0; k < efs.Count; k++)
		{
			RoomSettings.RoomEffect effect = efs[k];
			if (effect.type == ReplaceEffectColorA || effect.type == ReplaceEffectColorB)
			{
				LogDebug($"ReplaceEffectColor in room {self.abstractRoom.name}");
				self.AddObject(new ReplaceEffectColor(self, effect.type == ReplaceEffectColorB));
			}
		}
	}

	/// <summary>
	/// ReplaceEffectColor Update method.
	/// </summary>
	public override void PausedUpdate()
	{
		base.PausedUpdate();
		if (room?.game is RainWorldGame game)
		{
			RoomCamera[] cams = game.cameras;
			for (var i = 0; i < cams.Length; i++)
			{
				RoomCamera cam = cams[i];
				if (cam.room?.roomSettings is RoomSettings rs)
				{
					RoomSettings.RoomEffect.Type type = _colorB ? ReplaceEffectColorB : ReplaceEffectColorA;
					if (rs.IsEffectInRoom(type))
					{
						float a = rs.GetEffectAmount(type), clrar = rs.GetRedAmount(type), clrag = rs.GetGreenAmount(type), clrab = rs.GetBlueAmount(type);
						var clrArA = new Color[] { new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a), new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a) };
						cam.fadeTexA.SetPixels(30, _colorB ? 2 : 4, 2, 2, clrArA, 0);
						cam.fadeTexA.SetPixels(30, _colorB ? 10 : 12, 2, 2, clrArA, 0);
						if (cam.paletteB > -1)
						{
							cam.fadeTexB.SetPixels(30, _colorB ? 2 : 4, 2, 2, clrArA, 0);
							cam.fadeTexB.SetPixels(30, _colorB ? 10 : 12, 2, 2, clrArA, 0);
						}
						foreach (RoomSettings.FadePalette fade in Misc.MoreFadePalettes.MoreFadeTextures(cam).Keys)
						{
							if (fade is not null && fade.palette != -1)
							{
								Misc.MoreFadePalettes.MoreFadeTextures(cam)[fade].SetPixels(30, _colorB ? 2 : 4, 2, 2, clrArA, 0);
								Misc.MoreFadePalettes.MoreFadeTextures(cam)[fade].SetPixels(30, _colorB ? 10 : 12, 2, 2, clrArA, 0);
							}
						}
					}
				}
				cam.ApplyFade();
			}
		}
	}
}
