using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Effects;

/// <summary>
/// By LB/M4rbleL1ne.
/// </summary>
internal static class PolePlantColor
{
	public static void Apply()
	{
		IL.PoleMimicGraphics.DrawSprites += PoleMimicGraphics_DrawSprites;
		On.RoomSettings.RoomEffect.GetSliderCount += RoomEffect_GetSliderCount;
		On.RoomSettings.RoomEffect.GetSliderDefault += RoomEffect_GetSliderDefault;
		On.RoomSettings.RoomEffect.GetSliderName += RoomEffect_GetSliderName;
	}

	public static void Undo()
	{
		IL.PoleMimicGraphics.DrawSprites -= PoleMimicGraphics_DrawSprites;
		On.RoomSettings.RoomEffect.GetSliderCount -= RoomEffect_GetSliderCount;
		On.RoomSettings.RoomEffect.GetSliderDefault -= RoomEffect_GetSliderDefault;
		On.RoomSettings.RoomEffect.GetSliderName -= RoomEffect_GetSliderName;
	}

	private static string RoomEffect_GetSliderName(On.RoomSettings.RoomEffect.orig_GetSliderName orig, RoomSettings.RoomEffect.Type type, int index)
	{
		if (type == _Enums.PolePlantColor)
			return index switch
			{
				1 => "Red",
				2 => "Green",
				3 => "Blue",
				_ => "Amount"
			};
		return orig(type, index);
	}

	private static float RoomEffect_GetSliderDefault(On.RoomSettings.RoomEffect.orig_GetSliderDefault orig, RoomSettings.RoomEffect.Type type, int index)
	{
		if (type == _Enums.PolePlantColor)
			return index == 1 ? 1f : 0f;
		return orig(type, index);
	}

	private static int RoomEffect_GetSliderCount(On.RoomSettings.RoomEffect.orig_GetSliderCount orig, RoomSettings.RoomEffect.Type type)
	{
		if (type == _Enums.PolePlantColor)
			return 4;
		return orig(type);
	}

	private static void PoleMimicGraphics_DrawSprites(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchNewobj<Color>()))
		{
			c.Emit(OpCodes.Ldarg_2)
			 .EmitDelegate((Color clr, RoomCamera rCam) =>
			 {
				 if (rCam.room is Room rm && rm.roomSettings.GetEffect(_Enums.PolePlantColor) is RoomSettings.RoomEffect ef)
					 return Color.Lerp(clr, new(ef.GetAmount(1), ef.GetAmount(2), ef.GetAmount(3)), ef.amount);
				 return clr;
			 });
		}
		else
			LogError("Couldn't ILHook PoleMimicGraphics.DrawSprites!");
	}
}
