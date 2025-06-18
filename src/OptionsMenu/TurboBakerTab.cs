using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;

#pragma warning disable CS0162 // Unreachable code detected
namespace RegionKit.OptionsMenu
{
	/// <summary>
	/// Vigaro's Turbo Baker ported to RegionKit by Alduris
	/// </summary>
	public class TurboBakerTab : OpTab
	{
		private const bool DEBUG = false;
		private const float RegionCheckboxHeight = 30;
		private const float ThreadLabelHeight = 20;

		public List<TaskData> Tasks = new();
		public List<OpLabel> ThreadLabels = new();
		public OpLabel? StatusLabel;
		public OpSimpleButton BakeButton = null!;
		public DateTime BakeStartTime;
		public DateTime ActualBakeStartTime;
		public bool Baking = false;

		public readonly Configurable<int> Threads;
		public readonly Configurable<bool> ForceBake;
		public readonly Configurable<bool> HiddenSlugcats;
		public readonly Dictionary<string, OpCheckBox> Regions = new();

		private int _updateTimer;

		public TurboBakerTab(OptionInterface owner) : base(owner, "Bakery")
		{
			Threads ??= owner.config.Bind(nameof(Threads), Mathf.CeilToInt(Environment.ProcessorCount * 0.5f), new ConfigAcceptableRange<int>(1, Environment.ProcessorCount));
			ForceBake ??= owner.config.Bind(nameof(ForceBake), false);
			HiddenSlugcats ??= owner.config.Bind(nameof(HiddenSlugcats), false);

			var regions = Region.GetFullRegionOrder();

			foreach (var region in regions)
			{
				Regions[region] = null!;
			}
		}

		public void Initialize()
		{
			OpScrollBox scrollBox = null!;
			UIelement[] elements = new UIelement[]
			{
				new OpCheckBox(ForceBake, 10, 560),
				new OpLabel(45f, 560f, "Rebake All Rooms"),
				new OpCheckBox(HiddenSlugcats, 10, 520),
				new OpLabel(45f, 520f, "Include Hidden Slugcats"),
				new OpDragger(Threads, 10, 480),
				new OpLabel(45f, 480f, "Baking Threads"),
				scrollBox = new OpScrollBox(Vector2.zero, new Vector2(120f, 440f), Regions.Count * RegionCheckboxHeight, false, false),
				new OpLabel(10f, 450, "Regions:"),
				BakeButton = new OpSimpleButton(new Vector2(130, 0), new Vector2(80, 30), "Bake!"),
				StatusLabel = new OpLabel(250, 5, "")
			};

			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				var label = new OpLabel(250, 590 - ThreadLabelHeight * (i + 1), "");

				ThreadLabels.Add(label);
				AddItems(label);
			}

			BakeButton.OnClick += BakeClick;

			AddItems(elements);

			var acronyms = Regions.Keys.ToList();
			for (int i = 0; i < acronyms.Count; i++)
			{
				string region = acronyms[i];

				float posY = (Regions.Count - i - 1) * RegionCheckboxHeight;
				var checkBox = new OpCheckBox(OIUtil.CosmeticBind(false), 10, posY);
				var label = new OpLabel(45, posY, region);

				scrollBox.AddItems(checkBox, label);
				Regions[region] = checkBox;
			}
		}

		public void Update()
		{
			if (!Baking) return;

			_updateTimer++;
			if (_updateTimer < 40) return;

			_updateTimer = 0;

			var activeTasks = Tasks.Where(x => x.Started && !x.Finished).ToList();

			for (int i = 0; i < activeTasks.Count && i < ThreadLabels.Count; i++)
			{
				TaskData task = activeTasks[i];
				OpLabel label = ThreadLabels[i];

				TimeSpan duration;
				lock (task)
				{
					duration = task.Duration;
				}

				label.text = $"{task.Room}: {duration.Minutes:D2}:{duration.Seconds:D2}";
			}

			int finished = Tasks.Count(x => x.Finished);
			TimeSpan elapsed = DateTime.Now - BakeStartTime;

			string statusText = "";
			statusText += $"Baked Rooms: {finished}/{Tasks.Count}\r\n";
			statusText += $"Baking Time: {elapsed.Hours * 60 + elapsed.Minutes:D2}:{elapsed.Seconds:D2}\r\n";

			StatusLabel!.text = statusText;

			if (finished == Tasks.Count)
			{
				Baking = false;
				Tasks.Clear();
				BakeButton.greyedOut = false;
			}
		}

		public void BakeClick(UIfocusable trigger)
		{
			var regionsToBake = Regions.Where(x => x.Value.GetValueBool()).Select(x => x.Key).ToList();
			if (DEBUG) LogInfo("REGIONS TO BAKE (prior): " + string.Join(", ", regionsToBake));
			if (regionsToBake.Count == 0)
			{
				trigger.PlaySound(SoundID.MENU_Error_Ping);
				return;
			}

			Baking = true;
			trigger.greyedOut = true;
			BakeStartTime = DateTime.Now;
			TurboBake();
		}

