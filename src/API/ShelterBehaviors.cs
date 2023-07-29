namespace RegionKit.API;

using Impl = RegionKit.Modules.ShelterBehaviors;

public static class ShelterBehaviors
{
    /// <summary>
    /// Whether all shelters should wait for player to hold down in order to start closing.
    /// </summary>
	public static bool GlobalOverrideHoldToTrigger
	{
		get => Impl.ShelterBehaviorManager.Override_HTT;
		set { Impl.ShelterBehaviorManager.Override_HTT = value; }
	}
}