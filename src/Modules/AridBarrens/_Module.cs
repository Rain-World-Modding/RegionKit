using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegionKit.Modules.AridBarrens;

[RegionKitModule(nameof(Register), nameof(Disable), moduleName: "AridBarrens")]
static class _Module
{
	public static void Register()
	{
		_CommonHooks.PostRoomLoad += RoomPostLoad;
	}
	public static void Disable()
	{
		_CommonHooks.PostRoomLoad -= RoomPostLoad;
	}

	private static void RoomPostLoad(Room self)
	{
		for (int k = 0; k < self.roomSettings.effects.Count; k++)
		{
			if (self.roomSettings.effects[k].type == EnumExt_AridBarrens.SandStorm)
			{
				self.AddObject(new SandStorm(self.roomSettings.effects[k], self));
			}
			else if (self.roomSettings.effects[k].type == EnumExt_AridBarrens.SandPuffs)
			{
				self.AddObject(new SandPuffs(self.roomSettings.effects[k], self));
			}
		}
	}
}
