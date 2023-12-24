using MonoMod.RuntimeDetour;
using DevInterface;
using static Pom.Pom;

namespace RegionKit.Modules.Objects;
///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "MiscObjects")]
public static class _Module
{
	public const string OBJECTS_POM_CATEGORY = RK_POM_CATEGORY + "-MISCOBJECTS";
	public const string GAMEPLAY_POM_CATEGORY = RK_POM_CATEGORY + "-Gameplay";
	public const string DECORATIONS_POM_CATEGORY = RK_POM_CATEGORY + "-Decorations";
	private static List<Hook> __objectHooks = new();
	internal static void Setup()
	{
		//NewEffects/
		//NewObjects.Hook();
		RegisterFullyManagedObjectType(ColouredLightSource.__fields, typeof(ColouredLightSource), null, DECORATIONS_POM_CATEGORY);
		RegisterFullyManagedObjectType(Drawable.__fields, typeof(Drawable), "FreeformDecalOrSprite", DECORATIONS_POM_CATEGORY);
		List<ManagedField> shroudFields = new()
		{
			new Vector2ArrayField("quad", 4, true, Vector2ArrayField.Vector2ArrayRepresentationType.Polygon, Vector2.zero, Vector2.right * 20f, (Vector2.right + Vector2.up) * 20f, Vector2.up * 20f)
		};
		RegisterFullyManagedObjectType(shroudFields.ToArray(), typeof(Shroud), nameof(Shroud), DECORATIONS_POM_CATEGORY);

		List<ManagedField> fanFields = new()
		{
			new FloatField("speed", 0f, 1f, 0.6f,0.01f, ManagedFieldWithPanel.ControlType.slider, "Speed"),
			new FloatField("scale", 0f, 1f, 0.3f,0.01f, ManagedFieldWithPanel.ControlType.slider, "Scale"),
			new FloatField("depth", 0f, 1f, 0.3f,0.01f, ManagedFieldWithPanel.ControlType.slider, "Depth")
		};
		RegisterFullyManagedObjectType(fanFields.ToArray(), typeof(SpinningFan), nameof(SpinningFan), DECORATIONS_POM_CATEGORY);

		List<ManagedField> steamFields = new()
		{
			new FloatField("f1", 0f, 1f, 0.5f,0.01f, ManagedFieldWithPanel.ControlType.slider, "Duration"),
			new FloatField("f2", 0f,1f,0.5f,0.01f, ManagedFieldWithPanel.ControlType.slider, "Frequency"),
			new FloatField("f3", 0f,1f,0.5f,0.01f, ManagedFieldWithPanel.ControlType.slider, "Lifetime"),
			new Vector2Field("v1", new Vector2(0f,45f), Vector2Field.VectorReprType.line)
		};
		RegisterFullyManagedObjectType(steamFields.ToArray(), typeof(SteamHazard), nameof(SteamHazard), GAMEPLAY_POM_CATEGORY);


		RegisterManagedObject<RoomBorderTeleport, BorderTpData, ManagedRepresentation>("RoomBorderTP", GAMEPLAY_POM_CATEGORY);
		RegisterEmptyObjectType<WormgrassRectData, ManagedRepresentation>("WormgrassRect", GAMEPLAY_POM_CATEGORY);
		RegisterManagedObject<PlacedWaterFall, PlacedWaterfallData, ManagedRepresentation>("PlacedWaterfall", DECORATIONS_POM_CATEGORY);
		RegisterManagedObject<ColorifierUAD, ShortcutColorifierData, ManagedRepresentation>("ShortcutColor", DECORATIONS_POM_CATEGORY);

		__objectHooks = new List<Hook>
		{
			//new Hook(typeof(Room).GetMethodAllContexts(nameof(Room.Loaded)), typeof(_Module).GetMethodAllContexts(nameof(Room_Loaded))),
			//new Hook(typeof(GHalo).GetMethodAllContexts("get_Speed"), _mt.GetMethodAllContexts(nameof(halo_speed)))
		};
		WaterSpoutObjRep.Register();
		PopupsMod.Register();

		RegisterManagedObject<ShortcutCannon, shortcutCannonData, ShortcutCannonRepresentation>("ShortcutCannon", GAMEPLAY_POM_CATEGORY);
		RegisterManagedObject<CameraNoise, CameraNoise.CameraNoiseData, ManagedRepresentation>("CameraNoise", DECORATIONS_POM_CATEGORY);
        RegisterManagedObject<SlugcatEyeSelector, SlugcatEyeSelectorData, ManagedRepresentation>("SlugcatEyeSelector", DECORATIONS_POM_CATEGORY);
        RegisterFullyManagedObjectType(new ManagedField[]
			{
				new IntegerField("reqkarma", 0, 9, 0, displayName:"Req Karma"),
				new IntegerField("reqkarmacap", 0, 9, 9, displayName:"Req Karma Cap"),
				new IntegerField("setkarma", -1, 9, 9, displayName:"New Karma"),
				new IntegerField("setkarmacap", -1, 9, -1, displayName:"New Karma Cap"),
				new EnumField<BigKarmaShrine.direction>("direction", BigKarmaShrine.direction.Any, displayName:"Direction"),
				new Vector2Field("radius", Vector2.up * 134.5f, Vector2Field.VectorReprType.circle),
				new BooleanField("superslow", false, displayName:"Super Slowdown"),
				new BooleanField("sprite", true, displayName:"Use Sprite"),
			}, typeof(BigKarmaShrine), "BigKarmaShrine", GAMEPLAY_POM_CATEGORY);


		RegisterFullyManagedObjectType(new ManagedField[]
			{
				new Vector2Field("radius", Vector2.up * 134.5f, Vector2Field.VectorReprType.circle),
				new BooleanField("frame", true, displayName:"Use Frame"),
				new ColorField("topcolor", new Color(1f, 0.7f, 0.2f), displayName: "top color"),
				new ColorField("bottomcolor", new Color(0.6f, 0.46f, 0.14f), displayName: "bottom color"),
				new IntegerField("depth", 0, 30, 0, ManagedFieldWithPanel.ControlType.slider, "depth"),
				new IntegerField("spriteindex", 0, 9, 9, displayName: "karma display"),
				new StringField("spritename", "", "sprite name")
			}, typeof(BigKarmaShrine.MarkSprite), "KarmaShrineSprite", GAMEPLAY_POM_CATEGORY);
		RegisterEmptyObjectType<CustomWallMyceliaData, ManagedRepresentation>("CustomWallMycelia", RK_POM_CATEGORY);

		RegisterManagedObject<GuardProtectNode, GuardProtectData, GuardProtectRepresentation>("GuardProtectNode", GAMEPLAY_POM_CATEGORY);
	}

