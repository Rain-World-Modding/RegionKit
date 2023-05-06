using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static RoofTopView;
using static AboveCloudsView;
using static RegionKit.Modules.BackgroundBuilder.BackgroundElementData;

namespace RegionKit.Modules.BackgroundBuilder;

public class BackgroundTemplateType : ExtEnum<BackgroundTemplateType>
{
	public BackgroundTemplateType(string value, bool register = false) : base(value, register)
	{
	}
	public static readonly BackgroundTemplateType None = new("None", true);

	public static readonly BackgroundTemplateType AboveCloudsView = new("AboveCloudsView", true);

	public static readonly BackgroundTemplateType RoofTopView = new("RoofTopView", true);

	public static readonly BackgroundTemplateType VoidSeaScene = new("VoidSeaScene", true);
}

internal static class Data
{
	#region hooks
	public static void Apply()
	{
		On.RoomSettings.Load += RoomSettings_Load;
		On.RoomSettings.Save_string_bool += RoomSettings_Save_string_bool;
		On.RoomSettings.InheritEffects += RoomSettings_InheritEffects;
	}

	public static void Undo()
	{
		On.RoomSettings.Load -= RoomSettings_Load;
		On.RoomSettings.Save_string_bool -= RoomSettings_Save_string_bool;
		On.RoomSettings.InheritEffects -= RoomSettings_InheritEffects;
	}

	private static void RoomSettings_InheritEffects(On.RoomSettings.orig_InheritEffects orig, RoomSettings self)
	{
		orig(self);
		if (self.parent.BackgroundData().roomOffsetInit && !self.BackgroundData().roomOffsetInit)
		{ self.BackgroundData().roomOffset = self.parent.BackgroundData().roomOffset; }

		if (self.parent.BackgroundData().HasData() && !self.BackgroundData().HasData())
		{ self.BackgroundData().InheritFromTemplate(self.parent.BackgroundData()); }
	}

	private static void RoomSettings_Save_string_bool(On.RoomSettings.orig_Save_string_bool orig, RoomSettings self, string path, bool saveAsTemplate)
	{
		orig(self, path, saveAsTemplate);
		if (self.BackgroundData().SaveLines().Length > 0)
		{ File.AppendAllLines(path, self.BackgroundData().SaveLines()); }
	}

	private static bool RoomSettings_Load(On.RoomSettings.orig_Load orig, RoomSettings self, SlugcatStats.Name playerChar)
	{
		if (!orig(self, playerChar))
		{ return false; }

		foreach (string line in File.ReadAllLines(self.filePath))
		{
			string[] array2 = Regex.Split(line, ": ");

			if (array2.Length == 2 && array2[0] == "BackgroundOffset")
			{
				string[] array3 = Regex.Split(array2[1], ",");
				self.BackgroundData().roomOffset = new Vector2(float.Parse(array3[0]), float.Parse(array3[1]));
				self.BackgroundData().roomOffsetInit = true;
			}

			if (array2.Length == 2 && array2[0] == "BackgroundType")
			{ self.BackgroundData().FromName(array2[1], playerChar); }
		}
		return true;
	}
	#endregion hooks

	private static readonly ConditionalWeakTable<RoomSettings, RoomBGData> table = new();

	public static RoomBGData BackgroundData(this RoomSettings p) => table.GetValue(p, _ => new RoomBGData(p));
	public static RoomBGData BackgroundData(this RoomSettings p, RoomBGData r) => table.GetValue(p, _ => r);

	public class RoomBGData
	{
		public RoomBGData(RoomSettings roomSettings)
		{ this.roomSettings = roomSettings; }

		public RoomSettings roomSettings;

		public Vector2 roomOffset = Vector2.zero;
		public bool roomOffsetInit = false; //nullables were too painful to work with

		public void UpdateOffsetInit() => roomOffsetInit = (roomOffset == roomSettings.parent.BackgroundData().roomOffset);

		public string backgroundName = "";

		public bool protect = false;

