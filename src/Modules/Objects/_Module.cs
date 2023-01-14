namespace RegionKit.Modules.Objects;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "MiscObjects")]
public static class _Module
{
	private static bool _ranOnce = false;
	public static void Enable()
	{
		if (!_ranOnce)
		{
			ColouredLightSource.RegisterAsFullyManagedObject();
			Drawable.Register();
			//RegisterManagedObject<PWLightRod, PWLightRodData, PWLightRodRepresentation>("PWLightRod", false);
			//TODO: apply pwlr
			ShroudObjRep.ShroudRep();
			SpinningFanObjRep.SpinningFanRep();
			SteamObjRep.SteamRep();
		}
		_ranOnce = true;


	}
	public static void Disable()
	{


	}

}
