using RegionKit.Modules.Atmo.API;
using RegionKit.Modules.Atmo.Data;
using RegionKit.Modules.Atmo.Helpers;
using static RegionKit.Modules.Atmo.API.Backing;
using static RegionKit.Modules.Atmo.Atmod;
using LOG = BepInEx.Logging;
using DBG = System.Diagnostics;
using RFL = System.Reflection;
using TXT = System.Text.RegularExpressions;
using RND = UnityEngine.Random;
using UAD = UpdatableAndDeletable;
//the above were previously global usings

using RegionKit.Modules.Atmo.Body;
using RegionKit.Modules.Atmo.Gen;
using THR = System.Threading;
//using VREG = Atmo.Helpers.VarRegistry;


namespace RegionKit.Modules.Atmo;

/// <summary>
/// Atmo is a scripting layer mod targeted at region makers. This is the main plugin class.
/// <para>
/// To interact with the mod, see <seealso cref="Atmo.API"/> namespace. For internal details, see <seealso cref="Atmo.Gen"/> and <seealso cref="Atmo.Body"/> namespace contents.
/// </para>
/// </summary>
[RegionKitModule(nameof(OnEnable), nameof(OnDisable), moduleName: "Atmo")]
public static class Atmod
{

	/// <summary>
	/// Mod version
	/// </summary>
	public const string Ver = "0.9";