		public Vector2 backgroundOffset = Vector2.zero;

		public BackgroundTemplateType type = BackgroundTemplateType.None;

		public RoomBGData? parent = null;

		public BGData realData = new();

		/// <summary>
		/// Checks if data was loaded from txt or serialized from background
		/// </summary>
		public bool HasData() => type != BackgroundTemplateType.None && TryGetPathFromName(backgroundName, out _);

		public void Reset()
		{
			backgroundName = "";
			protect = false;
			backgroundOffset = Vector2.zero;
			type = BackgroundTemplateType.None;
			parent = null;
			realData = new();
		}

		/// <summary>
		/// processes slugcat conditions and removes any that no longer apply
		/// </summary>
		public static string[] ProcessedLines(List<string> lines, SlugcatStats.Name slug)
		{
			string remove = "lll";

			for (int i = 0; i < lines.Count; i++)
			{
				if (lines[i].Length < 1) continue;
				if (lines[i][0] == '(' && lines[i].Contains(')'))
				{
					bool include = false;
					bool inverted = false;

					string text = lines[i].Substring(1, lines[i].IndexOf(")") - 1);
					if (text.StartsWith("X-"))
					{
						text = text.Substring(2);
						inverted = true;
					}

					if (slug == null)
					{
						lines[i] = !inverted ? remove : lines[i].Substring(lines[i].IndexOf(")") + 1);
						continue;
					}

					foreach (string text2 in text.Split(','))
					{
						if (text2 == slug.ToString())
						{
							include = true;
							break;
						}
					}

					include = inverted != include;

					lines[i] = !include? remove : lines[i].Substring(lines[i].IndexOf(")") + 1);
				}
			}

			lines.RemoveAll(x => x == remove);

			return lines.ToArray();
		}

		public void FromName(string name, SlugcatStats.Name slug)
		{
			Reset();
			backgroundName = name;
			if (TryGetPathFromName(name, out string path)) TryGetFromPath(path, slug);
			else { backgroundName = ""; }
		}

		/// <summary>
		/// loads macro data from file
		/// </summary>
		public bool TryGetFromPath(string path, SlugcatStats.Name slug)
		{
			if (!File.Exists(path)) return false;

			string[] lines = ProcessedLines(File.ReadAllLines(path).ToList(), slug);

			foreach (string line in lines)
			{
				try
				{
					if (line == "PROTECTED") protect = true;

					string[] array = Regex.Split(line, ": ");
					if (array.Length != 2) continue;

					switch (array[0])
					{
					case "OffsetX":
						backgroundOffset.x = float.Parse(array[1]);
						break;

					case "OffsetY":
						backgroundOffset.y = float.Parse(array[1]);
						break;

					case "Type":
						SetBGTypeAndData((BackgroundTemplateType)ExtEnumBase.Parse(typeof(BackgroundTemplateType), array[1], false));
						
						break;

					case "Parent":
						if (TryGetPathFromName(array[1], out string parentPath) && array[1].ToLower() != backgroundName.ToLower())
						{
							parent = new RoomBGData(roomSettings);
							parent.FromName(array[1], slug);
							InheritFromParent();
						}
						break;
					}
				}
				catch (Exception e) { __logger.LogError($"BackgroundBuilder: error loading line [{line}]\n{e}"); }
			}
			realData.LoadData(lines);
			return true;
		}

		/// <summary>
		/// For inheriting from RoomSettings template
		/// </summary>
		public void InheritFromTemplate(RoomBGData templateData)
		{
			realData = templateData.realData;
			parent = templateData.parent;
			backgroundOffset = templateData.backgroundOffset;
			protect = templateData.protect;
			backgroundName = templateData.backgroundName;
			type = templateData.type;

		}

		/// <summary>
		/// For inheriting from parent background after parent is already assigned
		/// </summary>
		public void InheritFromParent()
		{
			if (parent == null) return;

			backgroundOffset = parent.backgroundOffset;
			type = parent.type;
			realData = parent.realData;
		}

