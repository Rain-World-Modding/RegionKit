using DevInterface;
using EffExt;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.Insects
{
	[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Insects")]
	public static class _Module
	{
		internal static void Setup()
		{
		}

		internal static void Enable()
		{
			On.InsectCoordinator.RoomEffectToInsectType += InsectCoordinator_RoomEffectToInsectType;
			On.InsectCoordinator.CreateInsect += InsectCoordinator_CreateInsect;
			On.InsectCoordinator.TileLegalForInsect += InsectCoordinator_TileLegalForInsect;
			On.InsectCoordinator.EffectSpawnChanceForInsect += InsectCoordinator_EffectSpawnChanceForInsect;
			On.InsectCoordinator.SpeciesDensity_Type_1 += InsectCoordinator_SpeciesDensity_Type_1;
			On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType += RoomSettingsPageDevEffectGetCategoryFromEffectType;
			_CommonHooks.PostRoomLoad += PostRoomLoad;
		}

		internal static void Disable()
		{
			On.InsectCoordinator.CreateInsect -= InsectCoordinator_CreateInsect;
			On.InsectCoordinator.TileLegalForInsect -= InsectCoordinator_TileLegalForInsect;
			On.InsectCoordinator.EffectSpawnChanceForInsect -= InsectCoordinator_EffectSpawnChanceForInsect;
			On.InsectCoordinator.RoomEffectToInsectType -= InsectCoordinator_RoomEffectToInsectType;
			On.InsectCoordinator.SpeciesDensity_Type_1 -= InsectCoordinator_SpeciesDensity_Type_1;
			On.DevInterface.RoomSettingsPage.DevEffectGetCategoryFromEffectType -= RoomSettingsPageDevEffectGetCategoryFromEffectType;
			_CommonHooks.PostRoomLoad -= PostRoomLoad;
		}

		private static CosmeticInsect.Type InsectCoordinator_RoomEffectToInsectType(On.InsectCoordinator.orig_RoomEffectToInsectType orig, RoomSettings.RoomEffect.Type type)
		{
			if (type == _Enums.ButterfliesA)
				return _Enums.ButterflyA;
			else if (type == _Enums.ButterfliesB)
				return _Enums.ButterflyB;
			else if (type == _Enums.ColoredCamoBeetles)
				return _Enums.ColoredCamoBeetle;
			else if (type == _Enums.GlowingSwimmers)
				return _Enums.GlowingSwimmerInsect;
			else if (type == _Enums.MosquitoInsects)
				return _Enums.MosquitoInsect;
			else if (type == _Enums.Seedlings)
				return _Enums.Seedling;
			else if (type == _Enums.Zippers)
				return _Enums.Zipper;
			else
				return orig(type);
		}

		private static void InsectCoordinator_CreateInsect(On.InsectCoordinator.orig_CreateInsect orig, InsectCoordinator self, CosmeticInsect.Type type, Vector2 pos, InsectCoordinator.Swarm swarm)
		{
			if (type.IsRegionKitInsect())
			{
				// Initial removal checks
				if (!InsectCoordinator.TileLegalForInsect(type, self.room, pos))
				{
					return;
				}
				if (self.room.world.rainCycle.TimeUntilRain < Random.Range(1200, 1600))
				{
					return;
				}

				// Try to create insect
				CosmeticInsect? insect = null;
				if (type == _Enums.ButterflyA || type == _Enums.ButterflyB)
				{
					insect = new Butterfly(self.room, pos, type == _Enums.ButterflyA);
				}
				else if (type == _Enums.ColoredCamoBeetle)
				{
					Room rm = self.room;
					if (!rm.readyForAI || rm.aimap.getTerrainProximity(pos) < 5)
					{
						for (var j = 0; j < 5; j++)
						{
							Vector2? vector2 = SharedPhysics.ExactTerrainRayTracePos(rm, pos, pos + RNV() * 100f);
							if (vector2.HasValue)
							{
								pos = vector2.Value;
								break;
							}
						}
					}

					insect = new ColoredCamoBeetleInsect(rm, pos);
				}
				else if (type == _Enums.GlowingSwimmerInsect)
				{
					insect = new GlowingSwimmer(self.room, pos);
				}
				else if (type == _Enums.MosquitoInsect)
				{
					insect = new MosquitoInsect(self.room, pos);
				}
				else if (type == _Enums.Zipper)
				{
					insect = new Zipper(self.room, pos);
				}

				// Add insect to room
				if (insect != null)
				{
					self.allInsects.Add(insect);
					if (swarm != null)
					{
						swarm.members.Add(insect);
						insect.mySwarm = swarm;
					}
					self.room.AddObject(insect);
				}
			}

			// Call orig
			orig(self, type, pos, swarm);
		}

		private static bool InsectCoordinator_TileLegalForInsect(On.InsectCoordinator.orig_TileLegalForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos)
		{
			// No water and no narrow spaces
			if (type == _Enums.ButterflyA || type == _Enums.ButterflyB)
			{
				return !room.GetTile(testPos).AnyWater && !room.readyForAI || !room.aimap.getAItile(testPos).narrowSpace;
			}
			// No water
			if (type == _Enums.ColoredCamoBeetle || type == _Enums.Zipper)
			{
				return !room.GetTile(testPos).AnyWater;
			}
			// Deep water only
			if (type == _Enums.GlowingSwimmerInsect)
			{
				return room.GetTile(testPos).DeepWater;
			}
			// No deep water
			if (type == _Enums.MosquitoInsect)
			{
				return !room.GetTile(testPos).DeepWater;
			}

			return orig(type, room, testPos);
		}

		private static bool InsectCoordinator_EffectSpawnChanceForInsect(On.InsectCoordinator.orig_EffectSpawnChanceForInsect orig, CosmeticInsect.Type type, Room room, Vector2 testPos, float effectAmount)
		{
			if (type == _Enums.ButterflyA || type == _Enums.ButterflyB || type == _Enums.Zipper)
			{
				return Mathf.Pow(Random.value, 1f - effectAmount) > (room.readyForAI ? room.aimap.getTerrainProximity(testPos) : 5) * 0.05f;
			}
			if (type == _Enums.ColoredCamoBeetle)
			{
				return !room.readyForAI || !room.aimap.getAItile(testPos).narrowSpace;
			}
			if (type == _Enums.GlowingSwimmerInsect || type == _Enums.MosquitoInsect)
			{
				return true;
			}
			return orig(type, room, testPos, effectAmount);
		}

		private static float InsectCoordinator_SpeciesDensity_Type_1(On.InsectCoordinator.orig_SpeciesDensity_Type_1 orig, CosmeticInsect.Type type)
		{
			if (type == _Enums.GlowingSwimmerInsect)
			{
				return 0.8f;
			}
			if (type == _Enums.MosquitoInsect)
			{
				return 1.5f;
			}
			return orig(type);
		}

		private static RoomSettingsPage.DevEffectsCategories RoomSettingsPageDevEffectGetCategoryFromEffectType(On.DevInterface.RoomSettingsPage.orig_DevEffectGetCategoryFromEffectType orig, RoomSettingsPage self, RoomSettings.RoomEffect.Type type)
		{
			RoomSettingsPage.DevEffectsCategories res = orig(self, type);
			if (type.IsRegionKitInsect())
				res = _Enums.RegionKitInsects;
			return res;
		}

		private static void PostRoomLoad(Room self)
		{
			for (int i = 0; i < self.roomSettings.effects.Count; i++)
			{
				if (self.roomSettings.effects[i].type.IsRegionKitInsect())
				{
					if (self.insectCoordinator == null)
					{
						self.insectCoordinator = new InsectCoordinator(self);
						self.AddObject(self.insectCoordinator);
					}
					self.insectCoordinator.AddEffect(self.roomSettings.effects[i]);
				}
			}
		}
	}
}
