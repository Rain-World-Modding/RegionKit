using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast;

internal static class SkyDandelionBgFix
{
	private static RoomSettings.RoomEffect.Type[] __bgEffects = new RoomSettings.RoomEffect.Type[]
	{
		RoomSettings.RoomEffect.Type.AboveCloudsView,
		RoomSettings.RoomEffect.Type.RoofTopView,
		RoomSettings.RoomEffect.Type.VoidSea
	};
	public static void Apply()
	{
		On.SkyDandelions.SkyDandelion.AddToContainer += SkyDandelion_AddToContainer;
	}
	public static void Undo()
	{
		On.SkyDandelions.SkyDandelion.AddToContainer -= SkyDandelion_AddToContainer;
	}
	private static bool HasBackgroundScene(Room room)
	{
		for (int i = 0; i < __bgEffects.Length; i++)
			if (room.roomSettings.GetEffect(__bgEffects[i]) != null) return true;
		return false;
	}
	private static void SkyDandelion_AddToContainer(On.SkyDandelions.SkyDandelion.orig_AddToContainer orig, SkyDandelions.SkyDandelion self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		if (HasBackgroundScene(rCam.room))
		{
			newContatiner = rCam.ReturnFContainer("GrabShaders");
			sLeaser.sprites[0].RemoveFromContainer();
			newContatiner.AddChild(sLeaser.sprites[0]);
			if (sLeaser.sprites.Length == 2)
			{
				sLeaser.sprites[1].RemoveFromContainer();
				rCam.ReturnFContainer("Shadows").AddChild(sLeaser.sprites[1]);
			}
		}
		else
			orig(self, sLeaser, rCam, newContatiner);
	}
}