	private static bool _setupRan = false;
	private static bool _dying = false;
	/// <summary>
	/// Currently active <see cref="HappenSet"/>. Null if not in session, if in arena session, or if failed to read from session.
	/// </summary>
	public static HappenSet? CurrentSet { get; private set; }
	/// <summary>
	/// Applies hooks and sets <see cref="inst"/>.
	/// </summary>
	public static void OnEnable()
	{
		Conversion.Register();
		LogWarning($"Atmo booting... {THR.Thread.CurrentThread.ManagedThreadId}");
		try
		{
			On.AbstractRoom.Update += RunHappensAbstUpd;
			On.RainWorldGame.Update += DoBodyUpdates;
			On.Room.Update += RunHappensRealUpd;
			On.World.LoadWorld += FetchHappenSet;
			On.OverWorld.LoadFirstWorld += SetTempSSN;
			VarRegistry.__Init();
			HappenBuilding.__InitBuiltins();
			SaveVarRegistry.ApplyHooks();

		}
		catch (Exception ex)
		{
			LogFatal($"Error on enable!\n{ex}");
		}
		try
		{
			//ConsoleFace.Apply();
		}
		catch (Exception ex)
		{
			switch (ex)
			{
			case TypeLoadException or System.IO.FileNotFoundException:
				LogWarning("DevConsole not present");
				break;
			default:
				LogError($"Unexpected error on devconsole apply:" +
					$"\n{ex}");
				break;
			}
		}
	}
	/// <summary>
	/// Undoes hooks and spins up a static cleanup member cleanup procedure.
	/// </summary>
	public static void OnDisable()
	{
		_dying = true;
		try
		{
			__slugnameNotFound = new(SLUG_NOT_FOUND, true);
			//On.World.ctor -= FetchHappenSet;
			On.Room.Update -= RunHappensRealUpd;
			On.RainWorldGame.Update -= DoBodyUpdates;
			On.AbstractRoom.Update -= RunHappensAbstUpd;
			On.World.LoadWorld -= FetchHappenSet;
			On.OverWorld.LoadFirstWorld -= SetTempSSN;
			VarRegistry.__Clear();

			LOG.ManualLogSource? cleanup_logger =
				LOG.Logger.CreateLogSource("Atmo_Purge");
			DBG.Stopwatch sw = new();
			bool verbose = false;
			sw.Start();
			cleanup_logger.LogMessage("Spooling cleanup thread.\nNote: errors logged here are nonconsequential and can be ignored.");
			System.Threading.ThreadPool.QueueUserWorkItem((_) =>
			{
				static string aggregator(string x, string y)
				{
					return $"{x}\n\t{y}";
				}
				List<string> success = new();
				List<string> failure = new();
				IEnumerable<Type> types = typeof(Atmod).Assembly.GetTypesSafe(out System.Reflection.ReflectionTypeLoadException? tlex);
				foreach (Type t in types)
				{
					try
					{
						var l = t.CleanupStatic();
						success.AddRange(l.Item1);
						failure.AddRange(l.Item2);
					}
					catch (Exception ex)
					{
						cleanup_logger
						.LogError($"{t}: Unhandled Error cleaning up static fields:" +
							$"\n{ex}");
					}
				}
				sw.Stop();
				cleanup_logger.LogDebug($"Finished statics cleanup: {sw.Elapsed}.");
				if (tlex is not null)
				{
					cleanup_logger.LogWarning($"TypeLoadExceptions occured: {tlex.LoaderExceptions.Select(x => x.ToString()).Stitch(aggregator)}");
				}
				if (verbose)
				{
					cleanup_logger.LogDebug(
						$"Successfully cleared: {success.Stitch(aggregator)}");
					cleanup_logger.LogDebug(
						$"\nErrored on: {failure.Stitch(aggregator)}");
				}
			});
		}
		catch (Exception ex)
		{
			LogFatal($"Error on disable!\n{ex}");
		}
	}
	/// <summary>
	/// Cleans up set if not ingame, updates some builtin variables.
	/// </summary>
	public static void Update()
	{
		if (_dying) return;
		rainWorld ??= CRW;
		if (!_setupRan && rainWorld is not null)
		{

			_setupRan = true;
		}

		if (rainWorld is null || CurrentSet is null) return;
		if (rainWorld.processManager.currentMainLoop is RainWorldGame) return;
		if (rainWorld?.processManager.FindSubProcess<RainWorldGame>() is null)
		{
			LogDebug("No RainWorldGame in processmanager, erasing currentset");
			CurrentSet = null;
		}
	}
	#region lifecycle hooks
	/// <summary>
	/// Temporarily forces currentindex during LoadFirstWorld. Needed for <see cref="VarRegistry"/> function.
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	private static void SetTempSSN(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
	{

		__tempSlugName = self.PlayerCharacterNumber;
		LogMessage($"Setting temp SSN: {__tempSlugName}, {THR.Thread.CurrentThread.ManagedThreadId}");
		orig(self);
		__tempSlugName = null;
	}
	private static void FetchHappenSet(On.World.orig_LoadWorld orig, World self, SlugcatStats.Name slugcatNumber, List<AbstractRoom> abstractRoomsList, int[] swarmRooms, int[] shelters, int[] gates)
	{
		LogMessage($"Fetching happenset for {self.name} {THR.Thread.CurrentThread.ManagedThreadId}");
		orig(self, slugcatNumber, abstractRoomsList, swarmRooms, shelters, gates);
		if (self.singleRoomWorld) return;
		__temp_World = self;
		try
		{
			CurrentSet = HappenSet.TryCreate(self);
		}
		catch (Exception e)
		{
			LogError($"Could not create a happenset: {e}");
		}
		__temp_World = null;
	}
	/// <summary>
	/// Sends an Update call to all events for loaded world
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	private static void DoBodyUpdates(On.RainWorldGame.orig_Update orig, RainWorldGame self)
	{
		orig(self);
		if (CurrentSet is null) return;
		if (self.pauseMenu != null) return;
		foreach (Happen? ha in CurrentSet.AllHappens)
		{
			if (ha is null) continue;
			try
			{
				ha.CoreUpdate();
			}
			catch (Exception e)
			{
				LogError($"Error doing body update for {ha.name}:\n{e}");
			}
		}
	}
	/// <summary>
	/// Runs abstract world update for events in a room
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	/// <param name="timePassed"></param>
	private static void RunHappensAbstUpd(On.AbstractRoom.orig_Update orig, AbstractRoom self, int timePassed)
	{
		orig(self, timePassed);
		if (CurrentSet is null) return;
		IEnumerable<Happen>? haps = CurrentSet.GetHappensForRoom(self.name);
		foreach (Happen? ha in haps)
		{
			if (ha is null) continue;
			try
			{
				if (ha.Active)
				{
					if (!ha.InitRan) { ha.Init(self.world); ha.InitRan = true; }
					ha.AbstUpdate(self, timePassed);
				}
			}
			catch (Exception e)
			{
				LogError($"Error running event abstupdate for room {self.name}:\n{e}");
			}
		}
	}
	/// <summary>
	/// Runs realized updates for events in a room
	/// </summary>
	/// <param name="orig"></param>
	/// <param name="self"></param>
	private static void RunHappensRealUpd(On.Room.orig_Update orig, Room self)
	{
		//#warning issue: for some reason geteventsforroom always returns none on real update
		//in my infinite wisdom i set SU_S04 as test room instead of SU_C04. everything worked as intended except for my brain

		orig(self);
		//DBG.Stopwatch sw = DBG.Stopwatch.StartNew();
		if (CurrentSet is null) return;
		IEnumerable<Happen>? haps = CurrentSet.GetHappensForRoom(self.abstractRoom.name);
		foreach (Happen? ha in haps)
		{
			try
			{
				if (ha.Active)
				{
					if (!ha.InitRan) { ha.Init(self.world); ha.InitRan = true; }
					ha.RealizedUpdate(self);
				}
				else
				{
					ha.InitRan = false;
				}
			}
			catch (Exception e)
			{
				LogError($"Error running event realupdate for room {self.abstractRoom.name}:\n{e}");
			}
		}
	}
	#endregion lifecycle hooks

	public static void VerboseLog(string message)
	{
		UnityEngine.Debug.Log(message);
	}


	public static void LogFrameTime(List<TimeSpan> realup_times, TimeSpan elapsed, LinkedList<double> realup_readings, int sTORE_CYCLES)
	{
		//throw new NotImplementedException();
	}

	/// <summary>
	/// Strings that evaluate to bool.true
	/// </summary>
	public static readonly string[] trueStrings = new[] { "true", "1", "yes", };
	/// <summary>
	/// Strings that evaluate to bool.false
	/// </summary>
	public static readonly string[] falseStrings = new[] { "false", "0", "no", };
	public const string SLUG_NOT_FOUND = "ATMO_SER_NOCHAR";
	internal static SlugcatStats.Name __slugnameNotFound = null!;
	internal static SlugcatStats.Name? __tempSlugName;
	internal static World? __temp_World;
	internal static int? __CurrentSaveslot => rainWorld?.options?.saveSlot;
	internal static SlugcatStats.Name? __CurrentCharacter => rainWorld?.processManager.FindSubProcess<RainWorldGame>()?.GetStorySession?.characterStats.name ?? __tempSlugName;
}
