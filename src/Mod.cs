namespace RegionKit;

[BIE.BepInPlugin("rwmodding.coreorg.rk", "RegionKit", "2.0")]
public class Mod : BIE.BaseUnityPlugin
{
	internal static Mod inst = null!;
	internal static LOG.ManualLogSource plog => inst.Logger;
	public void OnEnable()
	{
		inst = this;
		
	}

	public void OnDisable()
	{
		inst = null!;

	}
}