	internal static void Enable()
	{
		//TODO: make unapplies?
		foreach (var hk in __objectHooks) if (!hk.IsApplied) hk.Apply();
		On.PlacedObject.GenerateEmptyData += MakeEmptyData;
		On.DevInterface.ObjectsPage.CreateObjRep += CreateObjectReps;
		On.DevInterface.ObjectsPage.DevObjectGetCategoryFromPlacedType += ObjectsPageDevObjectGetCategoryFromPlacedType;
		On.Room.NowViewed += Room_Viewed;
		On.Room.NoLongerViewed += Room_NotViewed;
		CustomEntranceSymbols.Apply();
		ColoredLightBeam.Apply();
		NoWallSlideZones.Apply();
		RKAdditionalClimbables.Apply();
		//todo: check if it's okay to have like this
		_CommonHooks.PostRoomLoad += RoomPostLoad;
		//On.RainWorld.LoadResources += LoadLittlePlanetResources;
		ShortcutCannon.Apply();
		SlugcatEyeSelector.Apply();
		BigKarmaShrine.Apply();
		CustomWallMycelia.Apply();
		GuardProtectNode.Apply();
	}

	internal static void Disable()
	{
		//On.RainWorld.LoadResources -= LoadLittlePlanetResources;
		On.Room.NowViewed -= Room_Viewed;
		On.Room.NoLongerViewed -= Room_NotViewed;
		On.PlacedObject.GenerateEmptyData -= MakeEmptyData;
		On.DevInterface.ObjectsPage.CreateObjRep -= CreateObjectReps;
		On.DevInterface.ObjectsPage.DevObjectGetCategoryFromPlacedType -= ObjectsPageDevObjectGetCategoryFromPlacedType;
		_CommonHooks.PostRoomLoad -= RoomPostLoad;
		CustomEntranceSymbols.Undo();
		ColoredLightBeam.Undo();
		NoWallSlideZones.Undo();
		RKAdditionalClimbables.Undo();
		foreach (var hk in __objectHooks) if (hk.IsApplied) hk.Undo();
		ShortcutCannon.Undo();
		SlugcatEyeSelector.Undo();
		BigKarmaShrine.Undo();
		CustomWallMycelia.Undo();
		GuardProtectNode.Undo();
	}

