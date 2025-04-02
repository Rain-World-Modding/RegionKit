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
[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Machinery")]
public static class _Module
{
	private const string MACHINERY_POM_CATEGORY = RK_POM_CATEGORY + "-Machinery";
	private static List<IDetour> __machineryHooks = [];
	internal static readonly Dictionary<int, V1.RoomPowerManager> __managersByRoomHash = new Dictionary<int, V1.RoomPowerManager>();
	internal static readonly Dictionary<OscillationMode, Func<float, float>> __defaultOscillators = new() {
		{ OscillationMode.None, (x) => 0f },
		{ OscillationMode.Sinal, Mathf.Sin },
		{ OscillationMode.Cosinal, Mathf.Cos },
		{ OscillationMode.SinalCubed, (x) => Mathf.Pow(Mathf.Sin(x), 3f) },
	};
	public static void Setup()
	{
		RegisterMPO();
		GenerateHooks();
	}

	/// <summary>
	/// Applies hooks and registers MPO.
	/// </summary>
	public static void Enable()
	{
		foreach (var hk in __machineryHooks) if (!hk.IsApplied) hk.Apply();
	}

	/// <summary>
	/// Undoes hooks.
	/// </summary>
	public static void Disable()
	{
		foreach (var hk in __machineryHooks) hk.Undo();
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
		if (__managersByRoomHash.TryGetValue(self.GetHashCode(), out var manager) && obj is V1.RoomPowerManager.IRoomPowerModifier rpm) manager.RegisterPowerDevice(rpm);
	}
	private delegate float orig_Room_GetPower(Room self);
	private static void RWG_new(RWG_Ctor orig, RainWorldGame self, ProcessManager manager)
	{
		__managersByRoomHash.Clear();
		orig(self, manager);
	}
	private delegate void RWG_Ctor(RainWorldGame self, ProcessManager manager);
	private static float Room_GetPower(orig_Room_GetPower orig, Room self)
	{
		if (__managersByRoomHash.TryGetValue(self.GetHashCode(), out var rpm)) return rpm.GetGlobalPower();
		return orig(self);
	}
	private static void GenerateHooks()
	{
		__machineryHooks = new List<IDetour>
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
		RegisterManagedObject<V1.SimplePiston, V1.PistonData, ManagedRepresentation>("SimplePiston", MACHINERY_POM_CATEGORY);
		RegisterManagedObject<V1.PistonArray, V1.PistonArrayData, ManagedRepresentation>("PistonArray", MACHINERY_POM_CATEGORY);
		RegisterEmptyObjectType<V1.MachineryCustomizer, ManagedRepresentation>("MachineryCustomizer", MACHINERY_POM_CATEGORY);
		RegisterManagedObject<V1.SimpleCog, V1.SimpleCogData, ManagedRepresentation>("SimpleCog", MACHINERY_POM_CATEGORY);
		RegisterManagedObject<V1.RoomPowerManager, V1.PowerManagerData, ManagedRepresentation>("PowerManager", MACHINERY_POM_CATEGORY, true);

		RegisterEmptyObjectType<V2.POMVisualsProvider, ManagedRepresentation>("V2MachineryVisuals", MACHINERY_POM_CATEGORY);
		RegisterEmptyObjectType<V2.POMOscillationProvider, ManagedRepresentation>("V2MachineryOscillation", MACHINERY_POM_CATEGORY);
		RegisterManagedObject<V2.SinglePistonController, V2.SinglePistonControllerData, ManagedRepresentation>("V2SinglePiston", MACHINERY_POM_CATEGORY);
		RegisterManagedObject<V2.PistonArrayController, V2.PistonArrayControllerData, ManagedRepresentation>("V2PistonArray", MACHINERY_POM_CATEGORY);
		RegisterManagedObject<V2.SingleWheelController, V2.SingleWheelControllerData, ManagedRepresentation>("V2SingleWheel", MACHINERY_POM_CATEGORY);
	}
}
