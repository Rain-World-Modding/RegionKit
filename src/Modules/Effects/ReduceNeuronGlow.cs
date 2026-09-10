using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Modules.Effects;

internal static class ReduceNeuronGlow
{
	public static void Apply()
	{
		On.PlayerGraphics.Update += PlayerGraphics_Update;
	}
	public static void Undo()
	{
		On.PlayerGraphics.Update -= PlayerGraphics_Update;
	}


	private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
	{
		orig(self);
		if (self.lightSource != null && self.player.room != null)
		{
			float amount = self.player.room.roomSettings.GetEffectAmount(_Enums.ReduceNeuronGlow);
			if (amount > 0)
			{
				self.lightSource.setAlpha = 1f - amount;
			}
		}
	}
}
