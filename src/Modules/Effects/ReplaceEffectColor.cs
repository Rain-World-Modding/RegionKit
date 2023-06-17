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

	internal static void Undo()
	{
		ColorRoomEffect.colorEffectTypes.Remove(ReplaceEffectColorA);
		ColorRoomEffect.colorEffectTypes.Remove(ReplaceEffectColorB);
		_CommonHooks.PostRoomLoad -= PostRoomLoad;
	}

	private static void PostRoomLoad(Room self)
	{
		for (var k = 0; k < self.roomSettings.effects.Count; k++)
		{
			RoomSettings.RoomEffect effect = self.roomSettings.effects[k];
			if (effect.type == ReplaceEffectColorA || effect.type == ReplaceEffectColorB)
			{
				__logger.LogDebug($"ReplaceEffectColor in room {self.abstractRoom.name}");
				self.AddObject(new ReplaceEffectColor(self, effect.type == ReplaceEffectColorB));
			}
		}
	}

	/// <summary>
	/// ReplaceEffectColor Update method.
	/// </summary>
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (room?.game is RainWorldGame game)
		{
			foreach (RoomCamera cam in game.cameras)
			{
				if (cam.room?.roomSettings is RoomSettings rs)
				{
					var flag = cam.paletteB > -1;
					if (!_colorB && rs.IsEffectInRoom(ReplaceEffectColorA))
					{
						float a = rs.GetEffectAmount(ReplaceEffectColorA), clrar = rs.GetRedAmount(ReplaceEffectColorA), clrag = rs.GetGreenAmount(ReplaceEffectColorA), clrab = rs.GetBlueAmount(ReplaceEffectColorA);
						var clrArA = new Color[] { new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a), new(clrar, clrag, clrab), new(clrar - a, clrag - a, clrab - a) };
						cam.fadeTexA.SetPixels(30, 4, 2, 2, clrArA, 0);
						cam.fadeTexA.SetPixels(30, 12, 2, 2, clrArA, 0);
						if (flag)
						{
							cam.fadeTexB.SetPixels(30, 4, 2, 2, clrArA, 0);
							cam.fadeTexB.SetPixels(30, 12, 2, 2, clrArA, 0);
						}
					}
					else if (_colorB && rs.IsEffectInRoom(ReplaceEffectColorB))
					{
						float b = rs.GetEffectAmount(ReplaceEffectColorB), clrbr = rs.GetRedAmount(ReplaceEffectColorB), clrbg = rs.GetGreenAmount(ReplaceEffectColorB), clrbb = rs.GetBlueAmount(ReplaceEffectColorB);
						var clrArB = new Color[] { new(clrbr, clrbg, clrbb), new(clrbr - b, clrbg - b, clrbb - b), new(clrbr, clrbg, clrbb), new(clrbr - b, clrbg - b, clrbb - b) };
						cam.fadeTexA.SetPixels(30, 2, 2, 2, clrArB, 0);
						cam.fadeTexA.SetPixels(30, 10, 2, 2, clrArB, 0);
						if (flag)
						{
							cam.fadeTexB.SetPixels(30, 2, 2, 2, clrArB, 0);
							cam.fadeTexB.SetPixels(30, 10, 2, 2, clrArB, 0);
						}
					}
				}
				cam.ApplyFade();
			}
		}
	}
}
