namespace RegionKit.Modules.Insects
{
	public static class _Enums
	{
		// Room effects
		public static RoomSettings.RoomEffect.Type GlowingSwimmers = new(nameof(GlowingSwimmers), true);
		public static RoomSettings.RoomEffect.Type ColoredCamoBeetles = new(nameof(ColoredCamoBeetles), true);
		public static RoomSettings.RoomEffect.Type MosquitoInsects = new(nameof(MosquitoInsects), true);
		public static RoomSettings.RoomEffect.Type ButterfliesA = new(nameof(ButterfliesA), true);
		public static RoomSettings.RoomEffect.Type ButterfliesB = new(nameof(ButterfliesB), true);
		public static RoomSettings.RoomEffect.Type Zippers = new(nameof(Zippers), true);
		public static RoomSettings.RoomEffect.Type Seedlings = new(nameof(Seedlings), true);
		public static RoomSettings.RoomEffect.Type RippleFlies = new(nameof(RippleFlies), true);
		public static RoomSettings.RoomEffect.Type RippleGlowworms = new(nameof(RippleGlowworms), true);
		public static RoomSettings.RoomEffect.Type SI_Dragonflies = new(nameof(SI_Dragonflies), true);

		// Cosmetic insect types
		public static CosmeticInsect.Type GlowingSwimmerInsect = new(nameof(GlowingSwimmerInsect), true);
		public static CosmeticInsect.Type ColoredCamoBeetle = new(nameof(ColoredCamoBeetle), true);
		public static CosmeticInsect.Type MosquitoInsect = new(nameof(MosquitoInsect), true);
		public static CosmeticInsect.Type ButterflyA = new(nameof(ButterflyA), true);
		public static CosmeticInsect.Type ButterflyB = new(nameof(ButterflyB), true);
		public static CosmeticInsect.Type Zipper = new(nameof(Zipper), true);
		public static CosmeticInsect.Type Seedling = new(nameof(Seedling), true);
		public static CosmeticInsect.Type RippleFly = new(nameof(RippleFly), true);
		public static CosmeticInsect.Type RippleGlowworm = new(nameof(RippleGlowworm), true);
		public static CosmeticInsect.Type SI_Dragonfly = new(nameof(SI_Dragonfly), true);

		// Insect category
		public static DevInterface.RoomSettingsPage.DevEffectsCategories RegionKitInsects = new("RegionKit-Insects", true);


		/// <summary>
		/// Returns whether or not the cosmetic insect type is from RegionKit
		/// </summary>
		/// <param name="type">The cosmetic insect type</param>
		/// <returns>If the enum is from RegionKit</returns>

		public static bool IsRegionKitInsect(this CosmeticInsect.Type type)
		{
			return type == GlowingSwimmerInsect
				|| type == ColoredCamoBeetle
				|| type == MosquitoInsect
				|| type == ButterflyA
				|| type == ButterflyB
				|| type == Zipper
				|| type == Seedling
				|| type == RippleFly
				|| type == RippleGlowworm
				|| type == SI_Dragonfly;
		}

		/// <summary>
		/// Returns whether or not the room effect type is a cosmetic insect from RegionKit
		/// </summary>
		/// <param name="type">The room effect type</param>
		/// <returns>If the enum is from RegionKit</returns>
		public static bool IsRegionKitInsect(this RoomSettings.RoomEffect.Type type)
		{
			return type == GlowingSwimmers
				|| type == ColoredCamoBeetles
				|| type == MosquitoInsects
				|| type == ButterfliesA
				|| type == ButterfliesB
				|| type == Zippers
				|| type == Seedlings
				|| type == RippleFlies
				|| type == RippleGlowworms
				|| type == SI_Dragonflies;
		}
	}
}
