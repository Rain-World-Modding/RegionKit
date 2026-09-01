namespace RegionKit.Modules.Misc
{
	internal static class VirtualMicrophoneFix
	{
		internal static void Apply()
		{
			On.RoomCamera.ClearAllSprites += RoomCamera_ClearAllSprites;
		}

		internal static void Undo()
		{
			On.RoomCamera.ClearAllSprites -= RoomCamera_ClearAllSprites;
		}

		private static void RoomCamera_ClearAllSprites(On.RoomCamera.orig_ClearAllSprites orig, RoomCamera self)
		{
			if (self.virtualMicrophone != null)
			{
				self.virtualMicrophone.visualization?.RemoveFromContainer();
				self.virtualMicrophone.visualization2?.RemoveFromContainer();
				self.virtualMicrophone.samplesText?.RemoveFromContainer();
				self.virtualMicrophone.samplesText2?.RemoveFromContainer();
				self.virtualMicrophone.visualize = false;
			}
			orig(self);
		}
	}
}
