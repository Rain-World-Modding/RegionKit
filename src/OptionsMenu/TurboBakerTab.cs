using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;

namespace RegionKit.OptionsMenu
{
	/// <summary>
	/// Vigaro's Turbo Baker ported to RegionKit by Alduris
	/// </summary>
	public class TurboBakerTab : OpTab
	{
		private const float CheckboxSpacing = 30;
		private const float ThreadLabelHeight = 20;

		public List<TaskData> Tasks = new();
		public List<OpLabel> ThreadLabels = new();
		public OpLabel? StatusLabel;
		public OpSimpleButton BakeButton = null!;
		public DateTime BakeStartTime;
		public DateTime ActualBakeStartTime;
		public bool Baking = false;

		public OpDragger? ThreadsInput = null;
		public OpCheckBox? ForceBakeInput = null;
		public readonly Dictionary<SlugcatStats.Timeline, OpCheckBox> TimelinesMap = [];
		public readonly Dictionary<string, OpCheckBox> RegionsMap = [];

		private int _updateTimer;

		public TurboBakerTab(OptionInterface owner) : base(owner, "Bakery")
		{
		}

		private static Color TimelineColor(SlugcatStats.Timeline timeline)
		{
			// Check if slugcat has same name as timeline
			SlugcatStats.Name maybeName = new SlugcatStats.Name(timeline.value, false);
			if (maybeName.Index > -1)
			{
				Color useColor = PlayerGraphics.DefaultSlugcatColor(maybeName);
				Vector3 hsl = Custom.RGB2HSL(useColor);
				return Custom.HSL2RGB(hsl.x, hsl.y, Mathf.Lerp(0.4f, 1f, hsl.z));
			}

			// No defined slugcat with same name
			return Color.white;
		}

		public void Initialize()
		{
			List<SlugcatStats.Timeline> timelines = SlugcatStats.Timeline.values.entries.Select(x => new SlugcatStats.Timeline(x, false)).ToList();
			List<string> acronyms = Region.GetFullRegionOrder();

			OpScrollBox timelineScrollBox, regionScrollBox;
			UIelement[] elements = new UIelement[]
			{
				new OpLabel(45f, 560f, "Rebake All Rooms"),
				ForceBakeInput = new OpCheckBox(OIUtil.CosmeticBind(false), 10f, 560f),

				new OpLabel(45f, 520f, "Baking Threads"),
				ThreadsInput = new OpDragger(OIUtil.CosmeticRange(Mathf.CeilToInt(Environment.ProcessorCount * 0.5f), 1, Environment.ProcessorCount), 10f, 520f),

				new OpLabel(10f, 480f, "Timelines:"),
				timelineScrollBox = new OpScrollBox(new Vector2(0f, 300f), new Vector2(180f, 180f), timelines.Count * CheckboxSpacing, false, false),

				new OpLabel(10f, 270f, "Regions:"),
				regionScrollBox = new OpScrollBox(Vector2.zero, new Vector2(180f, 270f), acronyms.Count * CheckboxSpacing, false, false),

				BakeButton = new OpSimpleButton(new Vector2(190f, 0f), new Vector2(80f, 30f), "Bake!"),

				StatusLabel = new OpLabel(310, 5, "")
			};

			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				var label = new OpLabel(250, 590 - ThreadLabelHeight * (i + 1), "");

				ThreadLabels.Add(label);
				AddItems(label);
			}

			BakeButton.OnClick += BakeClick;

			AddItems(elements);

			TimelinesMap.Clear();
			for (int i = 0; i < timelines.Count; i++)
			{
				SlugcatStats.Timeline timeline = timelines[i];

				float posY = (timelines.Count - i - 1) * CheckboxSpacing;
				var checkBox = new OpCheckBox(OIUtil.CosmeticBind(true), 10, posY);
				var label = new OpLabel(45, posY, timeline.value);
				label.color = TimelineColor(timeline);
				timelineScrollBox.AddItems(checkBox, label);
				TimelinesMap[timeline] = checkBox;
			}

			RegionsMap.Clear();
			for (int i = 0; i < acronyms.Count; i++)
			{
				string region = acronyms[i];

				float posY = (acronyms.Count - i - 1) * CheckboxSpacing;
				var checkBox = new OpCheckBox(OIUtil.CosmeticBind(false), 10, posY);
				var label = new OpLabel(45, posY, region);
				label.color = Region.RegionColor(region);

				regionScrollBox.AddItems(checkBox, label);
				RegionsMap[region] = checkBox;
			}
		}

