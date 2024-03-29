namespace RegionKit.API;

using Impl = RegionKit.Modules.ShelterBehaviors;

/// <summary>
/// Functionality for changing shelter activation conditions and more.
/// </summary>
public static class ShelterBehaviors
{
	/// <summary>
	/// Whether all shelters should wait for player to hold down in order to start closing.
	/// </summary>
	public static bool GlobalOverrideHoldToTrigger
	{
		get
		{
			ThrowIfModNotInitialized();
			return Impl.ShelterBehaviorManager.Override_HTT;
		}
		set
		{
			ThrowIfModNotInitialized();
			Impl.ShelterBehaviorManager.Override_HTT = value;
		}
	}
}