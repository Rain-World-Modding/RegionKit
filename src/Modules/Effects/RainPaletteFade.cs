using System.Reflection;
using MonoMod.RuntimeDetour;

namespace RegionKit.Modules.Effects
{
	internal static class RainPaletteFade
	{
		private static readonly List<IDetour> _hooks = [];
		internal static void Apply()
		{
			_hooks.Add(new Hook(typeof(RoomCamera).GetProperty(nameof(RoomCamera.DarkPalette), BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true), RoomCamera_get_DarkPalette));
		}

		internal static void Undo()
		{
			foreach (IDetour hook in _hooks)
			{
				hook.Undo();
				hook.Dispose();
			}
			_hooks.Clear();
		}

		private static float RoomCamera_get_DarkPalette(Func<RoomCamera, float> orig, RoomCamera self)
		{
			float f = orig(self);
			if (self.room != null)
			{
				return Mathf.Max(f, self.room.roomSettings.GetEffectAmount(_Enums.RainPaletteFade));
			}
			return f;
		}
	}
}
