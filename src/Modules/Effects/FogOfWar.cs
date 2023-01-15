using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using EffectType = RoomSettings.RoomEffect.Type;

namespace RegionKit.Modules.Effects;

/// <summary>
/// Limits vision to everything in line-of-sight.
/// </summary>
internal static class FogOfWar
{
	public static void Patch()
	{
		//On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
	}
#if false

	private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
	{
		orig(self, timeStacker, timeSpeed);
		List<RoomCamera.SpriteLeaser> sLeasers = self.spriteLeasers;

		FogOfWarController.hackToDelayDrawingUntilAfterTheLevelMoves = true;
		for (int i = 0; i < sLeasers.Count; i++)
		{
			if (!(sLeasers[i].drawableObject is FogOfWarController)) continue;
			sLeasers[i].Update(timeStacker, self, Vector2.zero);
			if (sLeasers[i].deleteMeNextFrame)
				sLeasers.RemoveAt(i);
		}
		FogOfWarController.hackToDelayDrawingUntilAfterTheLevelMoves = false;
	}

	public static void Refresh(Room room)
	{
		foreach (var obj in room.updateList)
		{
			if (obj is FogOfWarController)
				return;
		}

		foreach (var effect in room.roomSettings.effects)
		{
			if (FogOfWarController.GetFogType(effect.type) != FogOfWarController.FogType.None)
				room.AddObject(new FogOfWarController(room));
		}
	}
#endif
}