		public void SetBGTypeAndData(BackgroundTemplateType type)
		{
			if (parent?.type == type) return;

			this.type = type;

			if (type == BackgroundTemplateType.AboveCloudsView)
			{ realData = new AboveCloudsView_BGData(); }

			else if (type == BackgroundTemplateType.RoofTopView)
			{ realData = new RoofTopView_BGData(); }

			else { realData = new BGData(); }
			
		}

		/// <summary>
		/// returns the string that goes in background files
		/// </summary>
		public string Serialize()
		{
			List<string> lines = new List<string>
			{
				$"Type: {type}",
			$"OffsetX: {backgroundOffset.x}",
			$"OffsetY: {backgroundOffset.y}",
			"---------------",
			};

			return string.Join("\n",lines) + "\n" + realData.Serialize();
		}

		/// <summary>
		/// returns the string lines that goes in room settings.txt files
		/// </summary>
		public string[] SaveLines()
		{
			List<string> result = new();
			if (roomOffsetInit && roomOffset != roomSettings.parent.BackgroundData().roomOffset)
			{ result.Add($"BackgroundOffset: {roomOffset.x},{roomOffset.y}"); }

			bool isParent = TryGetPathFromName(roomSettings.parent.BackgroundData().backgroundName, out _) && backgroundName.ToLower() == roomSettings.parent.BackgroundData().backgroundName.ToLower();
			if (TryGetPathFromName(backgroundName, out _) && !isParent)
			{ result.Add($"BackgroundType: {backgroundName}"); }

			return result.ToArray();
		}
	}

	public partial class BGData
	{
		public virtual string Serialize()
		{
			List<string> list = new();
			foreach (CustomBgElement element in backgroundElements)
			{
				list.Add(element.Serialize());
			}
			return string.Join("\n", list);
		}

		public virtual void MakeScene(BackgroundScene self)
		{
			sceneInitialized = true;
			if (backgroundElements.Count != 0)
			{
				self.elements.RemoveAll(x => { if (x.HasInstanceData()) { x.Destroy(); return true; } return false; });

				foreach (CustomBgElement element in backgroundElements)
				{
					self.AddElement(element.MakeSceneElement(self));
				}
			}
			else
			{
				foreach (BackgroundScene.BackgroundSceneElement element in self.elements)
				{
					if (element.HasInstanceData())
					{ backgroundElements.Add(element.DataFromElement()); }
				}
			}
		}
		public virtual void UpdateSceneElement(string message)
		{

		}

		public virtual void LoadData(string[] fileText)
		{
			foreach (string line in fileText)
			{ LineToData(line); }
		}
		public virtual void LineToData(string line)
		{
			if (line.StartsWith("REMOVE_"))
			{
				string[] array = Regex.Split(line.Substring(7), ": ");
				if (array.Length < 2) return;

				string removeElement = $"{array[0]}: {array[1]}";
				backgroundElements.RemoveAll(x => x.Serialize() == removeElement);
			}
		}

		public List<CustomBgElement> backgroundElements = new();

		public bool sceneInitialized = false;
	}

	public class AboveCloudsView_BGData : BGData
	{

		public float startAltitude;
		public float endAltitude;
		public float cloudsStartDepth;
		public float cloudsEndDepth;
		public float distantCloudsEndDepth;
		public float cloudsCount;
		public float distantCloudsCount;
		public float curveCloudDepth;
		public float overrideYStart;
		public float overrideYEnd;

		public string daySky;

		public string duskSky;

		public string nightSky;

		public AboveCloudsView? Scene;

		public AboveCloudsView_BGData()
		{
			startAltitude = float.MaxValue;
			endAltitude = float.MaxValue;
			cloudsStartDepth = float.MaxValue;
			cloudsEndDepth = float.MaxValue;
			distantCloudsEndDepth = float.MaxValue;
			cloudsCount = float.MaxValue;
			distantCloudsCount = float.MaxValue;
			curveCloudDepth = float.MaxValue;
			overrideYStart = float.MaxValue;
			overrideYEnd = float.MaxValue;
			daySky = "";
			duskSky = "";
			nightSky = "";
		}

