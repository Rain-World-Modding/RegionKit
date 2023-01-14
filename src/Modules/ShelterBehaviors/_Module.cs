
namespace RegionKit.Modules.ShelterBehaviors;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Shelter Behaviors")]
public static class _Module
{
	public const string ModID = "ShelterBehaviors";
	public const string breakVer = "1.0";

	internal static bool EnabledOnce = false;

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
	public static void Enable()
	{
		// Hooking code goose hre
		On.AbstractCreature.RealizeInRoom += CreatureShuffleHook;

		//ApplyHooks();
		if (!EnabledOnce)
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

				}, typeof(ShelterBehaviorManager), EnumNames.Manager);
			RegisterFullyManagedObjectType(new ManagedField[]{
                //new BooleanField("httt", false, displayName: "HTT Tutorial"),
                new IntegerField("htttcd", -1, 12, 6, displayName: "HTT Tut. Cooldown"), }
				, typeof(ShelterBehaviorManager.HoldToTriggerTutorialObject), EnumNames.HTTTutorial);

			//RegisterEmptyObjectType("ShelterBhvrPlacedDoor", typeof()) TODO directional data and rep;
			RegisterFullyManagedObjectType(new ManagedField[]{
				new IntVector2Field("dir", new RWCustom.IntVector2(0,1), IntVector2Field.IntVectorReprType.fourdir), }
			, null!, EnumNames.PlacedDoor);

			RegisterEmptyObjectType(EnumNames.TriggerZone, typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
			RegisterEmptyObjectType(EnumNames.NoTriggerZone, typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
			RegisterEmptyObjectType(EnumNames.SpawnPosition, null!, null!); // No data required :)
		}
		else{
			
		}
	}

	public static void Disable()
	{
		On.AbstractCreature.RealizeInRoom -= CreatureShuffleHook;
	}

	public static class EnumNames
	{
		public const string Manager = "ShelterBhvrManager";
		public const string PlacedDoor = "ShelterBhvrPlacedDoor";
		public const string TriggerZone = "ShelterBhvrTriggerZone";
		public const string NoTriggerZone = "ShelterBhvrNoTriggerZone";
		public const string HTTTutorial = "ShelterBhvrHTTTutorial";
		public const string SpawnPosition = "ShelterBhvrSpawnPosition";
	}
}
