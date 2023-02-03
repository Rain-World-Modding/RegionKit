namespace RegionKit.Modules.ConcealedGarden;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "The Mast")]
internal static class _Module
{
	private static bool __appliedOnce = false;
	public static void Enable()
	{
		if (!__appliedOnce)
		{
			RegisterManagedObject(new ManagedObjectType("CGDrySpot", RK_POM_CATEGORY,
			typeof(CGDrySpot), typeof(CGDrySpot.CGDrySpotData), typeof(ManagedRepresentation)));
			RegisterFullyManagedObjectType(new ManagedField[]
			{
				new BooleanField("noleft", false, displayName:"No Left Door"),
				new BooleanField("noright", false, displayName:"No Right Door"),
				new BooleanField("nowater", false, displayName:"No Water"),
				new BooleanField("zdontstop", false, displayName:"Dont cut song"),
			}, typeof(CGGateCustomization), "CGGateCustomization", RK_POM_CATEGORY);
		}
		__appliedOnce = true;
	}
	public static void Disable()
	{

	}
}