		public override void MakeScene(BackgroundScene self)
		{
			if (self is not AboveCloudsView acv) return;

			Scene = acv;

			Scene.SyncAndLoadIfTextureNameExists(ref daySky, ref acv.daySky.illustrationName);

			Scene.SyncAndLoadIfTextureNameExists(ref duskSky, ref acv.duskSky.illustrationName);

			Scene.SyncAndLoadIfTextureNameExists(ref nightSky, ref acv.nightSky.illustrationName);

			SyncIfDefault(ref startAltitude, ref acv.startAltitude);
			SyncIfDefault(ref endAltitude, ref acv.endAltitude);

			bool redoClouds = SyncIfDefault(ref cloudsStartDepth, ref acv.cloudsStartDepth);

			redoClouds = SyncIfDefault(ref cloudsEndDepth, ref acv.cloudsEndDepth) || redoClouds;
			redoClouds = SyncIfDefault(ref distantCloudsEndDepth, ref acv.distantCloudsEndDepth) || redoClouds;

			float count = acv.elements.Where(x => x is CloseCloud).Count();
			redoClouds = SyncIfDefault(ref cloudsCount, ref count) || redoClouds;

			count = acv.elements.Where(x => x is DistantCloud).Count();
			redoClouds = SyncIfDefault(ref distantCloudsCount, ref count) || redoClouds;

			count = 1;
			redoClouds = SyncIfDefault(ref curveCloudDepth, ref count) || redoClouds;

			count = -40 * cloudsEndDepth;
			redoClouds = SyncIfDefault(ref overrideYStart, ref count) || redoClouds;

			count = 0;
			redoClouds = SyncIfDefault(ref overrideYEnd, ref count) || redoClouds;

			self.sceneOrigo = new Vector2(2514f, (acv.startAltitude + acv.endAltitude) / 2f);

			base.MakeScene(self);

			if (redoClouds)
			{
				//reset clouds
				self.elements.RemoveAll(x => { if (x is Cloud) { x.Destroy(); return true; } return false; });
				Scene.clouds = new();

				for (int i = 0; i < cloudsCount; i++)
				{
					float cloudDepth = i / (cloudsCount - 1f);
					CloseCloud cloud = new CloseCloud(acv, new Vector2(0f, 0f), cloudDepth, i);
					self.AddElement(cloud);
				}

				for (int j = 0; j < distantCloudsCount; j++)
				{
					float num15 = j / (distantCloudsCount - 1f);
					num15 = Mathf.Pow(num15, curveCloudDepth);
					DistantCloud dcloud = new DistantCloud(Scene, new Vector2(0f, Mathf.Lerp(overrideYStart, overrideYEnd, num15)), num15, j);
					Scene.AddElement(dcloud);
				}
			}
		}

		public override string Serialize()
		{
			List<string> lines = new List<string>
			{
				$"startAltitude: {startAltitude}",
				$"endAltitude: {endAltitude}",
				$"cloudsStartDepth: {cloudsStartDepth}",
				$"cloudsEndDepth: {cloudsEndDepth}",
				$"distantCloudsEndDepth: {distantCloudsEndDepth}",
				$"cloudsCount: {cloudsCount}",
				$"distantCloudsCount: {distantCloudsCount}",
				$"curveCloudDepth: {curveCloudDepth}",
				$"daySky: {daySky}",
				$"duskSky: {duskSky}",
				$"nightSky: {nightSky}"
			};

			if (curveCloudDepth != 1) lines.Add($"curveCloudDepth: {curveCloudDepth}");
			if (overrideYStart != -40 * cloudsEndDepth) lines.Add($"overrideYStart: {overrideYStart}");
			if (overrideYEnd != 0) lines.Add($"overrideYEnd: {overrideYEnd}");

			foreach (DistantCloud cloud in Scene.clouds.Where(x => x is DistantCloud))
			{
				Debug.Log("\nCLOUD DEPTHS\nshouldn't go in background file, but useful for positioning\n");
				Debug.Log($"distantCloud: {cloud.pos.x}, {cloud.pos.y}, {cloud.depth}, {cloud.distantCloudDepth}");
			}

			return string.Join("\n", lines) + "\n\n" + base.Serialize();

		}

