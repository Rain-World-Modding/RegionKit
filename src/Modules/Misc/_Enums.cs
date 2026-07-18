namespace RegionKit.Modules.Misc
{
	public static class _Enums
	{
		static _Enums()
		{
			_ = RoomPalette.ColorName.BlackColor; // static initialize first so items appear at the end
			EffectColor1 = new(nameof(EffectColor1), true);
			EffectColor2 = new(nameof(EffectColor2), true);
			White = new(nameof(White), true);
		}

		public static RoomPalette.ColorName EffectColor1;
		public static RoomPalette.ColorName EffectColor2;
		public static RoomPalette.ColorName White;

	}
}
