namespace RegionKit.API;

using Impl = RegionKit.Modules.Misc.ExtendedGates;

public static class ExtendedGates
{
	/// <summary>
	/// Attempts registering a new lock condition.
	/// </summary>
	/// <param name="req">GateRequirement extended value your lock should be associated with.</param>
	/// <param name="lock">Your lock object.</param>
	/// <typeparam name="TL">Type of your lock object.</typeparam>
	/// <returns>true if lock could be registered; false if there was already another lock registered for this GateRequirement.</returns>
	public static bool TryRegisterLock<TL>(RegionGate.GateRequirement req, TL @lock)
		where TL : Impl.ExtendedLocks.LockData 
        => Impl.ExLocks.TryAdd(req, @lock);
	/// <summary>
	/// Attempts registering a new lock condition, using raw data rather than a class implementing LockData.
	/// </summary>
	/// <param name="req">GateRequirement extended value your lock should be associated with.</param>
	/// <param name="gateElementName">Name of the gate hologram sprite element.</param>
	/// <param name="mapElementName">Name of the map icon sprite element.</param>
	/// <param name="openCondition">A callback that will be called each time a gate needs to check whether it should open.</param>
	/// <returns>true if lock could be registered; false if there was already another lock registered for this GateRequirement.</returns>
	public static bool TryRegisterLockCallback(RegionGate.GateRequirement req, string gateElementName, string mapElementName,  Func<RegionGate, bool> openCondition) 
        => TryRegisterLock(req, new Impl.ExtendedLocks.DelegateDriven(req, gateElementName, mapElementName, openCondition));
	/// <summary>
	/// Attempts removing a registered lock associated with a given GateRequirement.
	/// </summary>
	/// <param name="req">GateRequirement value you want to disassociate from a LockData.</param>
	/// <returns>true if lock could be unregistered; false if there was no registered lock for this GateRequirement.</returns>
	public static bool TryUnRegisterLock(RegionGate.GateRequirement req)
        => Impl.ExLocks.TryRemove(req);
}