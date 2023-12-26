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

		var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		ColorHook = new Hook(typeof(OverseerGraphics).GetProperty(nameof(OverseerGraphics.MainColor), flags).GetGetMethod(), OverseerGraphics_MainColor_Get);
	}
	public static void Undo()
	{
		On.OverseerAbstractAI.ctor -= OverseerAbstractAI_ctor;
		On.OverseerAbstractAI.SetAsPlayerGuide -= OverseerAbstractAI_SetAsPlayerGuide;
		ColorHook?.Undo();
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