	private static ObjectsPage.DevObjectCategories ObjectsPageDevObjectGetCategoryFromPlacedType(On.DevInterface.ObjectsPage.orig_DevObjectGetCategoryFromPlacedType orig, ObjectsPage self, PlacedObject.Type type)
	{
		ObjectsPage.DevObjectCategories res = orig(self, type);
		if (type == _Enums.ProjectedCircle ||
			type == _Enums.ColoredLightBeam ||
			type == _Enums.CustomEntranceSymbol ||
			type == _Enums.PWLightrod ||
			type == _Enums.UpsideDownWaterFall ||
			type == _Enums.LittlePlanet ||
			type == _Enums.RainbowNoFade)
			res = new ObjectsPage.DevObjectCategories(DECORATIONS_POM_CATEGORY);
		else if (
			type == _Enums.NoWallSlideZone ||
			type == _Enums.ARKillRect ||
			type == _Enums.ClimbablePole ||
			type == _Enums.ClimbableWire ||
			type == EchoExtender._Enums.EEGhostSpot ||
			type == TheMast._Enums.PlacedWind ||
			type == TheMast._Enums.PlacedPearlChain)
			res = new ObjectsPage.DevObjectCategories(GAMEPLAY_POM_CATEGORY);
		return res;
	}

	private static void RoomPostLoad(Room self)
	{
		bool wormgrassDataFound = false;
		if (self.game == null)
		{
			return;
		}
		for (int m = 0; m < self.roomSettings.placedObjects.Count; m++)
		{
			if (!self.roomSettings.placedObjects[m].active)
			{
				continue;
			}
			PlacedObject pObj = self.roomSettings.placedObjects[m];
			switch (pObj.type.value)
			{
			case nameof(_Enums.ARKillRect):
				self.AddObject(new ARKillRect(self, pObj));
				break;
			case nameof(_Enums.RainbowNoFade):
				self.AddObject(new RainbowNoFade(self, pObj));
				break;
			case nameof(_Enums.LittlePlanet):
				self.AddObject(new LittlePlanet(self, pObj));
				break;
			case nameof(_Enums.NoWallSlideZone):
				self.AddObject(new NoWallSlideZone(self, pObj));
				break;
			case nameof(_Enums.ProjectedCircle):
				self.AddObject(new ProjectedCircleObject(self, pObj));
				break;
			case nameof(_Enums.UpsideDownWaterFall):
				self.AddObject(new UpsideDownWaterFallObject(self, pObj, 1f, 5f, 1f, 5f, 1f, "Water", true));
				break;
			case nameof(_Enums.ColoredLightBeam):
				var coloredLightBeam = new ColoredLightBeam(pObj);
				self.AddObject(coloredLightBeam);
				self.SetLightBeamBlink(coloredLightBeam, m);
				coloredLightBeam._baseColorMode = (pObj.data as ColoredLightBeam.ColoredLightBeamData)!.colorType is ColoredLightBeam.ColoredLightBeamData.ColorType.Environment;
				coloredLightBeam._effectColor = (int)(pObj.data as ColoredLightBeam.ColoredLightBeamData)!.colorType - 1;
				break;
			case nameof(_Enums.ClimbablePole):
				self.AddObject(new ClimbablePole(self, (ClimbJumpVineData)pObj.data));
				break;
			case nameof(_Enums.ClimbableWire):
				MoreSlugcats.ClimbableVineRenderer? climbableVineRenderer = null;
				var num13 = self.updateList.Count - 1;
				while (num13 >= 0 && climbableVineRenderer == null)
				{
					if (self.updateList[num13] is MoreSlugcats.ClimbableVineRenderer r)
						climbableVineRenderer = r;
					num13--;
				}
				if (climbableVineRenderer == null)
				{
					climbableVineRenderer = new(self);
					self.AddObject(climbableVineRenderer);
				}
				self.waitToEnterAfterFullyLoaded = Math.Max(self.waitToEnterAfterFullyLoaded, 80);
				break;
			}
			if (pObj.data is WormgrassRectData && !wormgrassDataFound)
			{
				self.AddObject(new WormgrassManager(self));
				wormgrassDataFound = true;
			}
		}
	}

