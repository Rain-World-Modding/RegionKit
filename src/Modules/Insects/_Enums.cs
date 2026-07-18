namespace RegionKit.Modules.Insects
{
	public static class _Enums
	{
		/// <summary>
		/// Glowing Swimmer effect enum
		/// </summary>
		public static RoomSettings.RoomEffect.Type GlowingSwimmers = new(nameof(GlowingSwimmers), true);
		/// <summary>
		/// Colored Camo Beetle effect enum
		/// </summary>
		public static RoomSettings.RoomEffect.Type ColoredCamoBeetles = new(nameof(ColoredCamoBeetles), true);
		/// <summary>
		/// Mosquito Insect effect enum
		/// </summary>
		public static RoomSettings.RoomEffect.Type MosquitoInsects = new(nameof(MosquitoInsects), true);
		/// <summary>
		/// Butterfly A effect enum
		/// </summary>
		public static RoomSettings.RoomEffect.Type ButterfliesA = new(nameof(ButterfliesA), true);
		/// <summary>
		/// Butterfly B effect enum
		/// </summary>
		public static RoomSettings.RoomEffect.Type ButterfliesB = new(nameof(ButterfliesB), true);
		/// <summary>
		/// Zipper effect enum
		/// </summary>
		public static RoomSettings.RoomEffect.Type Zippers = new(nameof(Zippers), true);
		/// <summary>
		/// Seedling effect enum
		/// </summary>
		public static RoomSettings.RoomEffect.Type Seedlings = new(nameof(Seedlings), true);
		/// <summary>
		/// Ripple flies effect enum
		/// </summary>
		public static RoomSettings.RoomEffect.Type RippleFlies = new(nameof(RippleFlies), true);
		/// <summary>
		/// Ripple glowworms effect enum
		/// </summary>
		public static RoomSettings.RoomEffect.Type RippleGlowworms = new(nameof(RippleGlowworms), true);


		/// <summary>
		/// Glowing Swimmer insect enum
		/// </summary>
		public static CosmeticInsect.Type GlowingSwimmerInsect = new(nameof(GlowingSwimmerInsect), true);
		/// <summary>
		/// Colored Camo Beetle insect enum
		/// </summary>
		public static CosmeticInsect.Type ColoredCamoBeetle = new(nameof(ColoredCamoBeetle), true);
		/// <summary>
		/// Mosquito Insect enum
		/// </summary>
		public static CosmeticInsect.Type MosquitoInsect = new(nameof(MosquitoInsect), true);
		/// <summary>
		/// Butterfly A enum
		/// </summary>
		public static CosmeticInsect.Type ButterflyA = new(nameof(ButterflyA), true);
		/// <summary>
		/// Butterfly B enum
		/// </summary>
		public static CosmeticInsect.Type ButterflyB = new(nameof(ButterflyB), true);
		/// <summary>
		/// Zipper enum
		/// </summary>
		public static CosmeticInsect.Type Zipper = new(nameof(Zipper), true);
		/// <summary>
		/// Seedling enum
		/// </summary>
		public static CosmeticInsect.Type Seedling = new(nameof(Seedling), true);
		/// <summary>
		/// Ripple fly enum
		/// </summary>
		public static CosmeticInsect.Type RippleFly = new(nameof(RippleFly), true);
		/// <summary>
		/// Ripple glowworm enum
		/// </summary>
		public static CosmeticInsect.Type RippleGlowworm = new(nameof(RippleGlowworm), true);


		/// <summary>
		/// Insect category
		/// </summary>
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
				|| type == RippleGlowworm;
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
				|| type == RippleGlowworms;
		}
	}
}