		public override void UpdateSceneElement(string message)
		{
			if (Scene == null) return;

			Scene.startAltitude = startAltitude;
			Scene.endAltitude = endAltitude;

			Scene.sceneOrigo = new Vector2(2514f, (Scene.startAltitude + Scene.endAltitude) / 2f);

			if (message == "CloudAmount")
			{
				foreach (BackgroundScene.BackgroundSceneElement element in Scene.elements.ToList())
				{
					if (element is DistantCloud or CloseCloud)
					{
						element.Destroy();
						Scene.elements.Remove(element);
					}
				}

				int num = (int)cloudsCount;
				for (int i = 0; i < num; i++)
				{
					float cloudDepth = i / (float)(num - 1);
					CloseCloud cloud = new CloseCloud(Scene, new Vector2(0f, 0f), cloudDepth, i);
					cloud.CData().needsAddToRoom = true;
					Scene.AddElement(cloud);
				}

				num = (int)distantCloudsCount;
				for (int j = 0; j < num; j++)
				{
					float num15 = j / (float)(num - 1);
					DistantCloud dcloud = new DistantCloud(Scene, new Vector2(0f, -40f * Scene.cloudsEndDepth * (1f - num15)), num15, j);
					dcloud.CData().needsAddToRoom = true;
					Scene.AddElement(dcloud);
				}
			}
		}

		public override void LineToData(string line)
		{
			base.LineToData(line);

			string[] array = Regex.Split(line, ": ");
			if (array.Length < 2) return;

			switch (array[0])
			{
			case "startAltitude":
				startAltitude = float.Parse(array[1].Trim());
				break;

			case "endAltitude":
				endAltitude = float.Parse(array[1].Trim());
				break;

			case "cloudsStartDepth":
				cloudsStartDepth = float.Parse(array[1].Trim());
				break;

			case "cloudsEndDepth":
				cloudsEndDepth = float.Parse(array[1].Trim());
				break;

			case "distantCloudsEndDepth":
				distantCloudsEndDepth = float.Parse(array[1].Trim());
				break;

			case "cloudsCount":
				cloudsCount = float.Parse(array[1].Trim());
				break;

			case "distantCloudsCount":
				distantCloudsCount = float.Parse(array[1].Trim());
				break;

			case "curveCloudDepth":
				curveCloudDepth = float.Parse(array[1].Trim());
				break;

			case "overrideYStart":
				overrideYStart = float.Parse(array[1].Trim());
				break;

			case "overrideYEnd":
				overrideYEnd = float.Parse(array[1].Trim());
				break;

			case "daySky":
				daySky = array[1];
				break;

			case "duskSky":
				duskSky = array[1];
				break;

			case "nightSky":
				nightSky = array[1];
				break;

			case "DistantBuilding":
			case "DistantLightning":
			case "FlyingCloud":
				if (TryGetBgElementFromString(line, out CustomBgElement element))
				{ backgroundElements.Add(element); }
				break;
			}
		}
	}

	public class RoofTopView_BGData : BGData
	{
		public float floorLevel;

		public string daySky;

		public string duskSky;

		public string nightSky;

		public RoofTopView? Scene;

		public RoofTopView_BGData()
		{
			floorLevel = float.MaxValue;
			daySky = "";
			duskSky = "";
			nightSky = "";
		}

		public override void MakeScene(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) return;

			Scene = rtv;

			if (Scene.floorLevel != floorLevel && floorLevel != float.MaxValue)
			{
				foreach (BackgroundScene.BackgroundSceneElement element in Scene.elements)
				{
					if (element is RoofTopView.DistantBuilding or Building or Floor or RoofTopView.Smoke or Rubble)
					{ element.pos.y += floorLevel - Scene.floorLevel; }
				}
				Scene.floorLevel = floorLevel;
			}
			SyncIfDefault(ref floorLevel, ref rtv.floorLevel);

