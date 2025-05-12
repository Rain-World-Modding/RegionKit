using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static RoofTopView;
using static AboveCloudsView;
using static RegionKit.Modules.BackgroundBuilder.BackgroundElementData;
using static RegionKit.Modules.BackgroundBuilder.CustomBackgroundElements;
using System.Reflection;
using System;
using Watcher;

namespace RegionKit.Modules.BackgroundBuilder;

public class BackgroundTemplateType : ExtEnum<BackgroundTemplateType>
{
	public BackgroundTemplateType(string value, bool register = false) : base(value, register)
	{
	}
	public static readonly BackgroundTemplateType None = new("None", true);

	public static readonly BackgroundTemplateType AboveCloudsView = new("AboveCloudsView", true);

	public static readonly BackgroundTemplateType RoofTopView = new("RoofTopView", true);

	public static readonly BackgroundTemplateType AncientUrbanView = new("AncientUrbanView", true);

	public static readonly BackgroundTemplateType RotWormScene = new("RotWormScene", true);

	public static readonly BackgroundTemplateType VoidSeaScene = new("VoidSeaScene", true);
}

internal static class Data
{
	#region reflection
	class BackgroundDataAttribute : Attribute
	{
		public string? name = null;
		public int order = 0;
		/// <summary>
		/// if backingFieldName isn't null, the serializer will only write if the backing field is assigned
		/// </summary>
		public string? backingFieldName = null;
		public string? defaultFieldName = null;

		public bool ShouldWrite<T>(T data, object value) where T : BGSceneData
		{
			if (defaultFieldName != null) return typeof(T).GetField(defaultFieldName, BF_ALL_CONTEXTS)?.GetValue(data) != value;
			return backingFieldName == null || typeof(T).GetField(backingFieldName, BF_ALL_CONTEXTS)?.GetValue(data) != null;
		}
	}
	public static IEnumerable<string> SerializeBackgroundData(BGSceneData data)
	{
		foreach ((string name, (PropertyInfo info, BackgroundDataAttribute attribute)) in GetPropertyAttributes(data).OrderBy(x => x.Value.Item2.order))
		{
			object value = info.GetValue(data);
			if (attribute.ShouldWrite(data, value)) yield return name + ": " + ObjectToString(value);
		}
	}

	public static void ParseBackgroundData(BGSceneData data, string[] fileText)
	{
		Dictionary<string, Tuple<PropertyInfo, BackgroundDataAttribute>> attributes = GetPropertyAttributes(data);

		foreach (string line in fileText)
		{
			string[] array = Regex.Split(line, ": ");
			if (array.Length < 2) continue;
			if (attributes.ContainsKey(array[0]))
			{
				try
				{
					Type type = attributes[array[0]].Item1.PropertyType;
					bool nullable = false;
					if (Nullable.GetUnderlyingType(type) != null)
					{
						nullable = true;
						type = Nullable.GetUnderlyingType(type);
					}
					object value = StringToObject(array[1], type);
					if (value != null || nullable)
						attributes[array[0]].Item1.SetValue(data, value);
				}
				catch { }
			}
			else
			{
				data.LineToData(line);
			}
		}
	}

	private static Dictionary<string, Tuple<PropertyInfo, BackgroundDataAttribute>> GetPropertyAttributes(BGSceneData data)
	{
		Dictionary<string, Tuple<PropertyInfo, BackgroundDataAttribute>> attributes = new();
		foreach (PropertyInfo prop in data.GetType().GetProperties(BF_ALL_CONTEXTS))
		{
			if (prop.GetCustomAttribute(typeof(BackgroundDataAttribute)) is BackgroundDataAttribute attribute)
			{
				attributes[attribute.name ?? prop.Name] = new(prop, attribute);
			}
		}
		return attributes;
	}

	public static string ObjectToString(object obj)
	{
		return obj switch
		{
			bool => (bool)obj ? "True" : "False",
			Color => colorToHex((Color)obj),
			Vector2 => $"{((Vector2)obj).x}, {((Vector2)obj).y}",
			string => (string)obj,
			null => "Null",
			_ => obj.ToString()
		};
	}

	public static object StringToObject(string str, Type type)
	{
		if (str == "Null") return null!;

		if (type == typeof(string)) return str;
		else if (type == typeof(bool)) return str == "True";
		else if (type == typeof(int)) return int.Parse(str);
		else if (type == typeof(float)) return float.Parse(str);
		else if (type.IsEnum) return Enum.Parse(type, str);
		else if (type == typeof(Color)) return hexToColor(str);
		else if (type == typeof(Vector2))
		{
			string[] array2 = Regex.Split(str, ",").Select(p => p.Trim()).ToArray();
			return new Vector2(float.Parse(array2[0]), float.Parse(array2[1]));
		}
		return null!;
	}
	#endregion
	#region hooks
	public static void Apply()
	{
		On.RoomSettings.Load_Timeline += RoomSettings_Load;
		_CommonHooks.RoomSettingsSave += RoomSettings_Save_string_bool;
		On.RoomSettings.InheritEffects += RoomSettings_InheritEffects;
	}

