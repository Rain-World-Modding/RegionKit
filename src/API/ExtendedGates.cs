namespace RegionKit.API;

using Impl = RegionKit.Modules.Misc.ExtendedGates;

public static class ExtendedGates
{
	public static bool TryRegisterLock<TL>(RegionGate.GateRequirement req, TL @lock)
		where TL : Impl.ExtendedLocks.LockData 
        => Impl.ExLocks.TryAdd(req, @lock);
	public static bool TryRegisterLockCallback(RegionGate.GateRequirement req, string gateElementName, string mapElementName,  Func<RegionGate, bool> openCondition) 
        => TryRegisterLock(req, new Impl.ExtendedLocks.DelegateDriven(req, gateElementName, mapElementName, openCondition));
	public static bool TryUnRegisterLock<TL>(RegionGate.GateRequirement req)
		where TL : Impl.ExtendedLocks.LockData 
        => Impl.ExLocks.TryRemove(req);
}