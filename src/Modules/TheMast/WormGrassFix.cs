using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using UnityEngine;

//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast;

internal static class WormGrassFix
{
	public static void Apply()
	{
		// Disable scavenger terrain-clipping protection when in wormgrass
		On.PhysicalObject.Update += PhysicalObject_Update;
		On.Scavenger.Update += Scavenger_Update;

		// Disable water physics when in wormgrass
		new Hook(
			typeof(BodyChunk).GetProperty("submersion", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(),
			typeof(WormGrassFix).GetMethod("BodyChunk_submersion")
		);
	}

	public static float BodyChunk_submersion(Func<BodyChunk, float> orig, BodyChunk self)
	{
		if ((self.owner.room?.world?.name == "TM") && !self.collideWithTerrain) return 0f;
		return orig(self);
	}

	private static bool __clipScavBody;
	private static Vector2 __clipPos;
	private static Vector2 __lastClipPos;
	private static Vector2 __lastLastClipPos;
	private static void Scavenger_Update(On.Scavenger.orig_Update orig, Scavenger self, bool eu)
	{
		orig(self, eu);
		if (__clipScavBody)
		{
			BodyChunk mbc = self.mainBodyChunk;
			mbc.lastLastPos = __lastLastClipPos;
			mbc.lastPos = __lastClipPos;
			mbc.pos = __clipPos;
			__clipScavBody = false;
		}
	}

	private static void PhysicalObject_Update(On.PhysicalObject.orig_Update orig, PhysicalObject self, bool eu)
	{
		__clipScavBody = false;
		if (self is Scavenger scav && (self.room?.world?.name == "TM"))
		{
			if (!self.bodyChunks[0].collideWithTerrain)
			{
				self.bodyChunks[2].collideWithTerrain = false;
				__clipScavBody = true;
			}
		}
		orig(self, eu);
		if (__clipScavBody)
		{
			BodyChunk mbc = (self as Creature)!.mainBodyChunk;
			__lastLastClipPos = mbc.lastLastPos;
			__lastClipPos = mbc.lastPos;
			__clipPos = mbc.pos;
		}
	}
}