		public void TurboBake()
		{
			try
			{
				bool includeHiddenSlugcats = HiddenSlugcats.Value;
				bool forceRebake = ForceBake.Value;

				var regionsToBake = Regions.Where(x => x.Value.GetValueBool()).Select(x => x.Key).ToList();
				if (DEBUG) LogInfo("REGIONS TO BAKE: " + string.Join(", ", regionsToBake));

				var worldLoaders = new List<WorldLoader>();
				foreach (string slugcatName in ExtEnumBase.GetNames(typeof(SlugcatStats.Name)))
				{
					var slugcat = new SlugcatStats.Name(slugcatName);

					if (!includeHiddenSlugcats && SlugcatStats.HiddenOrUnplayableSlugcat(slugcat)) continue;

					IEnumerable<Region> regions = Region.LoadAllRegions(SlugcatStats.SlugcatToTimeline(slugcat), null).Where(x => regionsToBake.Contains(x.name));

					foreach (Region region in regions)
					{
						var worldLoader = new WorldLoader(null, slugcat, SlugcatStats.SlugcatToTimeline(slugcat), false, region.name, region, RainWorld.LoadSetupValues(true), WorldLoader.LoadingContext.MAPMERGE);
						worldLoader.NextActivity();
						while (!worldLoader.Finished)
						{
							worldLoader.Update();
						}
						worldLoaders.Add(worldLoader);
						if (DEBUG) LogInfo("Loaded world " + region.name + " for " + slugcatName);
					}
				}

				if (DEBUG) LogInfo("ITERATING WORLDLOADER LIST");

				var queuedRooms = new List<string>();
				foreach (WorldLoader worldLoader in worldLoaders)
				{
					if (DEBUG) LogInfo("Retrieving world " + worldLoader.worldName + " for " + worldLoader.playerCharacter.value);
					World world = worldLoader.ReturnWorld();

					for (int i = 0; i < worldLoader.roomAdder.Count; i++)
					{
						string roomName = worldLoader.roomAdder[i][0];
						if (queuedRooms.Contains(worldLoader.roomAdder[i][0]))
						{
							LogInfo("Skipping already prepared room: " + roomName);
							continue;
						}
						queuedRooms.Add(roomName);
						LogInfo("Started preparing room: " + roomName);

						var roomText = File.ReadAllLines(WorldLoader.FindRoomFile(roomName, false, ".txt"));
						if (int.Parse(roomText[9].Split('|')[0], NumberStyles.Any, CultureInfo.InvariantCulture) < world.preProcessingGeneration || forceRebake)
						{
							AbstractRoom abstractRoom = worldLoader.abstractRooms[i];
							int generation = world.preProcessingGeneration;
							var room = new Room(null, world, abstractRoom);
							var roomPreparer = new RoomPreparer(room, false, false, false);
							var taskData = new TaskData(abstractRoom.name)
							{
								Size = room.Width * room.Height
							};
							LogInfo("Done preparing room: " + abstractRoom.name);

							var task = new Action(() =>
							{
								try
								{
									LogInfo("Started baking room: " + abstractRoom.name);
									lock (taskData)
									{
										taskData.StartTime = DateTime.Now;
									}
									taskData.Started = true;

									RunToCompletion(roomPreparer);

									abstractRoom.InitNodes(roomPreparer.ReturnRoomConnectivity(), roomText[1]);
									roomText[9] = RoomPreprocessor.ConnMapToString(generation, abstractRoom.nodes);
									roomText[10] = RoomPreprocessor.CompressAIMapsToString(room.aimap);
									File.WriteAllLines(WorldLoader.FindRoomFile(abstractRoom.name, false, ".txt"), roomText);

									LogInfo("Done baking room: " + abstractRoom.name);
									lock (taskData)
									{
										taskData.EndTime = DateTime.Now;
									}
									taskData.Finished = true;
								}
								catch (Exception ex)
								{
									LogError(ex);
								}
							});

							taskData.BakingTask = task;
							Tasks.Add(taskData);
						}
						else
						{
							LogInfo("Skipping already baked room: " + roomName);
						}
					}
				}

				Tasks = Tasks.OrderByDescending(x => x.Size).ToList();

				ActualBakeStartTime = DateTime.Now;
				new Thread(() => Parallel.Invoke(new ParallelOptions { MaxDegreeOfParallelism = Threads.Value }, Tasks.Select(x => x.BakingTask).ToArray())).Start();

				if (DEBUG) LogInfo("Created thread");
			}
			catch (Exception e)
			{
				LogError(e);
				BakeButton.PlaySound(SoundID.MENU_Error_Ping);
			}
		}

		private static void RunToCompletion(RoomPreparer preparer)
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
			while (!preparer.scMapper.done)
			{
				preparer.scMapper.Update();
			}
			preparer.scMapper = null;
			preparer.aiMapper = new AImapper(preparer.room);

			while (!preparer.aiMapper.done)
			{
				preparer.aiMapper.Update();
			}
			preparer.room.aimap = preparer.aiMapper.ReturnAIMap();

			preparer.aiDataPreprocessor = new AIdataPreprocessor(preparer.room.aimap, false);

			while (!preparer.aiDataPreprocessor.done)
			{
				preparer.aiDataPreprocessor.Update();
			}
		}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public class TaskData
		{
			public Action BakingTask;
			public bool Started;
			public bool Finished;
			public string Room;
			public int Size;
			public DateTime StartTime;
			public DateTime EndTime;
			public TimeSpan Duration
			{
				get
				{
					lock (this)
					{
						return (Finished ? EndTime : DateTime.Now) - StartTime;
					}
				}
			}

			public TaskData(string room)
			{
				Room = room;
			}
		}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		// Made by Alduris
		internal sealed class OIUtil : OptionInterface
		{
			private OIUtil() { }
			public static readonly OIUtil Instance = new();

			public static Configurable<T> CosmeticBind<T>(T init) => new(Instance, null, init, null);
			public static Configurable<T> CosmeticRange<T>(T val, T min, T max) where T : IComparable => new(val, new ConfigAcceptableRange<T>(min, max));
		}
	}
}
#pragma warning restore CS0162 // Unreachable code detected
