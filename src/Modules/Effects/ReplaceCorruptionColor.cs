using System.Runtime.CompilerServices;
using static RegionKit.Modules.Effects._Enums;

namespace RegionKit.Modules.Effects
{
	/// <summary>
	/// Replaces DaddyLongLegs default colors in the current room with custom colors.
	/// By MagicaJaphet
	/// </summary>
	internal class ReplaceCorruptionColors : UpdatableAndDeletable
	{
		private static ConditionalWeakTable<Room, CorruptionValues> corruptionCWT = new();

		/// <inheritdoc cref="ReplaceCorruptionColors"/>
		public ReplaceCorruptionColors(Room room)
		{
			this.room = room;
			corruptionCWT.GetOrCreateValue(room);
		}

		internal static void Apply()
		{
			ColorRoomEffect.colorEffectTypes.Add(ReplaceCorruptionColor);
			_CommonHooks.PostRoomLoad += PostRoomLoad;
			On.CorruptionSpore.DrawSprites += CorruptionSpore_DrawSprites;
			On.DaddyCorruption.DrawSprites += DaddyCorruption_DrawSprites;
			On.DaddyGraphics.DrawSprites += DaddyGraphics_DrawSprites;
		}

		private static void PostRoomLoad(Room self)
		{
			List<RoomSettings.RoomEffect> efs = self.roomSettings.effects;
			for (var k = 0; k < efs.Count; k++)
			{
				RoomSettings.RoomEffect effect = efs[k];
				if (effect.type == ReplaceCorruptionColor)
				{
					LogDebug($"ReplaceCorruptionColor in room {self.abstractRoom.name}");
					self.AddObject(new ReplaceCorruptionColors(self));
				}
			}
		}

		private static void DaddyCorruption_DrawSprites(On.DaddyCorruption.orig_DrawSprites orig, DaddyCorruption self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (self.room != null && corruptionCWT.TryGetValue(self.room, out CorruptionValues corruption))
			{
				if (corruption._originalEffectColor == null || corruption._originalEffectColor == default)
				{
					corruption._originalEffectColor = self.effectColor;
					corruption._originalEyeColor = self.eyeColor;
				}
				else
				{
					self.effectColor = Color.Lerp(corruption._originalEffectColor, corruption._replacementColor, corruption._amount);
					self.eyeColor = Color.Lerp(corruption._originalEyeColor, corruption._replacementColor, corruption._amount);

					self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
				}
			}
		}

		private static void CorruptionSpore_DrawSprites(On.CorruptionSpore.orig_DrawSprites orig, CorruptionSpore self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (self.room != null && corruptionCWT.TryGetValue(self.room, out CorruptionValues corruption))
			{
				if (corruption._originalSporeColor == null || corruption._originalSporeColor == default)
				{
					corruption._originalSporeColor = self switch
					{
						var _ when self.calcified => new Color(0.25f, 0.25f, 0.25f),
						var _ when self.sentient => RainWorld.RippleColor,
						_ => new Color(0f, 0f, 1f)
					};
				}
				else
				{
					sLeaser.sprites[0].color = Color.Lerp(Color.black, Color.Lerp(corruption._originalSporeColor, corruption._replacementColor, corruption._amount), self.col);
				}
			}
		}

		private static void DaddyGraphics_DrawSprites(On.DaddyGraphics.orig_DrawSprites orig, DaddyGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);

			if (self.owner is DaddyLongLegs dLL && dLL.room != null && corruptionCWT.TryGetValue(dLL.room, out CorruptionValues corruption))
			{
				if (corruption._originalEffectColor == null || corruption._originalEffectColor == default)
				{
					corruption._originalEffectColor = dLL.effectColor;
					corruption._originalEyeColor = dLL.eyeColor;
				}
				else
				{
					dLL.effectColor = Color.Lerp(corruption._originalEffectColor, corruption._replacementColor, corruption._amount);
					dLL.eyeColor = Color.Lerp(corruption._originalEyeColor, corruption._replacementColor, corruption._amount);

					self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
				}
			}
		}

		internal static void Undo()
		{
			ColorRoomEffect.colorEffectTypes.Remove(ReplaceCorruptionColor);
			_CommonHooks.PostRoomLoad -= PostRoomLoad;
			On.CorruptionSpore.DrawSprites -= CorruptionSpore_DrawSprites;
			On.DaddyCorruption.DrawSprites -= DaddyCorruption_DrawSprites;
			On.DaddyGraphics.DrawSprites -= DaddyGraphics_DrawSprites;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			if (room?.roomSettings is RoomSettings rs && rs.IsEffectInRoom(ReplaceCorruptionColor) && corruptionCWT.TryGetValue(room, out CorruptionValues corruption))
			{
				RoomSettings.RoomEffect.Type type = ReplaceCorruptionColor;
				corruption._amount = rs.GetEffectAmount(type);
				corruption._replacementColor = new(rs.GetRedAmount(type), rs.GetGreenAmount(type), rs.GetBlueAmount(type));
			}
		}
	}

	internal class CorruptionValues
	{
		internal float _amount;
		internal Color _replacementColor;
		internal Color _lastReplacementColor;
		internal Color _originalEffectColor;
		internal Color _originalEyeColor;
		internal Color _originalSporeColor;
	}
}
