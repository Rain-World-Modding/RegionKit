using System;

namespace RegionKit.Modules.AridBarrens;

public class EnumExt_AridBarrens
{
	public static RoomSettings.RoomEffect.Type SandStorm = new(nameof(SandStorm), true);
	public static RoomSettings.RoomEffect.Type SandPuffs = new(nameof(SandPuffs), true);
}
