//  - - - - -
//  machinery module
//  author: thalber
//  unlicense
//  - - - - -

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using MonoMod.RuntimeDetour;

namespace RegionKit.Modules.Machinery;
/// <summary>
/// Handles registration of machinery objects.
/// </summary>
[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Machinery")]
public static class _Module
{
	private static bool appliedOnce = false;
	/// <summary>
	/// Applies hooks and registers MPO.
	/// </summary>
	public static void Enable()
	{
		if (!appliedOnce)
		{
			RegisterMPO();
			GenerateHooks();
		}
		else
		{
			foreach (var hk in MachineryHooks) hk.Apply();
		}
		appliedOnce = true;


	}

	internal static List<Hook> MachineryHooks = null!;

	/// <summary>
	/// Undoes hooks.
	/// </summary>
	public static void Disable()
	{
		foreach (var hk in MachineryHooks) hk.Undo();
	}

	//internal static RainWorld rw;
	#region hooks
	private static void RW_Start(RainWorld_Start orig, RainWorld self)
	{
		orig(self);
		//rw = self;
	}
	private delegate void RainWorld_Start(RainWorld self);
	private static void Room_AddObject(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
	{
		orig(self, obj);
		if (ManagersByRoom.TryGetValue(self.GetHashCode(), out var manager) && obj is V1.RoomPowerManager.IRoomPowerModifier rpm) manager.RegisterPowerDevice(rpm);
	}
	private delegate float orig_Room_GetPower(Room self);
	private static void RWG_new(RWG_Ctor orig, RainWorldGame self, ProcessManager manager)
	{
		ManagersByRoom.Clear();
		orig(self, manager);
	}
	private delegate void RWG_Ctor(RainWorldGame self, ProcessManager manager);
	private static float Room_GetPower(orig_Room_GetPower orig, Room self)
	{
		if (ManagersByRoom.TryGetValue(self.GetHashCode(), out var rpm)) return rpm.GetGlobalPower();
		return orig(self);
	}
	private static void GenerateHooks()
	{
		MachineryHooks = new List<Hook>
			{
				new Hook(
					typeof(RainWorld).GetMethodAllContexts(nameof(RainWorld.Start)),
					typeof(_Module).GetMethodAllContexts(nameof(RW_Start))),
				new Hook(
					typeof(Room).GetMethodAllContexts(nameof(Room.AddObject)),
					typeof(_Module).GetMethodAllContexts(nameof(Room_AddObject))),
				new Hook(
					typeof(Room).GetPropertyAllContexts(nameof(Room.ElectricPower))!.GetGetMethod(),
					typeof(_Module).GetMethodAllContexts(nameof(Room_GetPower))),
				new Hook(
					typeof(RainWorldGame).GetConstructor(new Type[]{ typeof(ProcessManager)}),
					typeof(_Module).GetMethodAllContexts(nameof(RWG_new)))
			};
	}
	#endregion

	private static void RegisterMPO()
	{
		RegisterManagedObject<V1.SimplePiston, V1.PistonData, ManagedRepresentation>("SimplePiston");
		RegisterManagedObject<V1.PistonArray, V1.PistonArrayData, ManagedRepresentation>("PistonArray");
		RegisterEmptyObjectType<V1.MachineryCustomizer, ManagedRepresentation>("MachineryCustomizer");
		RegisterManagedObject<V1.SimpleCog, V1.SimpleCogData, ManagedRepresentation>("SimpleCog");
		RegisterManagedObject<V1.RoomPowerManager, V1.PowerManagerData, ManagedRepresentation>("PowerManager", true);
	}
	public static readonly Dictionary<int, V1.RoomPowerManager> ManagersByRoom = new Dictionary<int, V1.RoomPowerManager>();
}


public static class EnumExt_RKMachinery
{
	//public static AbstractPhysicalObject.AbstractObjectType abstractPiston;
}

/// <summary>
/// Machinery operation modes.
/// </summary>
public enum OperationMode
{
	Sinal = 2,
	Cosinal = 4,
}
/// <summary>
/// Used as filter for <see cref="MachineryCustomizer"/>
/// </summary>
public enum MachineryID
{
	Piston,
	Cog
}

