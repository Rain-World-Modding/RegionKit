using DevInterface;
using RWCustom;
using UnityEngine;

namespace RegionKit.Modules;

public static class Enums_Objects
{
	public static PlacedObject.Type PWLightrod = new(nameof(PWLightrod), true);
}

internal static class Enums_Effects
{
	public static RoomSettings.RoomEffect.Type PWMalfunction = new(nameof(PWMalfunction), true);
	public static RoomSettings.RoomEffect.Type FogOfWarSolid = new(nameof(FogOfWarSolid), true);
	public static RoomSettings.RoomEffect.Type FogOfWarDarkened = new(nameof(FogOfWarDarkened), true);
	public static RoomSettings.RoomEffect.Type CloudAdjustment = new(nameof(CloudAdjustment), true);

}
[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "RoomLoader")]
static class _RoomLoader
{
	public static void Enable()
	{
		_CommonHooks.PostRoomLoad += RoomPostLoad;
		On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData;
		On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep;
		On.DevInterface.RoomSettingsPage.Refresh += RoomSettingsPage_Refresh;
	}

	private static void RoomSettingsPage_Refresh(On.DevInterface.RoomSettingsPage.orig_Refresh orig, RoomSettingsPage self)
	{
		orig(self);
#if false
		Effects.FogOfWar.Refresh(self.owner.room);
#endif
	}

	public static void Disable()
	{
		_CommonHooks.PostRoomLoad -= RoomPostLoad;
		On.PlacedObject.GenerateEmptyData -= PlacedObject_GenerateEmptyData;
		On.DevInterface.ObjectsPage.CreateObjRep -= ObjectsPage_CreateObjRep;
		On.DevInterface.RoomSettingsPage.Refresh -= RoomSettingsPage_Refresh;
	}

	public static void ObjectsPage_CreateObjRep(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
	{
		if (tp == Enums_Objects.PWLightrod)
		{
			bool isNewObject = false;
			if (pObj == null)
			{
				isNewObject = true;
				pObj = new PlacedObject(tp, null)
				{
					pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + Custom.DegToVec(RNG.value * 360f) * 0.2f
				};
				self.RoomSettings.placedObjects.Add(pObj);
				self.owner.room.AddObject(new Objects.PWLightRod(pObj, self.owner.room));
			}
			PlacedObjectRepresentation rep = new Objects.PWLightRodRepresentation(self.owner, tp.ToString() + "_Rep", self, pObj, tp.ToString(), isNewObject);
			self.tempNodes.Add(rep);
			self.subNodes.Add(rep);
		}
		else
		{
			orig(self, tp, pObj);
		}
	}

	private static void PlacedObject_GenerateEmptyData(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
	{
		orig(self);
		if (self.type == Enums_Objects.PWLightrod)
		{
			self.data = new Objects.PWLightRodData(self);
		}
	}

	private static void RoomPostLoad(Room self)
	{
		if (self.game == null) { return; }

#if false
		Modules.Effects.FogOfWar.Refresh(self);
#endif

		//Load all the effects
		for (int k = 0; k < self.roomSettings.effects.Count; k++)
		{
			var effect = self.roomSettings.effects[k];

			if (effect.type == Enums_Effects.PWMalfunction && self.world.rainCycle.brokenAntiGrav == null)
			{
				//Directly adds a brokenAntiGraivty to the world
				self.world.rainCycle.brokenAntiGrav = new AntiGravity.BrokenAntiGravity(self.game.setupValues.gravityFlickerCycleMin, self.game.setupValues.gravityFlickerCycleMax, self.game);
			}
		}

		//Load Objects
		for (int l = 0; l < self.roomSettings.placedObjects.Count; ++l)
		{
			var obj = self.roomSettings.placedObjects[l];
			if (obj.active)
			{
				if (obj.type == Enums_Objects.PWLightrod)
				{
					self.AddObject(new Objects.PWLightRod(obj, self));
				}
			}
		}
	}
}