		public void Update()
		{
			if (!Baking) return;

			_updateTimer++;
			if (_updateTimer < 5) return; // 8 times a second

			_updateTimer = 0;

			var activeTasks = Tasks.Where(x => x.Started && !x.Finished).ToList();

			if (Tasks.Count > 0)
			{
				for (int i = 0; i < ThreadLabels.Count; i++)
				{
					OpLabel label = ThreadLabels[i];
					if (i < activeTasks.Count)
					{
						TaskData task = activeTasks[i];

						TimeSpan duration;
						lock (task)
						{
							duration = task.Duration;
						}

						label.text = $"{task.Room}: {duration.Minutes:D2}:{duration.Seconds:D2}";
					}
					else
					{
						label.text = "";
					}
				}
			}
			else
			{
				ThreadLabels[0].text = "Loading regions...";
			}

			int finished = Tasks.Count(x => x.Finished);
			TimeSpan elapsed = DateTime.Now - BakeStartTime;

			string statusText = "";
			statusText += $"Baked Rooms: {finished}/{Tasks.Count}\r\n";
			statusText += $"Baking Time: {elapsed.Hours * 60 + elapsed.Minutes:D2}:{elapsed.Seconds:D2}\r\n";

			StatusLabel!.text = statusText;

			if (Tasks.Count > 0 && finished == Tasks.Count)
			{
				Baking = false;
				Tasks.Clear();
				BakeButton.greyedOut = false;
			}
		}

		public void BakeClick(UIfocusable trigger)
		{
			var regionsToBake = RegionsMap.Where(x => x.Value.GetValueBool()).Select(x => x.Key).ToList();
			LogInfo("REGIONS TO BAKE (prior): " + string.Join(", ", regionsToBake));
			if (regionsToBake.Count == 0)
			{
				trigger.PlaySound(SoundID.MENU_Error_Ping);
				return;
			}

			foreach (var label in ThreadLabels)
			{
				label.text = "";
			}

			Baking = true;
			trigger.greyedOut = true;
			BakeStartTime = DateTime.Now;
			Task.Run(() => TurboBake()); // offload it to another thread so it doesn't cause a huge lagspike loading all the regions
		}

		public void TurboBake()
		{
			try
			{
				bool forceRebake = ForceBakeInput!.GetValueBool();

				var regionsToBake = RegionsMap.Where(x => x.Value.GetValueBool()).Select(x => x.Key).ToList();
				LogInfo("REGIONS TO BAKE: " + string.Join(", ", regionsToBake));

				var worldLoaders = new List<WorldLoader>();
				foreach ((SlugcatStats.Timeline timeline, OpCheckBox timelineCheckbox) in TimelinesMap)
				{
					if (timelineCheckbox.GetValueBool())
					{
						IEnumerable<Region> regions = Region.LoadAllRegions(timeline, null).Where(x => regionsToBake.Contains(x.name));

						foreach (Region region in regions)
						{
							var worldLoader = new WorldLoader(null, null, timeline, false, region.name, region, RainWorld.LoadSetupValues(true), WorldLoader.LoadingContext.MAPMERGE);
							worldLoader.NextActivity();
							while (!worldLoader.Finished)
							{
								worldLoader.Update();
							}
							worldLoaders.Add(worldLoader);
							LogInfo("Loaded world " + region.name + " for " + timeline);
						}
					}
				}

				LogInfo("ITERATING WORLDLOADER LIST");

				var queuedRooms = new List<string>();
				foreach (WorldLoader worldLoader in worldLoaders)
				{
					LogInfo("Retrieving world " + worldLoader.worldName + " for " + worldLoader.timelinePosition.value);
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
									LogError("Errored baking room: " + abstractRoom.name);
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
				new Thread(() => Parallel.Invoke(new ParallelOptions { MaxDegreeOfParallelism = ThreadsInput!.GetValueInt() }, Tasks.Select(x => x.BakingTask).ToArray())).Start();

				LogInfo("Created thread");
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

		public class TaskData
		{
			public Action? BakingTask;
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
