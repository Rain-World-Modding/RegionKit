using UnityEngine;
using static RegionKit.Modules.Effects.ReplaceEffectColor.EnumExt_ReplaceEffectColor;

namespace RegionKit.Modules.Effects;

public class ReplaceEffectColor : UpdatableAndDeletable /// By M4rbleL1ne/LB Gamer
{
	public static class EnumExt_ReplaceEffectColor
	{
		public static RoomSettings.RoomEffect.Type ReplaceEffectColorA = new(nameof(ReplaceEffectColorA), true);
		public static RoomSettings.RoomEffect.Type ReplaceEffectColorB = new(nameof(ReplaceEffectColorB), true);
	}

	public ReplaceEffectColor(Room room) => this.room = room;

	internal static void Apply()
	{
		On.RainWorld.Start += (orig, self) =>
		{
			orig(self);
			ColoredRoomEffect.coloredEffects.Add(ReplaceEffectColorA);
			ColoredRoomEffect.coloredEffects.Add(ReplaceEffectColorB);
		};
		On.Room.Loaded += (orig, self) =>
		{
			orig(self);
			for (var k = 0; k < self.roomSettings.effects.Count; k++)
			{
				var effect = self.roomSettings.effects[k];
				if (effect.type == ReplaceEffectColorA || effect.type == ReplaceEffectColorB)
					self.AddObject(new ReplaceEffectColor(self));
			}
		};
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		if (room?.game is not null)
		{
			foreach (var cam in room.game.cameras)
			{
				if (cam.room?.roomSettings is not null)
				{
					var a = cam.room.roomSettings.GetEffectAmount(ReplaceEffectColorA);
					var b = cam.room.roomSettings.GetEffectAmount(ReplaceEffectColorB);
					var clrar = cam.room.roomSettings.GetColoredEffectRed(ReplaceEffectColorA);
					var clrbr = cam.room.roomSettings.GetColoredEffectRed(ReplaceEffectColorB);
					var clrag = cam.room.roomSettings.GetColoredEffectGreen(ReplaceEffectColorA);
					var clrbg = cam.room.roomSettings.GetColoredEffectGreen(ReplaceEffectColorB);
					var clrab = cam.room.roomSettings.GetColoredEffectBlue(ReplaceEffectColorA);
					var clrbb = cam.room.roomSettings.GetColoredEffectBlue(ReplaceEffectColorB);
					if (cam.room.roomSettings.IsEffectInRoom(ReplaceEffectColorA))
					{
						cam.fadeTexA.SetPixels(30, 4, 2, 2, new Color[] { new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a), new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a) }, 0);
						cam.fadeTexA.SetPixels(30, 12, 2, 2, new Color[] { new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a), new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a) }, 0);
					}
					if (cam.room.roomSettings.IsEffectInRoom(ReplaceEffectColorB))
					{
						cam.fadeTexA.SetPixels(30, 2, 2, 2, new Color[] { new(clrbr, clrbg, clrbb), new(clrbr - b, clrbg - b, clrbb - b), new(clrbr, clrbg, clrbb), new(clrbr - b, clrbg - b, clrbb - b) }, 0);
						cam.fadeTexA.SetPixels(30, 10, 2, 2, new Color[] { new(clrbr, clrbg, clrbb), new(clrbr - b, clrbg - b, clrbb - b), new(clrbr, clrbg, clrbb), new(clrbr - b, clrbg - b, clrbb - b) }, 0);
					}
					if (cam.paletteB > -1)
					{
						if (cam.room.roomSettings.IsEffectInRoom(ReplaceEffectColorA))
						{
							cam.fadeTexB.SetPixels(30, 4, 2, 2, new Color[] { new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a), new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a) }, 0);
							cam.fadeTexB.SetPixels(30, 12, 2, 2, new Color[] { new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a), new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a) }, 0);
						}
						if (cam.room.roomSettings.IsEffectInRoom(ReplaceEffectColorB))
						{
							cam.fadeTexB.SetPixels(30, 2, 2, 2, new Color[] { new(clrbr, clrbg, clrbb), new(clrbr - b, clrbg - b, clrbb - b), new(clrbr, clrbg, clrbb), new(clrbr - b, clrbg - b, clrbb - b) }, 0);
							cam.fadeTexB.SetPixels(30, 10, 2, 2, new Color[] { new(clrbr, clrbg, clrbb), new(clrbr - b, clrbg - b, clrbb - b), new(clrbr, clrbg, clrbb), new(clrbr - b, clrbg - b, clrbb - b) }, 0);
						}
					}
				}
				cam.ApplyFade();
			}
		}
	}
}
