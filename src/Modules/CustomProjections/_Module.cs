namespace RegionKit.Modules.CustomProjections;


[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "CustomProjections")]
public static class _Module
{
	public const string OVERSEER_POM_CATEGORY = Objects._Module.OBJECTS_POM_CATEGORY;
	internal static void Setup()
	{
		RegisterManagedObject<ReliableIggyEntrance, ReliableEntranceData, ReliableEntranceRep>("ReliableIggyEntrance", OVERSEER_POM_CATEGORY);
		RegisterManagedObject<CustomDoorPointer, DoorPointerData, DoorPointerRep>("CustomIggyDirection", OVERSEER_POM_CATEGORY);
	}

	internal static void Enable()
	{
		CustomProjections.Apply();
		ReliableIggyEntrance.Apply();
		CustomDoorPointer.Apply();
		OverseerProperties.Apply();
		PointerHooks.Apply();
		OverseerRecolor.Apply();
		On.ImageTrigger.AttemptTriggerFire += ImageTrigger_AttemptTriggerFire;
		LoadShaders();
	}

#pragma warning disable CS0162 // Unreachable code detected
	private static void ImageTrigger_AttemptTriggerFire(On.ImageTrigger.orig_AttemptTriggerFire orig, RainWorldGame game, Room room, ActiveTriggerChecker triggerChecker, ShowProjectedImageEvent imgEvent)
	{
		const bool DO_LOGGING = false;
		orig(game, room, triggerChecker, imgEvent);

		//shadows the logic of orig for debugging
		if (DO_LOGGING) LogInfo("attempting to fire an image trigger");
		if (game.session is StoryGameSession session)
		{
			if (imgEvent.afterEncounter != session.saveState.miscWorldSaveData.SLOracleState.playerEncounters > 0)
			{
				if (DO_LOGGING)
				{
					if (imgEvent.afterEncounter) LogInfo("trigger canceled due to afterEncounter without having met moon");
					else LogInfo("trigger canceled due to beforeEncounter while having met moon");
				}
				return;
			}
			if (session.saveState.miscWorldSaveData.playerGuideState.HasImageBeenShownInRoom(room.abstractRoom.name))
			{
				if (DO_LOGGING) LogInfo("trigger canceled due to having shown image in room");
				return;
			}
		}
		Overseer overseer = null!;
		for (int i = 0; i < room.abstractRoom.creatures.Count; i++)
		{
			if (room.abstractRoom.creatures[i].creatureTemplate.type == CreatureTemplate.Type.Overseer && (room.abstractRoom.creatures[i].abstractAI as OverseerAbstractAI)!.playerGuide && room.abstractRoom.creatures[i].realizedCreature != null)
			{
				overseer = (room.abstractRoom.creatures[i].realizedCreature as Overseer)!;
				break;
			}
		}
		if (overseer == null || overseer.AI == null || overseer.AI.communication == null)
		{
			if (DO_LOGGING) LogInfo("trigger canceled due to not finding overseer");
			return;
		}
		if (imgEvent.onlyWhenShowingDirection)
		{
			Player player;
			if (ModManager.CoopAvailable)
			{
				AbstractCreature firstAlivePlayer = room.game.FirstAlivePlayer;
				if (game.AlivePlayers.Count == 0 || firstAlivePlayer == null || firstAlivePlayer.realizedCreature == null)
				{
					if (DO_LOGGING) LogInfo("trigger canceled due to not finding firstAlivePlayer in coop");
					return;
				}
				player = (firstAlivePlayer.realizedCreature as Player)!;
			}
			else
			{
				if (game.Players.Count == 0 || game.Players[0].realizedCreature == null)
				{
					if (DO_LOGGING) LogInfo("trigger canceled due to not finding player");
					return;
				}
				player = (game.Players[0].realizedCreature as Player)!;
			}
			if (!overseer.AI.communication.AnyProgressionDirection(player))
			{
				if (DO_LOGGING) LogInfo("trigger canceled due to onlyProgDir. while there's no progDir.");
				return;
			}
		}
		if (DO_LOGGING) LogInfo("trigger is good!");
	}
#pragma warning restore CS0162 // Unreachable code detected

	internal static void Disable()
	{
		CustomProjections.Undo();
		ReliableIggyEntrance.Undo();
		CustomDoorPointer.Undo();
		OverseerProperties.Undo();
		PointerHooks.Undo();
		OverseerRecolor.Undo();
	}

	public static void LoadShaders()
	{
		rainWorld.Shaders["HKHoloGrid"] = FShader.CreateShader("HKHoloGrid", AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/hkhologrid")).LoadAsset<Shader>("Assets/HKHoloGrid.shader"));
	}
}
