using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Misc
{
	internal static class LightningFix
	{
		public static void Apply()
		{
			IL.RoomCamera.ApplyPositionChange += RoomCamera_ApplyPositionChange;
		}

		public static void Undo()
		{
			IL.RoomCamera.ApplyPositionChange -= RoomCamera_ApplyPositionChange;
		}

		private static void RoomCamera_ApplyPositionChange(ILContext il)
		{
			var c = new ILCursor(il);

			c.GotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<RoomCamera>(nameof(RoomCamera.UpdateGhostMode)));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((RoomCamera self) => self.ApplyFade());
		}
	}
}
