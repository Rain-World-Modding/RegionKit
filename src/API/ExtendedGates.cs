namespace RegionKit.API;

using RegionKit.Modules.ExtendedGates;
using Impl = Modules.ExtendedGates.ExtendedGates;

/// <summary>
/// Functionality for adding custom gate activation conditions.
/// </summary>
public static class ExtendedGates
{
	/// <summary>
	/// Attempts registering a new lock condition.
	/// </summary>
	/// <param name="req">GateRequirement extended value your lock should be associated with.</param>
	/// <param name="lock">Your lock object.</param>
	public static void RegisterLock(RegionGate.GateRequirement req, LockData @lock)
	{
		ThrowIfModNotInitialized();
		if (!Impl.ExLocks.TryAdd(req, @lock))
		{
			throw new ArgumentException($"A lock has already been registered for GateRequirement {req}");
		}
	}

	/// <summary>
	/// Attempts registering a new lock condition, using raw data rather than a class implementing <see cref="LockData"/>.
	/// </summary>
	/// <param name="req">GateRequirement extended value your lock should be associated with.</param>
	/// <param name="gateElementName">Name of the gate hologram sprite element.</param>
	/// <param name="mapElementName">Name of the map icon sprite element.</param>
	/// <param name="openCondition">A callback that will be used each time a gate needs to check whether it should open.</param>
	public static void RegisterLockCallback(RegionGate.GateRequirement req, string gateElementName, string mapElementName, Func<RegionGate, bool> openCondition)
	{
		ThrowIfModNotInitialized();
		RegisterLock(req, new ExtendedLocks.DelegateDriven(req, gateElementName, mapElementName, openCondition));
	}

	/// <summary>
	/// Registers a new extra requirement condition.
	/// </summary>
	/// <param name="extraRequirement">Your extra requirement object</param>
	public static void RegisterExtraRequirement(ExtraRequirement extraRequirement)
	{
		ThrowIfModNotInitialized();
		Impl.ExtraRequirements.Add(extraRequirement);
	}

	/// <summary>
	/// Registers a new extra requirement condition, using raw data rather than a class implementing <see cref="ExtraRequirement"/>
	/// </summary>
	/// <param name="keyword">The keyword to use in the tag</param>
	/// <param name="spriteName">The sprite name to show on the map and at the gate</param>
	/// <param name="spriteScale">The scaling factor for the sprite. Should aim to be around 24px</param>
	/// <param name="conditionToPass">The condition checked for whether or not to pass</param>
	public static void RegisterExtraRequirementCallback(string keyword, string spriteName, float spriteScale, Func<SaveState, bool> conditionToPass)
	{
		ThrowIfModNotInitialized();
		RegisterExtraRequirement(new ExtendedRequirements.DelegateDriven(keyword, spriteName, spriteScale, conditionToPass, null));
	}

	/// <summary>
	/// Registers a new extra requirement condition, using raw data rather than a class implementing <see cref="ExtraRequirement"/>
	/// </summary>
	/// <param name="keyword">The keyword to use in the tag</param>
	/// <param name="spriteName">The sprite name to show on the map and at the gate</param>
	/// <param name="spriteScale">The scaling factor for the sprite. Should aim to be around 24px</param>
	/// <param name="mapCondition">The condition checked to display as able to pass on the region map</param>
	/// <param name="gateCondition">The actual condition to check while at the region gate to determine whether or not to pass</param>
	public static void RegisterExtraRequirementCallback(string keyword, string spriteName, float spriteScale, Func<SaveState, bool> mapCondition, Func<RegionGate, bool> gateCondition)
	{
		ThrowIfModNotInitialized();
		RegisterExtraRequirement(new ExtendedRequirements.DelegateDriven(keyword, spriteName, spriteScale, mapCondition, gateCondition));
	}

	/// <summary>
	/// Attempts removing a registered lock associated with a given GateRequirement.
	/// </summary>
	/// <param name="req">GateRequirement value you want to disassociate from a LockData.</param>
	/// <returns>true if lock could be unregistered; false if there was no registered lock for this GateRequirement.</returns>
	public static bool UnregisterLock(RegionGate.GateRequirement req)
	{
		ThrowIfModNotInitialized();
		return Impl.ExLocks.Remove(req);
	}

	/// <summary>
	/// Attempts removing a registered extra requirement
	/// </summary>
	/// <param name="extraRequirement">Instance of extra requirement to remove</param>
	/// <returns>true if extra requirement was unregistered, false if this instance of extra requirement was not registered</returns>
	public static bool UnregisterExtraRequirement(ExtraRequirement extraRequirement)
	{
		ThrowIfModNotInitialized();
		return Impl.ExtraRequirements.Remove(extraRequirement);
	}

	/// <summary>
	/// Attempts removing a registered extra requirement with a given keyword
	/// </summary>
	/// <param name="keyword">Keyword to remove</param>
	/// <returns>true if extra requirement was unregistered, false if there was no registered extra requirement with this keyword</returns>
	public static bool UnregisterExtraRequirement(string keyword)
	{
		ThrowIfModNotInitialized();
		return Impl.ExtraRequirements.RemoveAll(x => x.BaseKeyword == keyword) > 0;
	}
}