	private static void CreateObjectReps(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, DevInterface.ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
	{
		if (tp == _Enums.ARKillRect)
		{
			if (pObj == null)
			{
				pObj = new PlacedObject(tp, null);
				pObj.pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.2f;
				self.RoomSettings.placedObjects.Add(pObj);
			}
			DevInterface.PlacedObjectRepresentation placedObjectRepresentation;
			placedObjectRepresentation = new DevInterface.GridRectObjectRepresentation(self.owner, "ARKillRect" + "_Rep", self, pObj, tp.ToString());
			if (placedObjectRepresentation != null)
			{
				self.tempNodes.Add(placedObjectRepresentation);
				self.subNodes.Add(placedObjectRepresentation);
			}
			return;
		}
		else if (tp == _Enums.RainbowNoFade)
		{
			if (pObj == null)
			{
				pObj = new PlacedObject(tp, null);
				pObj.pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.2f;
				self.RoomSettings.placedObjects.Add(pObj);
			}
			DevInterface.PlacedObjectRepresentation placedObjectRepresentation;
			placedObjectRepresentation = new RainbowNoFadeRepresentation(self.owner, "RainbowNoFade" + "_Rep", self, pObj);
			if (placedObjectRepresentation != null)
			{
				self.tempNodes.Add(placedObjectRepresentation);
				self.subNodes.Add(placedObjectRepresentation);
			}
			return;
		}
		else if (tp == _Enums.LittlePlanet)
		{
			if (pObj is null)
			{
				self.RoomSettings.placedObjects.Add(pObj = new(tp, null)
				{
					pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new(-683f, 384f), .25f) + Custom.DegToVec(UnityEngine.Random.value * 360f) * .2f
				});
			}
			var pObjRep = new LittlePlanetRepresentation(self.owner, "LittlePlanet_Rep", self, pObj);
			self.tempNodes.Add(pObjRep);
			self.subNodes.Add(pObjRep);
		}
		else if (tp == _Enums.NoWallSlideZone)
		{
			if (pObj is null)
			{
				self.RoomSettings.placedObjects.Add(pObj = new(tp, null)
				{
					pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new(-683f, 384f), .25f) + DegToVec(UnityEngine.Random.value * 360f) * .2f
				});
			}
			var pObjRep = new FloatRectRepresentation(self.owner, $"{tp}_Rep", self, pObj, tp.ToString());
			self.tempNodes.Add(pObjRep);
			self.subNodes.Add(pObjRep);
		}
		else if (tp == _Enums.CustomEntranceSymbol)
		{
			if (pObj is null)
			{
				self.RoomSettings.placedObjects.Add(pObj = new(tp, null)
				{
					pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new(-683f, 384f), .25f) + DegToVec(UnityEngine.Random.value * 360f) * .2f
				});
			}
			var pObjRep = new CESRepresentation(self.owner, $"{tp}_Rep", self, pObj);
			self.tempNodes.Add(pObjRep);
			self.subNodes.Add(pObjRep);
		}
		else if (tp == _Enums.UpsideDownWaterFall)
		{
			if (pObj is null)
			{
				self.RoomSettings.placedObjects.Add(pObj = new(tp, null)
				{
					pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new(-683f, 384f), .25f) + DegToVec(UnityEngine.Random.value * 360f) * .2f
				});
			}
			var pObjRep = new UpDownWFRepresentation(self.owner, $"{tp}_Rep", self, pObj, tp.ToString());
			self.tempNodes.Add(pObjRep);
			self.subNodes.Add(pObjRep);
		}
		else if (tp == _Enums.ColoredLightBeam)
		{
			if (pObj is null)
			{
				self.RoomSettings.placedObjects.Add(pObj = new(tp, null)
				{
					pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new(-683f, 384f), .25f) + DegToVec(UnityEngine.Random.value * 360f) * .2f
				});
			}
			var pObjRep = new ColoredLightBeamRepresentation(self.owner, $"{tp}_Rep", self, pObj);
			self.tempNodes.Add(pObjRep);
			self.subNodes.Add(pObjRep);
		}
		else if (tp == _Enums.ClimbablePole || tp == _Enums.ClimbableWire)
		{
			if (pObj is null)
			{
				self.RoomSettings.placedObjects.Add(pObj = new(tp, null)
				{
					pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new(-683f, 384f), .25f) + DegToVec(UnityEngine.Random.value * 360f) * .2f
				});
			}
			var pObjRep = new ClimbJumpVineRepresentation(self.owner, $"{tp}_Rep", self, pObj);
			self.tempNodes.Add(pObjRep);
			self.subNodes.Add(pObjRep);
		}
		else
			orig(self, tp, pObj);
		//orig.Invoke(self, tp, pObj); -> this will add a duplicate PlacedObjectRepresentation
	}

	private static void MakeEmptyData(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
	{
		if (self.type == _Enums.ARKillRect)
		{
			self.data = new PlacedObject.GridRectObjectData(self);
		}
		else if (self.type == _Enums.RainbowNoFade)
		{
			self.data = new RainbowNoFade.RainbowNoFadeData(self);
		}
		else if (self.type == _Enums.LittlePlanet)
		{
			self.data = new LittlePlanet.LittlePlanetData(self);
		}
		else if (self.type == _Enums.NoWallSlideZone)
		{
			self.data = new FloatRectData(self);
		}
		else if (self.type == _Enums.CustomEntranceSymbol)
		{
			self.data = new CESData(self);
		}
		else if (self.type == _Enums.UpsideDownWaterFall)
		{
			self.data = new UpDownWFData(self);
		}
		else if (self.type == _Enums.ColoredLightBeam)
		{
			self.data = new ColoredLightBeam.ColoredLightBeamData(self);
		}
		else if (self.type == _Enums.ClimbablePole)
		{
			self.data = new ClimbJumpVineData(self);
		}
		else if (self.type == _Enums.ClimbableWire)
		{
			self.data = new ClimbWireData(self);
		}
		orig.Invoke(self);
	}

	internal delegate void Room_Void_None(Room instance);
	// internal static void Room_Loaded(Room_Void_None orig, Room instance)
	// {
	// 	orig(instance);
	// }
	internal static void Room_NotViewed(On.Room.orig_NoLongerViewed orig, Room instance)
	{
		orig(instance);
		foreach (var uad in instance.updateList) if (uad is INotifyWhenRoomIsViewed tar) tar.RoomNoLongerViewed();
	}
	internal static void Room_Viewed(On.Room.orig_NowViewed orig, Room instance)
	{
		orig(instance);
		foreach (var uad in instance.updateList) if (uad is INotifyWhenRoomIsViewed tar) tar.RoomViewed();
	}

}