	public static void Undo()
	{
		On.RoomSettings.Load_Timeline -= RoomSettings_Load;
		_CommonHooks.RoomSettingsSave -= RoomSettings_Save_string_bool;
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

	private static List<string> RoomSettings_Save_string_bool(RoomSettings self, bool saveAsTemplate)
	{
		return self.BackgroundData().SaveLines().ToList();
	}

	private static bool RoomSettings_Load(On.RoomSettings.orig_Load_Timeline orig, RoomSettings self, SlugcatStats.Timeline timeline)
	{
		if (!orig(self, timeline))
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
			{ self.BackgroundData().FromTimeline(array2[1], timeline); }
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

		public void UpdateOffsetInit() => roomOffsetInit = (roomOffset != roomSettings.parent.BackgroundData().roomOffset);

		public string backgroundName = "";

		public bool protect = false;

		public Vector2 backgroundOffset = Vector2.zero;

		public BackgroundTemplateType type = BackgroundTemplateType.None;

		public RoomBGData? parent = null;

		public BGSceneData sceneData = new();

		public SlugcatStats.Timeline timeline = null!;

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
			sceneData = new();
		}

		[Obsolete("Deprecated. Use FromTimeline instead")]
		public void FromName(string name, SlugcatStats.Name slug)
		{
			FromTimeline(name, SlugcatStats.SlugcatToTimeline(slug));
		}

		public void FromTimeline(string name, SlugcatStats.Timeline timeline)
		{
			Reset();
			backgroundName = name;
			if (TryGetPathFromName(name, out string path)) TryGetFromPath(path, timeline);
			else { backgroundName = ""; }
		}

		/// <summary>
		/// loads macro data from file
		/// </summary>
		public bool TryGetFromPath(string path, SlugcatStats.Timeline timeline)
		{
			this.timeline = timeline;
			if (!File.Exists(path)) return false;

			string[] lines = ProcessTimelineConditions(File.ReadAllLines(path), timeline);

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
							parent.FromTimeline(array[1], timeline);
							InheritFromParent();
						}
						break;
					}
				}
				catch (Exception e) { LogError($"BackgroundBuilder: error loading line [{line}]\n{e}"); }
			}
			//realData.LoadData(lines);
			return true;
		}
		public void LoadSceneData(BackgroundScene scene)
		{
			if (parent != null)
			{
				parent.LoadSceneData(scene);
				InheritFromParent();
			}
			if (!TryGetPathFromName(backgroundName, out string path) || !File.Exists(path)) return;

			string[] lines = ProcessTimelineConditions(File.ReadAllLines(path), timeline);
			if (sceneData is AboveCloudsView_SceneData acvd && scene is AboveCloudsView acv) { acvd.Scene = acv; }
			if (sceneData is RoofTopView_SceneData rtvd && scene is RoofTopView rtv) { rtvd.Scene = rtv; }
			sceneData.LoadData(lines);
		}

		/// <summary>
		/// For inheriting from RoomSettings template
		/// </summary>
		public void InheritFromTemplate(RoomBGData templateData)
		{
			sceneData = templateData.sceneData;
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
			sceneData = parent.sceneData;
		}

		public void SetBGTypeAndData(BackgroundTemplateType type)
		{
			if (parent?.type == type) return;

			this.type = type;

			if (type == BackgroundTemplateType.AboveCloudsView)
			{ sceneData = new AboveCloudsView_SceneData(); }

			else if (type == BackgroundTemplateType.RoofTopView)
			{ sceneData = new RoofTopView_SceneData(); }

			else if (type == BackgroundTemplateType.AncientUrbanView)
			{ sceneData = new AncientUrbanView_SceneData(); }

			else if (type == BackgroundTemplateType.RotWormScene)
			{ sceneData = new RotWormScene_SceneData(); }

			else { sceneData = new BGSceneData(); }

		}

		/// <summary>
		/// returns the string that goes in background files
		/// </summary>
		public List<string> Serialize()
		{
			List<string> lines = new List<string>
			{
				$"Type: {type}",
			$"OffsetX: {backgroundOffset.x}",
			$"OffsetY: {backgroundOffset.y}",
			"---------------",
			};

			return lines.Concat(sceneData.Serialize()).ToList();
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

	public partial class BGSceneData
	{
		[BackgroundData(backingFieldName = nameof(_defaultContainer), name = "DefaultContainer")]
		public ContainerCodes? defaultContainer
		{
			get => _defaultContainer;
			set
			{
				_defaultContainer = value;
				//lol we're not updating all containers...
			}
		}
		private ContainerCodes? _defaultContainer = null;


		public virtual List<string> Serialize()
		{
			List<string> list = new();
			list = SerializeBackgroundData(this).ToList();
			foreach (CustomBgElement element in backgroundElements)
			{
				list.Add(element.Serialize() + element.SerializeTags());
			}
			return list;
		}

		public virtual void MakeScene(BackgroundScene self)
		{
			_Scene = self;
			sceneInitialized = true;
			MakeBackgroundElements(self);
		}

		public void MakeBackgroundElements(BackgroundScene self)
		{
			if (backgroundElements.Count != 0)
			{
				self.elements.RemoveAll(x => { if (x.HasInstanceData()) { x.Destroy(); return true; } return false; });

				foreach (CustomBgElement element in backgroundElements)
				{
					try
					{
						var e = element.MakeSceneElement(self);
						e.CData().dataElement = element;
						self.AddElement(e);
					}
					catch (BackgroundBuilderException bgex)
					{
						LogError($"Could not create custom background element: {bgex}");
					}
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
			ParseBackgroundData(this, fileText);
			//foreach (string line in fileText)
			//{ LineToData(line); }
		}
		public virtual void LineToData(string line)
		{

			string[] array2 = Regex.Split(line, ": ");
			if (array2.Length >= 2)
			{
				switch (array2[0])
				{
				case "RotWorm": //these can go in any scene...
				case "SimpleElement":
				case "SimpleIllustration":
					if (TryGetBgElementFromString(line, out CustomBgElement element))
					{ backgroundElements.Add(element); }
					break;
				}
			}

			if (line.StartsWith("REMOVE_"))
			{
				string[] array = Regex.Split(line.Substring(7), ": ");
				if (array.Length < 2) return;

				string removeElement = $"{array[0]}: {array[1]}";
				backgroundElements.RemoveAll(x => x.Serialize() + x.SerializeTags() == removeElement);
			}
		}

		public List<CustomBgElement> backgroundElements = new();

		public bool sceneInitialized = false;

		public BackgroundScene? _Scene;
	}

	public class DayNightSceneData : BGSceneData
	{

		private Color _defaultAtmosphereColor = new Color(0.16078432f, 0.23137255f, 0.31764707f);
		[BackgroundData(backingFieldName = nameof(_atmosphereColor), defaultFieldName = nameof(_defaultAtmosphereColor))]
		public Color atmosphereColor
		{
			get
			{
				return _atmosphereColor ?? 
					((_Scene is AboveCloudsView acv) ? acv.atmosphereColor : 
					(_Scene is RoofTopView rtv) ? rtv.atmosphereColor : _defaultAtmosphereColor);
			}

			set
			{
				_atmosphereColor = value;
				NeedColorUpdate = true;
			}
		}

		[BackgroundData(backingFieldName = nameof(_multiplyColor))]
		public Color multiplyColor
		{
			get => _multiplyColor ?? Color.white;
			set
			{
				_multiplyColor = value;
				NeedColorUpdate = true;
			}
		}

		[BackgroundData(backingFieldName = nameof(_duskAtmosphereColor))]
		public Color duskAtmosphereColor
		{
			get
			{
				if (_duskAtmosphereColor is Color color) return color;
				if (_Scene?.room.game.GetStorySession.saveStateNumber == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
				{
					return new Color(0.7564706f, 0.3756863f, 0.3756863f);
				}
				return new Color(0.5176471f, 0.3254902f, 0.40784314f);
			}
			set
			{
				_duskAtmosphereColor = value;
				NeedColorUpdate = true;
			}
		}

		[BackgroundData(backingFieldName = nameof(_duskMultiplyColor))]
		public Color duskMultiplyColor
		{
			get => _duskAtmosphereColor ?? new Color(1f, 0.79f, 0.47f);
			set
			{
				_duskMultiplyColor = value;
				NeedColorUpdate = true;
			}
		}

		[BackgroundData(backingFieldName = nameof(_nightAtmosphereColor))]
		public Color nightAtmosphereColor
		{
			get => _nightAtmosphereColor ?? new Color(0.04882353f, 0.0527451f, 0.06843138f);
			set
			{
				_nightAtmosphereColor = value;
				NeedColorUpdate = true;
			}
		}

		[BackgroundData(backingFieldName = nameof(_nightMultiplyColor))]
		public Color nightMultiplyColor
		{
			get => _nightMultiplyColor ?? new Color(0.078431375f, 0.14117648f, 0.21176471f);
			set
			{
				_nightMultiplyColor = value;
				NeedColorUpdate = true;
			}
		}

		[BackgroundData]
		public string daySky
		{
			get
			{
				return _daySky ?? 
					((_Scene is AboveCloudsView acv) ? acv.daySky.illustrationName : 
					(_Scene is RoofTopView rtv) ? rtv.daySky.illustrationName : 
					(_Scene is AncientUrbanView auv && (auv.elements.First(x => x is AncientUrbanView.Sky) is AncientUrbanView.Sky sky)) ? sky.illustrationName : "AtC_Sky");
			}

			set
			{
				_daySky = value;
				if (DoesTextureExist(value) && _Scene != null)
				{
					_Scene.LoadGraphic(value, true, true);
					if (_Scene is AboveCloudsView acv) acv.daySky.illustrationName = value;
					if (_Scene is RoofTopView rtv) rtv.daySky.illustrationName = value;
					if (_Scene is AncientUrbanView auv && (auv.elements.First(x => x is AncientUrbanView.Sky) is AncientUrbanView.Sky sky)) sky.illustrationName = value;
				}
			}
		}

		[BackgroundData]
		public string duskSky
		{
			get
			{
				return _duskSky ??
					((_Scene is AboveCloudsView acv) ? acv.duskSky.illustrationName :
					(_Scene is RoofTopView rtv) ? rtv.duskSky.illustrationName : "AtC_Sky");
			}

			set
			{
				_duskSky = value;
				if (DoesTextureExist(value) && _Scene != null)
				{
					_Scene.LoadGraphic(value, true, true);
					if (_Scene is AboveCloudsView acv) acv.duskSky.illustrationName = value;
					if (_Scene is RoofTopView rtv) rtv.duskSky.illustrationName = value;
				}
			}
		}

		[BackgroundData]
		public string nightSky
		{
			get
			{
				return _nightSky ??
					((_Scene is AboveCloudsView acv) ? acv.nightSky.illustrationName :
					(_Scene is RoofTopView rtv) ? rtv.nightSky.illustrationName : "AtC_Sky");
			}

			set
			{
				_daySky = value;
				if (DoesTextureExist(value) && _Scene != null)
				{
					_Scene.LoadGraphic(value, true, true);
					if (_Scene is AboveCloudsView acv) acv.nightSky.illustrationName = value;
					if (_Scene is RoofTopView rtv) rtv.nightSky.illustrationName = value;
				}
			}
		}

		public override void MakeScene(BackgroundScene self)
		{
			base.MakeScene(self);

			if (NeedColorUpdate)
			{
				ColorUpdate();
				NeedColorUpdate = false;
			}
		}

		/// <summary>
		/// this method is mostly copied from AboveCloudsView.Update \ RoofTopView.Update
		/// </summary>
		public void ColorUpdate()
		{
			if (_Scene == null) return;

			//getting the backing fields because if it's using vanilla colors then we can ignore it
			Color? atmoColor = _atmosphereColor;
			Color? mulColor = _multiplyColor;

			RainCycle rainCycle = _Scene.room.world.rainCycle;
			if ((_Scene.room.game.cameras[0].effect_dayNight > 0f && rainCycle.timer >= rainCycle.cycleLength)
				|| (ModManager.Expedition && _Scene.room.game.rainWorld.ExpeditionMode))
			{
				float t = 1320f; //33 seconds

				float duskTimer = Mathf.InverseLerp(0f, t, rainCycle.dayNightCounter);
				float nightTimer = Mathf.InverseLerp(t, t * 2f, rainCycle.dayNightCounter);
				float delayedNightCounter = Mathf.InverseLerp(t, t * 2.25f, rainCycle.dayNightCounter);

				if (0f < duskTimer && duskTimer < 1f)
				{
					if (_atmosphereColor != null || _duskAtmosphereColor != null)
						atmoColor = Color.Lerp(atmosphereColor, duskAtmosphereColor, duskTimer);

					if (_multiplyColor != null || _duskMultiplyColor != null)
						mulColor = Color.Lerp(multiplyColor, duskMultiplyColor, duskTimer);
				}

				else if (duskTimer == 1f && nightTimer < 1f)
				{
					if (_duskAtmosphereColor != null || _nightAtmosphereColor != null)
						atmoColor = Color.Lerp(duskAtmosphereColor, nightAtmosphereColor, nightTimer);

				}
				else if (1f <= nightTimer && _nightAtmosphereColor != null)
				{
					atmoColor = nightAtmosphereColor;
				}

				if (duskTimer == 1f && delayedNightCounter < 1f)
				{
					if (_duskMultiplyColor != null || _nightMultiplyColor != null)
						mulColor = Color.Lerp(duskMultiplyColor, nightMultiplyColor, delayedNightCounter);
				}
				else if (1f <= delayedNightCounter && _nightMultiplyColor != null)
				{
					mulColor = nightMultiplyColor;
				}

			}
			if (atmoColor is Color color)
			{

				if (_Scene is AboveCloudsView acv) acv.atmosphereColor = color;
				if (_Scene is RoofTopView rtv) rtv.atmosphereColor = color;

				Shader.SetGlobalVector(RainWorld.ShadPropAboveCloudsAtmosphereColor, color);
			}

			if (mulColor is Color color2)
			{
				Shader.SetGlobalVector(RainWorld.ShadPropMultiplyColor, color2);
			}
		}

		private bool NeedColorUpdate = false;

		#region backingfields
		private string? _daySky;
		private string? _duskSky;
		private string? _nightSky;

		private Color? _atmosphereColor;
		private Color? _multiplyColor;
		private Color? _duskAtmosphereColor;
		private Color? _duskMultiplyColor;
		private Color? _nightAtmosphereColor;
		private Color? _nightMultiplyColor;
		#endregion

	}

	public class AboveCloudsView_SceneData : DayNightSceneData
	{
		[BackgroundData]
		public float startAltitude 
		{ 
			get => _startAltitude ?? Scene?.startAltitude ?? 20000; 
			set { 
				_startAltitude = value;
				if (Scene != null)
				{
					Scene.startAltitude = value;
					Scene.sceneOrigo = new Vector2(2514f, (startAltitude + endAltitude) / 2f);
				}
			}
		}

		[BackgroundData]
		public float endAltitude 
		{ 
			get => _endAltitude ?? Scene?.endAltitude ?? 31400; 
			set { 
				_endAltitude = value; if (Scene != null)
				{
					Scene.endAltitude = value;
					Scene.sceneOrigo = new Vector2(2514f, (startAltitude + endAltitude) / 2f);
				}
			}
		}

		[BackgroundData]
		public float cloudsStartDepth 
		{ 
			get => _cloudsStartDepth ?? Scene?.cloudsStartDepth ?? 5; 
			set
			{
				redoClouds |= value != cloudsStartDepth; 
				_cloudsStartDepth = value; 
				if (Scene != null) Scene.cloudsStartDepth = value; 
			}
		}

		[BackgroundData]
		public float cloudsEndDepth 
		{ 
			get => _cloudsEndDepth ?? Scene?.cloudsEndDepth ?? 40; 
			set
			{
				redoClouds |= value != cloudsEndDepth;
				_cloudsEndDepth = value; 
				if (Scene != null) Scene.cloudsEndDepth = value; 
			}
		}

		[BackgroundData]
		public float distantCloudsEndDepth 
		{ 
			get => _distantCloudsEndDepth ?? Scene?.distantCloudsEndDepth ?? 200; 
			set 
			{
				redoClouds |= value != distantCloudsEndDepth;
				_distantCloudsEndDepth = value;
				if (Scene != null) Scene.distantCloudsEndDepth = value;
			}
		}

		[BackgroundData]
		public float cloudsCount
		{
			get => _cloudsCount ?? Scene?.elements.Where(x => x is CloseCloud).Count() ?? 7;
			set
			{
				redoClouds |= value != cloudsCount;
				_cloudsCount = value;
			}
		}

		[BackgroundData]
		public float distantCloudsCount
		{
			get => _distantCloudsCount ?? Scene?.elements.Where(x => x is DistantCloud).Count() ?? 11; 
			set
			{
				redoClouds |= value != distantCloudsCount;
				_distantCloudsCount = value;
			}
		}

		[BackgroundData(backingFieldName = nameof(_curveCloudDepth))]
		public float curveCloudDepth
		{
			get => _curveCloudDepth ?? 1;
			set
			{
				redoClouds |= value != curveCloudDepth;
				_curveCloudDepth = value;
			}
		}

		[BackgroundData(backingFieldName = nameof(_overrideYStart))]
		public float overrideYStart
		{
			get => _overrideYStart ?? -40 * cloudsEndDepth; 
			set
			{
				redoClouds |= value != overrideYStart;
				_overrideYStart = value;
			}
		}

		[BackgroundData(backingFieldName = nameof(_overrideYEnd))]
		public float overrideYEnd
		{
			get => _overrideYEnd ?? 0; 
			set
			{
				redoClouds |= value != overrideYEnd;
				_overrideYEnd = value;
			}
		}

		[BackgroundData(backingFieldName = nameof(_windDir))]
		public float windDir
		{
			get => _windDir ?? Shader.GetGlobalFloat("_windDir"); 
			set
			{
				_windDir = value;
				if (Scene != null)
					Shader.SetGlobalFloat("_windDir", value);
			}
		}

		static readonly float _startFogDefault = 18000f;
		[BackgroundData(backingFieldName = nameof(_startFogAltitude), defaultFieldName = nameof(_startFogDefault))]
		public float startFogAltitude
		{
			get
			{
				if (_startFogAltitude != null) return _startFogAltitude.Value;
				if (Scene != null)
				{
					if (Scene.room.game.IsArenaSession)
					{
						if (Scene.PinkSky)
						{ return 5700f; }
						else return 0f;
					}
					if (Scene.PinkSky)
					{ return 5700f; }
					else if (Scene.OEClouds)
					{ return 19887.3f; }
					else if (Scene.SIClouds)
					{ return 9000f; }
				}
				return _startFogDefault;
			}
			set => _startFogAltitude = value;
		}
		static readonly float _endFogDefault = 22000f;
		[BackgroundData(backingFieldName = nameof(_endFogAltitude), defaultFieldName = nameof(_endFogDefault))]
		public float endFogAltitude
		{
			get
			{
				if (_endFogAltitude != null) return _endFogAltitude.Value;
				if (Scene != null)
				{
					if (Scene.room.game.IsArenaSession)
					{
						if (Scene.PinkSky)
						{ return 9000f; }
						else return 0f;
					}
					if (Scene.PinkSky)
					{ return 9000f; }
					else if (Scene.OEClouds)
					{ return 21207.4f; }
					else if (Scene.SIClouds)
					{ return 16000f; }
				}
				return _endFogDefault;
			}
			set => _endFogAltitude = value;
		}

		#region backingfields
		private float? _startAltitude;
		private float? _endAltitude;
		private float? _cloudsStartDepth;
		private float? _cloudsEndDepth;
		private float? _distantCloudsEndDepth;
		private float? _cloudsCount;
		private float? _distantCloudsCount;
		private float? _curveCloudDepth;
		private float? _overrideYStart;
		private float? _overrideYEnd;
		private float? _windDir;
		public float? _startFogAltitude;
		public float? _endFogAltitude;
		#endregion

		public bool redoClouds;

		public AboveCloudsView? Scene { get => _Scene is AboveCloudsView acv ? acv : null; set => _Scene = value; }

		public override void MakeScene(BackgroundScene self)
		{
			if (self is not AboveCloudsView acv) return;

			Scene = acv;

			base.MakeScene(self);

			if (redoClouds)
			{
				RedoClouds(acv);
				redoClouds = false;
			}
		}

		public void RedoClouds(AboveCloudsView scene)
		{
			scene.elements.RemoveAll(x => { if (x is Cloud) { x.Destroy(); return true; } return false; });
			scene.clouds = new();

			for (int i = 0; i < cloudsCount; i++)
			{
				float cloudDepth = i / (cloudsCount - 1f);
				CloseCloud cloud = new CloseCloud(scene, new Vector2(0f, 0f), cloudDepth, i);
				scene.AddElement(cloud);
			}

			for (int j = 0; j < distantCloudsCount; j++)
			{
				float num15 = j / (distantCloudsCount - 1f);
				num15 = Mathf.Pow(num15, curveCloudDepth);
				var dcloud = new DistantCloud(scene, new Vector2(0f, Mathf.Lerp(overrideYStart, overrideYEnd, num15)), num15, j);
				scene.AddElement(dcloud);
			}
		}

		public override List<string> Serialize()
		{
			List<string> lines = new();

			LogMessage("\nCLOUD DEPTHS\nshouldn't go in background file, but useful for positioning\n");
			foreach (DistantCloud cloud in (Scene?.clouds ?? new()).Where(x => x is DistantCloud))
			{
				LogMessage($"distantCloud: {cloud.pos.x}, {cloud.pos.y}, {cloud.depth}, {cloud.distantCloudDepth}");
			}

			return lines.Concat(base.Serialize()).ToList();

		}

		public override void UpdateSceneElement(string message)
		{
			if (Scene == null || Scene.room.game.IsArenaSession) return;

			Scene.startAltitude = startAltitude;
			Scene.endAltitude = endAltitude;

			Scene.sceneOrigo = new Vector2(2514f, (Scene.startAltitude + Scene.endAltitude) / 2f);

			if (message == "CloudAmount")
			{
				RedoClouds(Scene);
			}
		}

		public override void LineToData(string line)
		{
			base.LineToData(line);

			string[] array = Regex.Split(line, ": ");
			if (array.Length < 2) return;

			switch (array[0])
			{
			case "DistantBuilding":
			case "DistantLightning":
			case "FlyingCloud":
			case "HorizonFog":
				if (TryGetBgElementFromString(line, out CustomBgElement element))
				{ backgroundElements.Add(element); }
				break;
			}
		}
	}

	public class RoofTopView_SceneData : DayNightSceneData
	{
		[BackgroundData]
		public float floorLevel
		{
			get => _floorLevel ?? Scene?.floorLevel ?? 26; 
			set
			{
				_floorLevel = value;
				if (Scene != null)
				{
					UpdateFloorLevel(floorLevel - Scene.floorLevel);
					Scene.floorLevel = value;
				}
			}
		}

		[BackgroundData]
		public Vector2? origin
		{
			get => _origin; 
			set
			{
				_origin = value;
				if (Scene is RoofTopView rtv && value is Vector2 v)
				{
					rtv.sceneOrigo = v;
					Shader.SetGlobalVector(RainWorld.ShadPropSceneOrigoPosition, v);
				}
			}
		}

		[BackgroundData]
		public int rubbleCount
		{
			get => _rubbleCount ?? Scene?.elements.Where(x => x is Rubble).Count() ?? 16;
			set
			{
				redoRubble |= value != rubbleCount;
				_rubbleCount = value;
			}
		}

		[BackgroundData]
		public float rubbleStartDepth
		{
			get => _rubbleStartDepth ?? 1.5f;
			set
			{
				redoRubble |= value != rubbleStartDepth;
				_rubbleStartDepth = value;
			}
		}

		[BackgroundData]
		public float rubbleEndDepth
		{
			get => _rubbleEndDepth ?? 8f;
			set
			{
				redoRubble |= value != rubbleEndDepth;
				_rubbleEndDepth = value;
			}
		}

		[BackgroundData]
		public float curveRubbleDepth
		{
			get => _curveRubbleDepth ?? 1.5f;
			set
			{
				redoRubble |= value != curveRubbleDepth;
				_curveRubbleDepth = value;
			}
		}

		#region backingfields
		private Vector2? _origin;
		private bool? lCMode;
		private float? _floorLevel;
		private int? _rubbleCount;
		private float? _rubbleStartDepth;
		private float? _rubbleEndDepth;
		private float? _curveRubbleDepth;
		#endregion

		public bool redoRubble;

		public RoofTopView? Scene { get => _Scene is RoofTopView acv ? acv : null; set => _Scene = value; }

		public override void MakeScene(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) return;

			Scene = rtv;

			base.MakeScene(self);

			if (redoRubble)
			{
				RedoRubble(Scene);
				redoRubble = false;
			}
		}

		public void UpdateFloorLevel(float difference)
		{
			if (Scene == null) return;
			foreach (BackgroundScene.BackgroundSceneElement element in Scene.elements)
			{
				if (element is RoofTopView.DistantBuilding or Building or Floor or RoofTopView.Smoke or Rubble)
				{ element.pos.y += difference; }
			}
		}

		public void RedoRubble(RoofTopView scene)
		{
			scene.elements.RemoveAll(x => { if (x is Rubble) { x.Destroy(); return true; } return false; });

			for (int i = 0; i < rubbleCount; i++)
			{
				float f = (float)i / (rubbleCount - 1);
				scene.AddElement(new RoofTopView.Rubble(scene, "Rf_Rubble", new Vector2(0f, scene.floorLevel), Mathf.Lerp(rubbleStartDepth, rubbleEndDepth, Mathf.Pow(f, curveRubbleDepth)), i));
			}
		}

		public override List<string> Serialize()
		{
			List<string> lines = new();

			if (origin is Vector2 v) lines.Add($"origin: {v.x}, {v.y}");

			return lines.Concat(base.Serialize()).ToList();
		}

		public override void UpdateSceneElement(string message)
		{
			if (Scene == null) return;
		}

		public override void LineToData(string line)
		{
			base.LineToData(line);

			string[] array = Regex.Split(line, ": ");
			if (array.Length < 2) return;

			switch (array[0])
			{
			case "DistantBuilding":
				if (TryGetBgElementFromString("RF_" + line, out CustomBgElement element))
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

	public class AncientUrbanView_SceneData : DayNightSceneData
	{
		[BackgroundData]
		public float floorLevel
		{
			get => _floorLevel ?? Scene?.floorLevel ?? -2000;
			set
			{
				_floorLevel = value;
				if (Scene != null)
				{
					UpdateFloorLevel(floorLevel - Scene.floorLevel);
					Scene.floorLevel = value;
				}
			}
		}

		[BackgroundData]
		public Vector2? origin
		{
			get => _origin;
			set
			{
				_origin = value;
				if (Scene is AncientUrbanView auv && value is Vector2 v)
				{
					auv.sceneOrigo = v;
					Shader.SetGlobalVector(RainWorld.ShadPropSceneOrigoPosition, v);
				}
			}
		}
		#region backingfields
		private Vector2? _origin;
		private float? _floorLevel;
		#endregion

		public bool redoRubble;

		public AncientUrbanView? Scene { get => _Scene is AncientUrbanView acv ? acv : null; set => _Scene = value; }

		public override void MakeScene(BackgroundScene self)
		{
			if (self is not AncientUrbanView auv) return;

			Scene = auv;

			base.MakeScene(self);

		}

		public void UpdateFloorLevel(float difference)
		{
			if (Scene == null) return;
			foreach (BackgroundScene.BackgroundSceneElement element in Scene.elements)
			{
				if (element is RoofTopView.DistantBuilding or Building or Floor or RoofTopView.Smoke or Rubble)
				{ element.pos.y += difference; }
			}
		}

		public override List<string> Serialize()
		{
			List<string> lines = new();

			if (origin is Vector2 v) lines.Add($"origin: {v.x}, {v.y}");

			return lines.Concat(base.Serialize()).ToList();
		}

		public override void UpdateSceneElement(string message)
		{
			if (Scene == null) return;
		}

		public override void LineToData(string line)
		{
			base.LineToData(line);

			string[] array = Regex.Split(line, ": ");
			if (array.Length < 2) return;

			switch (array[0])
			{
			case "Building":
			case "Smoke":
				if (TryGetBgElementFromString("AU_" + line, out CustomBgElement element))
				{ backgroundElements.Add(element); }
				break;
			case "SmokeGradient":
				if (TryGetBgElementFromString(line, out CustomBgElement element2))
				{ backgroundElements.Add(element2); }
				break;
			}
		}
	}

	public class RotWormScene_SceneData : BGSceneData
	{
		public List<RotWormScene.PebbsGrid> theGrid = new();


		private Color _defaultAtmosphereColor = Color.black;
		[BackgroundData(backingFieldName = nameof(_atmosphereColor), defaultFieldName = nameof(_defaultAtmosphereColor))]
		public Color atmosphereColor
		{
			get
			{
				return _atmosphereColor ?? Shader.GetGlobalVector(RainWorld.ShadPropAboveCloudsAtmosphereColor);
			}

			set
			{
				_atmosphereColor = value;
				Shader.SetGlobalVector(RainWorld.ShadPropAboveCloudsAtmosphereColor, value);
			}
		}

		[BackgroundData]
		public int seed
		{
			get => _seed ?? 123;
			set
			{
				redoGrid |= value != seed;
				_seed = value;
			}
		}

		[BackgroundData]
		public float sceneScale
		{
			get => _sceneScale ?? RotWormScene.sceneScale;
			set
			{
				redoGrid |= value != sceneScale;
				_sceneScale = value;
				RotWormScene.sceneScale = value;
			}
		}

		[BackgroundData]
		public float depthScale
		{
			get => _depthScale ?? RotWormScene.depthScale;
			set
			{
				//no redo grid necessary
				_depthScale = value;
				RotWormScene.depthScale = value;
			}
		}
		[BackgroundData]
		public float fogDepth
		{
			get => _fogDepth ?? RotWormScene.fogDepth;
			set
			{
				redoGrid |= value != fogDepth;
				_fogDepth = value;
				RotWormScene.fogDepth = value;
			}
		}

		[BackgroundData]
		public Vector2 perspectiveCenter
		{
			//default is screen center, should adjust for screen size, but eehhh...
			get => _perspectiveCenter ?? ((Scene != null) ? Scene.perspectiveCenter : new Vector2(683f, 384f));
			set
			{
				_perspectiveCenter = value;
				if(Scene != null) Scene.perspectiveCenter = value;
			}
		}

		[BackgroundData]
		public int gridLayers
		{
			get => _gridLayers ?? 8;
			set
			{
				redoGrid |= value != gridLayers;
				_gridLayers = value;
			}
		}
		[BackgroundData]
		public int gridParallelDepth
		{
			get => _gridParallelDepth ?? 24;
			set
			{
				redoGrid |= value != gridParallelDepth;
				_gridParallelDepth = value;
			}
		}
		[BackgroundData]
		public int gridPerpendicularDepth
		{
			get => _gridPerpendicularDepth ?? 2;
			set
			{
				redoGrid |= value != gridPerpendicularDepth;
				_gridPerpendicularDepth = value;
			}
		}

		#region backingfields
		Color? _atmosphereColor;
		private int? _seed;
		private float? _sceneScale;
		private float? _depthScale;
		private float? _fogDepth;
		private Vector2? _perspectiveCenter;
		private int? _gridLayers;
		private int? _gridParallelDepth;
		private int? _gridPerpendicularDepth;
		#endregion

		public bool redoGrid = false;

		public RotWormScene? Scene { get => _Scene is RotWormScene rws ? rws : null; set => _Scene = value; }

		public override void MakeScene(BackgroundScene self)
		{
			if (self is not RotWormScene rws) return;

			theGrid = self.elements.Where(x => x is RotWormScene.PebbsGrid).Select(x => (RotWormScene.PebbsGrid)x).ToList();

			Scene = rws;

			base.MakeScene(self);

			if (redoGrid)
			{
				redoGrid = false;
				RedoGrid(rws);
			}
		}

		public void RedoGrid(RotWormScene scene)
		{
			foreach (var grid in theGrid)
			{
				grid.Destroy();
				scene.elements.Remove(grid);
			}
			theGrid.Clear();

			UnityEngine.Random.State state = UnityEngine.Random.state;
			UnityEngine.Random.InitState(seed);

			try
			{
				float num4 = gridLayers * (gridParallelDepth + gridPerpendicularDepth);
				for (int i = 0; i < gridLayers; i++)
				{
					for (int j = 0; j < gridParallelDepth + gridPerpendicularDepth; j++)
					{
						float depth = (i * (gridParallelDepth + gridPerpendicularDepth) + j) / num4;
						var grid = new RotWormScene.PebbsGrid(scene, 0f, 0f, depth * fogDepth, 1f, j > gridParallelDepth);
						theGrid.Add(grid);
						scene.AddElement(grid);
					}
				}
			}
			finally
			{
				UnityEngine.Random.state = state;
			}
		}

		public override void UpdateSceneElement(string message)
		{
			if (Scene == null) return;
		}

		public override void LineToData(string line)
		{
			base.LineToData(line);

			string[] array = Regex.Split(line, ": ");
			if (array.Length < 2) return;

			switch (array[0])
			{
			case "PebbsGrid":
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

	public static bool DoesTextureExist(string name)
	{
		return Futile.atlasManager.GetAtlasWithName(name) != null || File.Exists(AssetManager.ResolveFilePath("Illustrations" + Path.DirectorySeparatorChar.ToString() + name + ".png"));
	}

	public static bool HasInstanceData(this BackgroundScene.BackgroundSceneElement element)
	{
		if (element is BackgroundScene.Simple2DBackgroundIllustration)
		{
			if (element.scene is AboveCloudsView acv)
			{
				return element != acv.daySky && element != acv.duskSky && element != acv.nightSky;
			}
			else if (element.scene is RoofTopView rtv)
			{
				return element != rtv.daySky && element != rtv.duskSky && element != rtv.nightSky;
			}
		}
		if (element is RotWormScene.PebbsGrid && element.scene.room?.roomSettings.BackgroundData().sceneData is RotWormScene_SceneData data)
		{
			return !data.theGrid.Contains(element);
		}

		return element is SimpleBackgroundElement or BackgroundScene.Simple2DBackgroundIllustration or
			AboveCloudsView.DistantBuilding or DistantLightning or FlyingCloud or HorizonFog or Floor or
			RoofTopView.DistantBuilding or Building or DistantGhost or DustWave or RoofTopView.Smoke or
			AncientUrbanView.Building or AncientUrbanView.Smoke or AncientUrbanView.SmokeGradient or RotWorm;
	}
}
