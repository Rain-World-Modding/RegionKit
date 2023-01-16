using DevInterface;
using RWCustom;
using UnityEngine;

namespace RegionKit.Modules.Objects
{
	//todo: apply
    //Made By LeeMoriya
    public static class NewObjects
    {
        public static void Hook()
        {
            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData;
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep;
            On.Room.Loaded += Room_Loaded;
            /// Unused
            //On.RoomCamera.ctor += RoomCamera_ctor;
        }

        /*private static void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
            orig.Invoke(self, game, cameraNumber);
        }*/

        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            if (self.game == null)
            {
                return;
            }
            for (int m = 0; m < self.roomSettings.placedObjects.Count; m++)
            {
                if (self.roomSettings.placedObjects[m].active)
                {
                    if (self.roomSettings.placedObjects[m].type == Enums_ARKillRect.ARKillRect)
                    {
                        self.AddObject(new ARKillRect(self, self.roomSettings.placedObjects[m]));
                    }
                    if (self.roomSettings.placedObjects[m].type == Enums_RainbowNoFade.RainbowNoFade)
                    {
                        self.AddObject(new RainbowNoFade(self, self.roomSettings.placedObjects[m]));
                    }
                }
            }
            orig.Invoke(self);
        }

        private static void ObjectsPage_CreateObjRep(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
        {
            if (tp == Enums_ARKillRect.ARKillRect)
            {
                if (pObj == null)
                {
                    pObj = new PlacedObject(tp, null);
                    pObj.pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + Custom.DegToVec(RNG.value * 360f) * 0.2f;
                    self.RoomSettings.placedObjects.Add(pObj);
                }
                PlacedObjectRepresentation placedObjectRepresentation;
                placedObjectRepresentation = new GridRectObjectRepresentation(self.owner, "ARKillRect" + "_Rep", self, pObj, tp.ToString());
                if (placedObjectRepresentation != null)
                {
                    self.tempNodes.Add(placedObjectRepresentation);
                    self.subNodes.Add(placedObjectRepresentation);
                }
                return;
            }
            if (tp == Enums_RainbowNoFade.RainbowNoFade)
            {
                if (pObj == null)
                {
                    pObj = new PlacedObject(tp, null);
                    pObj.pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + Custom.DegToVec(RNG.value * 360f) * 0.2f;
                    self.RoomSettings.placedObjects.Add(pObj);
                }
                PlacedObjectRepresentation placedObjectRepresentation;
                placedObjectRepresentation = new RainbowNoFadeRepresentation(self.owner, "RainbowNoFade" + "_Rep", self, pObj);
                if (placedObjectRepresentation != null)
                {
                    self.tempNodes.Add(placedObjectRepresentation);
                    self.subNodes.Add(placedObjectRepresentation);
                }
                return;
            }
            orig.Invoke(self, tp, pObj);
        }

        private static void PlacedObject_GenerateEmptyData(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
        {
            if (self.type == Enums_ARKillRect.ARKillRect)
            {
                self.data = new PlacedObject.GridRectObjectData(self);
                return;
            }
            if (self.type == Enums_RainbowNoFade.RainbowNoFade)
            {
                self.data = new RainbowNoFade.RainbowNoFadeData(self);
                return;
            }
            orig.Invoke(self);
        }
    }
}
