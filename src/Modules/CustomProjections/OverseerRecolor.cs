using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.CustomProjections;

internal static class OverseerRecolor
{
	static Hook? ColorHook;
	public static void Apply()
	{
		On.OverseerAbstractAI.ctor += OverseerAbstractAI_ctor;
		On.OverseerAbstractAI.SetAsPlayerGuide += OverseerAbstractAI_SetAsPlayerGuide;
		On.HologramLight.InitiateSprites += HologramLight_InitiateSprites;

		var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		ColorHook = new Hook(typeof(OverseerGraphics).GetProperty(nameof(OverseerGraphics.MainColor), flags).GetGetMethod(), OverseerGraphics_MainColor_Get);
	}

	public static void Undo()
	{
		On.OverseerAbstractAI.ctor -= OverseerAbstractAI_ctor;
		On.OverseerAbstractAI.SetAsPlayerGuide -= OverseerAbstractAI_SetAsPlayerGuide;
		On.HologramLight.InitiateSprites -= HologramLight_InitiateSprites;
		ColorHook?.Undo();
	}

	private static void HologramLight_InitiateSprites(On.HologramLight.orig_InitiateSprites orig, HologramLight self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		orig(self, sLeaser, rCam);

		if ((self.overseer.abstractCreature.abstractAI as OverseerAbstractAI)?.ownerIterator == 1) return;

		Color color = (self.overseer.graphicsModule as OverseerGraphics)!.MainColor;

		sLeaser.sprites[0].color = color;
		sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["HKHoloGrid"];
		Shader.SetGlobalVector("_HKHoloGridColor", color;

		for (int i = 1; i < sLeaser.sprites.Length; i++)
		{ sLeaser.sprites[i].color = color; }
	}

	private static void OverseerAbstractAI_SetAsPlayerGuide(On.OverseerAbstractAI.orig_SetAsPlayerGuide orig, OverseerAbstractAI self, int ownerOverride)
	{
		if (OverseerProperties.GetOverseerProperties(self.world.region).guideID != -1)
		{ ownerOverride = OverseerProperties.GetOverseerProperties(self.world.region).guideID; }

		orig(self, ownerOverride);
	}

	public static Color OverseerGraphics_MainColor_Get(Func<OverseerGraphics, Color> orig, OverseerGraphics self)
	{
		int id = (self.overseer.abstractCreature.abstractAI as OverseerAbstractAI)!.ownerIterator;

		if (!OverseerProperties.BaseIndex(id))
		{
			if (OverseerProperties.GetOverseerProperties(self.overseer.room.world.region).overseerColorLookup.TryGetValue(id, out var color))
			{ return color; }
		}
		return orig(self);
	}

	private static void OverseerAbstractAI_ctor(On.OverseerAbstractAI.orig_ctor orig, OverseerAbstractAI self, World world, AbstractCreature parent)
	{
		orig(self, world, parent);

		var properties = OverseerProperties.GetOverseerProperties(world.region);
		foreach (Color color in properties.overseerColorChances.OrderByDescending(x => x.Value).Select(x => x.Key))
		{
			if (Random.value < properties.overseerColorChances[color])
			{ self.ownerIterator = properties.GetOverseerID(color); }
		}
	}
}
