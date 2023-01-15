//Author : DELTATIME
using System.Reflection;
using MonoMod.RuntimeDetour;
using UnityEngine;
namespace RegionKit.Modules.Effects;
//Enum is extended in RoomLoader.cs
class PWMalfunction
{
	//Hooks setup
	public static void Patch()
	{
		On.AntiGravity.BrokenAntiGravity.Update += BrokenAntiGravity_Update;
		//On.AntiGravity.ctor += AntiGravity_Ctor;
		On.ZapCoil.Update += ZapCoil_Update;
		On.Redlight.ctor_Room_PlacedObject_LightFixtureData += Redlight_Ctor;
		On.SuperStructureFuses.ctor += SuperStructureFuses_Ctor;
		On.SuperStructureFuses.Update += SuperStructureFuses_Update;
		On.GravityDisruptor.Update += GravityDisruptor_Update;
		On.RegionGate.Update += RegionGate_Update;
		//On.ZapCoilLight.ctor += ZapcoilLight_Ctor;
		// Garrakx's code for hooking to a property manually
		new Hook(
			typeof(RoomCamera).GetMethod("get_DarkPalette", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
			typeof(PWMalfunction).GetMethod("hook_get_DarkPalette", BindingFlags.Static | BindingFlags.Public));
		new Hook(
			typeof(Room).GetMethod("get_ElectricPower", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
			typeof(PWMalfunction).GetMethod("hook_get_ElectricPower", BindingFlags.Static | BindingFlags.Public));
	}

	//Create orig functions for property gets
	public delegate float orig_DarkPalette(RoomCamera instance);
	public delegate float orig_ElectricPower(Room instance);
	//Make PWMalfunction change to the dark palette (unless something else is already making it darker)
	public static float hook_get_DarkPalette(orig_DarkPalette orig, RoomCamera instance)
	{
		float origVal = orig(instance);
		if (instance.room != null && instance.room.world != null && instance.room.world.rainCycle != null && instance.room.world.rainCycle.brokenAntiGrav != null
				&& instance.room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction) > 0f
				&& (instance.room.roomSettings.DangerType == RoomRain.DangerType.Thunder || instance.room.roomSettings.DangerType == RoomRain.DangerType.None))
		{
			return Mathf.Max(origVal, 1f - instance.room.world.rainCycle.brokenAntiGrav.CurrentLightsOn);
		}
		return origVal;
	}
	//Make PWMalfunction change electricpower
	public static float hook_get_ElectricPower(orig_ElectricPower orig, Room instance)
	{
		if (instance.world != null && instance.world.rainCycle != null && instance.world.rainCycle.brokenAntiGrav != null
				&& instance.roomSettings.GetEffectAmount(NewEffects.PWMalfunction) > 0f)
		{
			return instance.world.rainCycle.brokenAntiGrav.CurrentLightsOn;
		}
		return orig(instance);
	}


	//PWMalfunction can make a brokenAntiGravity Object, the gravity value is only changed when certain effects are active, which is good.
	// (If BrokenAntiGravity is also in this room, weirdness may ensue)
	/*public static void AntiGravity_Ctor(On.AntiGravity.orig_ctor orig, AntiGravity instance, Room room) {
		orig.Invoke(instance, room);
		if (room.roomSettings.GetEffectAmount(EnumExt_Effects.PWMalfunction) > 0f && room.world.rainCycle.brokenAntiGrav == null) {
			room.world.rainCycle.brokenAntiGrav = new AntiGravity.BrokenAntiGravity(room.game.setupValues.gravityFlickerCycleMin, room.game.setupValues.gravityFlickerCycleMax, room.game);
		}
		instance.brokenAntiGrav = room.world.rainCycle.brokenAntiGrav;
	}*/

	//brokenAntiGravtiy will shake screen & play sound with PWMalfunction
	public static void BrokenAntiGravity_Update(On.AntiGravity.BrokenAntiGravity.orig_Update orig, AntiGravity.BrokenAntiGravity instance)
	{
		//The counter is decremented (and reset at value 0) when orig is called, so check whenever the counter is 1
		bool counterFlag = (instance.counter <= 1);
		orig.Invoke(instance); //The counter changed here, but also progress is calculated
		if (counterFlag)
		{
			for (int i = 0; i < instance.game.cameras.Length; i++)
			{
				if (instance.game.cameras[i].room != null && instance.game.cameras[i].room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction) > 0f)
				{
					instance.game.cameras[i].room.PlaySound((!instance.on) ? SoundID.Broken_Anti_Gravity_Switch_Off : SoundID.Broken_Anti_Gravity_Switch_On, 0f, instance.game.cameras[i].room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction), 1f);

				}
			}
		}
		if (instance.progress > 0f && instance.progress < 1f)
		{
			for (int j = 0; j < instance.game.cameras.Length; j++)
			{
				if (instance.game.cameras[j].room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction) > 0f)
				{
					instance.game.cameras[j].room.ScreenMovement(null, new Vector2(0f, 0f), instance.game.cameras[j].room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction) * 0.5f * Mathf.Sin(instance.progress * 3.14159274f));
				}
			}
		}
	}

	//Zap coils will make the correct sound and turn off with PWMalfunction
	public static void ZapCoil_Update(On.ZapCoil.orig_Update orig, ZapCoil instance, bool eu)
	{
		orig.Invoke(instance, eu);
		//makes the zapcoil sound
		float val = 0f;
		if (instance.room.fullyLoaded && (val = instance.room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction)) > 0f)
		{
			instance.soundLoop.Volume = instance.turnedOn * val;
		}
		//makes the zapcoil switch on/off
		if (instance.room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction) > 0f && instance.room.world.rainCycle.brokenAntiGrav != null)
		{
			bool flag = instance.room.world.rainCycle.brokenAntiGrav.to == 1f && instance.room.world.rainCycle.brokenAntiGrav.progress == 1f;
			if (!flag)
			{
				instance.disruption = 1f;
				if (instance.powered && RNG.value < 0.2f)
				{
					instance.powered = false;
				}
			}
			if (flag && !instance.powered && RNG.value < 0.025f)
			{
				instance.powered = true;
			}
		}
	}

	//Makes the gravityDisruptor turn on/off with PWMalfunction
	public static void GravityDisruptor_Update(On.GravityDisruptor.orig_Update orig, GravityDisruptor instance, bool eu)
	{
		orig.Invoke(instance, eu);
		if (instance.room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction) > 0f && instance.room.world.rainCycle.brokenAntiGrav != null)
		{
			instance.power = instance.room.world.rainCycle.brokenAntiGrav.CurrentAntiGravity;
		}
	}

	//Makes redlights work with PWMalfunction
	public static void Redlight_Ctor(On.Redlight.orig_ctor_Room_PlacedObject_LightFixtureData orig, Redlight instance, Room placedInRoom, PlacedObject placedObject, PlacedObject.LightFixtureData lightData)
	{
		orig.Invoke(instance, placedInRoom, placedObject, lightData);
		float val = 0f;
		if ((val = placedInRoom.roomSettings.GetEffectAmount(NewEffects.PWMalfunction)) > 0f)
		{
			instance.gravityDependent = (val > 0f && (float)lightData.randomSeed > 0f);
		}
	}

	//Makes SuperStructureFuses be gravity dependent with PWMalfunction
	public static void SuperStructureFuses_Ctor(On.SuperStructureFuses.orig_ctor orig, SuperStructureFuses instance, PlacedObject placedObject, RWCustom.IntRect rect, Room room)
	{
		//Check to see whenever calling orig is a good idea for this...
		//if (room.world.region != null) { 
		//    orig.Invoke(instance, placedObject, rect, room);
		//}
		bool val = (room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction) > 0f);
		if (val)
		{
			instance.gravityDependent = val;
		}

	}

	//Makes SuperstructureFuses work with PWMalfunction (turns antigravity on for only as long as the function is called)
	public static void SuperStructureFuses_Update(On.SuperStructureFuses.orig_Update orig, SuperStructureFuses instance, bool eu)
	{
		float temp = instance.room.gravity;
		if (instance.room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction) > 0f)
		{
			instance.room.gravity = 1 - instance.room.ElectricPower;
		}
		orig.Invoke(instance, eu);
		instance.room.gravity = temp;
	}

	//Allows a new antigravity object to be made when switching between gates since switching worlds deletes the original one. (This is not smooth as of yet)
	public static void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate instance, bool eu)
	{
		if (instance.room.world is null) goto ORIG_;
		if (instance.room.roomSettings.GetEffectAmount(NewEffects.PWMalfunction) > 0)
		{
			if (instance.startCounter > 0 && instance.rainCycle.brokenAntiGrav != null && (!instance.rainCycle.brokenAntiGrav.on))
			{
				instance.rainCycle.brokenAntiGrav.counter = 1;
			}
		}
		else if (instance.mode == RegionGate.Mode.OpeningMiddle)
		{
			if (instance.room.world.rainCycle.brokenAntiGrav != null)
			{
				instance.room.world.rainCycle.brokenAntiGrav = new AntiGravity.BrokenAntiGravity(instance.room.game.setupValues.gravityFlickerCycleMin, instance.room.game.setupValues.gravityFlickerCycleMax, instance.room.game);
			}
		}
	ORIG_:;
		orig.Invoke(instance, eu);
	}

	//Prevents the ZapcoilLight from being in the wrong state when you move into a room too quickly. (DOESNT WORK CONSISTENTLY)
	//This bug happens because you enter the room before it is ready loading the AI? Update take a long time to be called.
	/*
	public static void ZapcoilLight_Ctor(On.ZapCoilLight.orig_ctor orig, ZapCoilLight instance, Room placedInRoom, PlacedObject placedObject, PlacedObject.LightFixtureData lightData) {
		orig.Invoke(instance, placedInRoom, placedObject, lightData);
		if (placedInRoom != null) {
			instance.lightSource.setAlpha = new float?(placedInRoom.ElectricPower);
		}
	} */

}


