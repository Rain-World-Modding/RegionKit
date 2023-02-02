
namespace RegionKit.Modules.ShelterBehaviors;
///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Shelter Behaviors")]
public static class _Module
{
	//public const string breakVer = "1.0";
	internal static bool __enabledOnce = false;

	/// <summary>
	/// Makes creatures <see cref="ShelterBehaviorManager.CycleSpawnPosition"/> on <see cref="AbstractCreature.RealizeInRoom"/>
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="instance"></param>
	public static void CreatureShuffleHook(On.AbstractCreature.orig_RealizeInRoom orig, AbstractCreature instance)
	{
		var mngr = instance.Room.realizedRoom?.updateList?.FirstOrDefault(x => x is ShelterBehaviorManager) as ShelterBehaviorManager;
		mngr?.CycleSpawnPosition();
		orig(instance);

	}
	internal static void Enable()
	{
		// Hooking code goose hre
		On.AbstractCreature.RealizeInRoom += CreatureShuffleHook;

		//ApplyHooks();
		if (!__enabledOnce)
		{
			RegisterFullyManagedObjectType(new ManagedField[]{
				new BooleanField("nvd", true, displayName:"No Vanilla Door"),
				new BooleanField("htt", false, displayName:"Hold To Trigger"),
				new IntegerField("htts", 1, 10, 4, displayName:"HTT Trigger Speed"),
				new BooleanField("cs", false, displayName:"Consumable Shelter"),
				new IntegerField("csmin", -1, 30, 3, displayName:"Consum. Cooldown Min"),
				new IntegerField("csmax", 0, 30, 6, displayName:"Consum. Cooldown Max"),
				new IntegerField("ftt", 0, 400, 20, ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Trigger"),
				new IntegerField("fts", 0, 400, 40, ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Sleep"),
				new IntegerField("ftsv", 0, 400, 60, ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Starvation"),
				new IntegerField("ftw", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Win"),
				new IntegerField("ini", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName:"Initial wait"),
				new IntegerField("ouf", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName:"Open up anim"),
				new BooleanField("ani", false, displayName:"Animate Water"),

				}, typeof(ShelterBehaviorManager), nameof(_Enums.ShelterBhvrManager), RK_POM_CATEGORY);
			RegisterFullyManagedObjectType(new ManagedField[]{
                //new BooleanField("httt", false, displayName: "HTT Tutorial"),
                new IntegerField("htttcd", -1, 12, 6, displayName: "HTT Tut. Cooldown"), }
				, typeof(ShelterBehaviorManager.HoldToTriggerTutorialObject), nameof(_Enums.ShelterBhvrHTTTutorial), RK_POM_CATEGORY);

			//RegisterEmptyObjectType("ShelterBhvrPlacedDoor", typeof()) TODO directional data and rep;
			RegisterFullyManagedObjectType(new ManagedField[]{
				new IntVector2Field("dir", new RWCustom.IntVector2(0,1), IntVector2Field.IntVectorReprType.fourdir), }
			, null!, nameof(_Enums.ShelterBhvrPlacedDoor), RK_POM_CATEGORY);

			RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrTriggerZone), RK_POM_CATEGORY, typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
			RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrNoTriggerZone), RK_POM_CATEGORY, typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
			RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrSpawnPosition), RK_POM_CATEGORY, null!, null!); // No data required :)
		}
		else
		{

		}
		__enabledOnce = true;
	}

	internal static void Disable()
	{
		On.AbstractCreature.RealizeInRoom -= CreatureShuffleHook;
	}
}
