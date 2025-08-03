using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace RegionKit.Modules.Misc;

/// <summary>
/// By LB/M4rbleL1ne.
/// Please leave this public, the game needs to be able to access this class and the method RKLeaveTextureIntact.
/// </summary>
public static class GhostEffectColorsFix
{
	internal static void Apply() => IL.RoomCamera.ApplyEffectColorsToAllPaletteTextures += IL_RoomCamera_ApplyEffectColorsToAllPaletteTextures;

	internal static void Undo() => IL.RoomCamera.ApplyEffectColorsToAllPaletteTextures -= IL_RoomCamera_ApplyEffectColorsToAllPaletteTextures;

	internal static void IL_RoomCamera_ApplyEffectColorsToAllPaletteTextures(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdflda<RoomCamera>("ghostFadeTex"),
			x => x.MatchLdarg(1),
			x => x.MatchLdarg(2),
			x => x.MatchCallOrCallvirt<RoomCamera>("ApplyEffectColorsToPaletteTexture")))
		{
			// I change an instruction, but I need to remove this call anyway so that's fine
			Instruction prev = c.Prev;
			prev.OpCode = OpCodes.Call;
			prev.Operand = il.Import(typeof(GhostEffectColorsFix).GetMethod("RKLeaveTextureIntact"));
		}
		else
			LogError("Couldn't ILHook RoomCamera.ApplyEffectColorsToAllPaletteTexture!");
	}

	// Please leave this public, the game needs to be able to access this class and the method RKLeaveTextureIntact.
	[MethodImpl(MethodImplOptions.NoInlining), SuppressMessage("", "IDE0060")]
	public static void RKLeaveTextureIntact(RoomCamera self, ref Texture2D texture, int color1, int color2) { }
}