			Scene.SyncAndLoadIfTextureNameExists(ref daySky, ref rtv.daySky.illustrationName);

			Scene.SyncAndLoadIfTextureNameExists(ref duskSky, ref rtv.duskSky.illustrationName);

			Scene.SyncAndLoadIfTextureNameExists(ref nightSky, ref rtv.nightSky.illustrationName);


			base.MakeScene(self);
		}

		public override string Serialize()
		{
			List<string> lines = new List<string>
			{
				$"floorLevel: {floorLevel}",
				$"daySky: {daySky}",
				$"duskSky: {duskSky}",
				$"nightSky: {nightSky}"
			};

			return string.Join("\n", lines) + "\n" + base.Serialize();

		}

		public override void UpdateSceneElement(string message)
		{
			if (Scene == null) return;

			if (Scene.floorLevel != floorLevel)
			{
				foreach (BackgroundScene.BackgroundSceneElement element in Scene.elements)
				{
					if (element is RoofTopView.DistantBuilding or Building or Floor or RoofTopView.Smoke or Rubble)
					{ element.pos.y += floorLevel - Scene.floorLevel; }
				}
				Scene.floorLevel = floorLevel;
			}
		}

		public override void LineToData(string line)
		{
			base.LineToData(line);

			string[] array = Regex.Split(line, ": ");
			if (array.Length < 2) return;

			switch (array[0])
			{
			case "floorLevel":
				floorLevel = float.Parse(array[1].Trim());
				break;

			case "daySky":
				daySky = array[1];
				break;

			case "duskSky":
				duskSky = array[1];
				break;

			case "nightSky":
				nightSky = array[1];
				break;

			case "DistantBuilding":
				if (TryGetBgElementFromString("RF_"+line, out CustomBgElement element))
				{ backgroundElements.Add(element); }
				break;
			case "Floor":
			case "Building":
			case "DistantGhost":
			case "DustWave":
			case "Smoke":
				if (TryGetBgElementFromString(line, out CustomBgElement element2))
				{ backgroundElements.Add(element2); }
				break;
			}
		}
	}

	public static bool TryGetPathFromName(string name, out string path)
	{
		path = "";
		try
		{
			path = AssetManager.ResolveFilePath(Path.Combine(_Module.BGPath, name + ".txt"));
			if (File.Exists(path)) return true;
			else { return false; }
		}
		catch { return false; }
	}

	public static bool SyncIfDefault(ref float one, ref float two)
	{
		if (one != float.MaxValue)
		{ two = one; return true; }
		else { one = two; return false; }
	}

	public static bool SyncAndLoadIfTextureNameExists(this BackgroundScene scene, ref string newName, ref string currentName, bool crispPixels = true, bool clampWrapMode = true)
	{
		if (SyncIfTextureNameExists(ref newName, ref currentName))
		{
			scene.LoadGraphic(newName, crispPixels, clampWrapMode);
			return true;
		}
		return false;
	}

	public static bool SyncIfTextureNameExists(ref string newName, ref string currentName)
	{
		if (DoesTextureExist(newName))
		{ currentName = newName; return true; }
		else { newName = currentName; return false; }
	}

	public static bool DoesTextureExist(string name)
	{
		return Futile.atlasManager.GetAtlasWithName(name) != null || File.Exists(AssetManager.ResolveFilePath("Illustrations" + Path.DirectorySeparatorChar.ToString() + name + ".png"));
	}

	public static bool HasInstanceData(this BackgroundScene.BackgroundSceneElement element)
	{ 
		return element is AboveCloudsView.DistantBuilding or DistantLightning or FlyingCloud or Floor or
			RoofTopView.DistantBuilding or Building or DistantGhost or DustWave or RoofTopView.Smoke; 
	}
}
