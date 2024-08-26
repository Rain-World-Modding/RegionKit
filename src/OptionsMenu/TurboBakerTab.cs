﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using static TurboBaker.TurboBaker;

namespace RegionKit.OptionsMenu
{
	public class TurboBakerTab : OpTab
	{
		private const float RegionCheckboxHeight = 30;
		private const float ThreadLabelHeight = 20;

		public static List<TaskData> Tasks = new();
		public static List<OpLabel> ThreadLabels = new();
		public static OpLabel StatusLabel;
		public static DateTime BakeStartTime;
		public static DateTime ActualBakeStartTime;
		public static bool Baking;

		public readonly Configurable<int> Threads;
		public readonly Configurable<bool> ForceBake;
		public readonly Configurable<bool> HiddenSlugcats;
		public readonly Dictionary<string, Configurable<bool>> Regions = new();

		private int _updateTimer;

		public TurboBakerTab(OptionInterface owner) : base(owner, "Bakery")
		{
			Threads = owner.config.Bind(nameof(Threads), Mathf.CeilToInt(Environment.ProcessorCount * 0.5f), new ConfigAcceptableRange<int>(1, Environment.ProcessorCount));
			ForceBake = owner.config.Bind(nameof(ForceBake), false);
			HiddenSlugcats = owner.config.Bind(nameof(HiddenSlugcats), false);
		}

		public void Initialize()
		{
			OpScrollBox scrollBox = null!;
			OpSimpleButton bakeButton = null!;
			UIelement[] elements = new UIelement[]
			{
				new OpCheckBox(ForceBake, 10, 560),
				new OpLabel(45f, 560f, "Rebake All Rooms"),
				new OpCheckBox(HiddenSlugcats, 10, 520),
				new OpLabel(45f, 520f, "Include Hidden Slugcats"),
				new OpSlider(Threads, new Vector2(6, 295), 200, true),
				new OpLabel(45f, 395f, "Baking Threads"),
				scrollBox = new OpScrollBox(Vector2.zero, new Vector2(120f, 280), Regions.Count * RegionCheckboxHeight, false, false),
				new OpLabel(130f, 140, "Regions"),
				bakeButton = new OpSimpleButton(new Vector2(130, 0), new Vector2(80, 30), "Bake!"),
				StatusLabel = new OpLabel(250, 5, "")
			};

			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				var label = new OpLabel(250, 590 - ThreadLabelHeight * (i + 1), "");

				ThreadLabels.Add(label);
				AddItems(label);
			}

			bakeButton.OnClick += BakeClick;

			AddItems(elements);

			var acronyms = Regions.Keys.ToList();
			for (int i = 0; i < acronyms.Count; i++)
			{
				string region = acronyms[i];
				Configurable<bool> configurable = Regions[region];

				float posY = (Regions.Count - i - 1) * RegionCheckboxHeight;
				var checkBox = new OpCheckBox(configurable, 10, posY);
				var label = new OpLabel(45, posY, region);

				scrollBox.AddItems(checkBox, label);
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

			StatusLabel.text = statusText;
		}

		public void BakeClick(UIfocusable trigger)
		{
			Baking = true;
			trigger.greyedOut = true;
			BakeStartTime = DateTime.Now;
			owner._SaveConfigFile();
			TurboBake();
		}

		public void TurboBake()
		{
			bool includeHiddenSlugcats = HiddenSlugcats.Value;
			bool forceRebake = ForceBake.Value;

			var regionsToBake = Regions.Where(x => x.Value.Value).Select(x => x.Key).ToList();

			var worldLoaders = new List<WorldLoader>();
			foreach (string? slugcatName in ExtEnumBase.GetNames(typeof(SlugcatStats.Name)))
			{
				var slugcat = new SlugcatStats.Name(slugcatName);

				if (!includeHiddenSlugcats && SlugcatStats.HiddenOrUnplayableSlugcat(slugcat)) continue;

				IEnumerable<Region> regions = Region.LoadAllRegions(slugcat).Where(x => regionsToBake.Contains(x.name));

				foreach (Region? region in regions)
				{
					var worldLoader = new WorldLoader(null, slugcat, false, region.name, region, RainWorld.LoadSetupValues(true));
					worldLoader.NextActivity();
					while (!worldLoader.Finished)
					{
						worldLoader.Update();
					}
					worldLoaders.Add(worldLoader);
				}
			}

			var queuedRooms = new List<string>();
			foreach (WorldLoader worldLoader in worldLoaders)
			{
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
						var taskData = new TaskData(abstractRoom.name);
						taskData.Size = room.Width * room.Height;
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
	}
}