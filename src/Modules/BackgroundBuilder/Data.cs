using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class Data
{

	public static void Apply()
	{
		On.RoomSettings.Load += RoomSettings_Load;
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
			}

			if (array2.Length == 2 && array2[0] == "BackgroundType")
			{ self.BackgroundData().FromName(array2[1]); }
		}
		return true;
	}

	public static bool PathFromName(string name, out string path)
	{
		path = AssetManager.ResolveFilePath(Path.Combine("RegionKit-Backgrounds", name + ".txt"));
		if (File.Exists(path)) return true;
		else { return false; }
	}

	private static readonly ConditionalWeakTable<RoomSettings, RoomBackgroundData> table = new();

	public static RoomBackgroundData BackgroundData(this RoomSettings p) => table.GetValue(p, _ => new RoomBackgroundData());

	public interface IBackgroundData
	{
		public void FromString(string filePath);

		public List<string> ToLines();

		public List<string> backgroundElementText { get; set; }
	}

	public static IBackgroundData? TypeToData(BackgroundTemplateType type)
	{
		if (type == BackgroundTemplateType.AboveCloudsView)
			return new CloudsBackgroundData();

		else if (type == BackgroundTemplateType.RoofTopView)
			return new RoofTopBackgroundData();

		return null;
	}

	public struct GenericBackgroundData
	{ }

	public struct RoofTopBackgroundData : IBackgroundData
	{

		public string daySky;

		public string duskSky;

		public string nightSky;

		public float floorLevel;

		public List<string> backgroundElementText { get; set; }

		public RoofTopBackgroundData()
		{
			daySky = "AtC_Sky";
			duskSky = "AtC_DuskSky";
			nightSky = "AtC_NightSky";
			floorLevel = 26f;
			backgroundElementText = new List<string>();
		}
		public void FromString(string filePath)
		{
			if (!File.Exists(filePath)) return;

			backgroundElementText = new();

			foreach (string line in File.ReadAllLines(filePath))
			{
				Debug.Log("aboveview from string");
				string[] array = Regex.Split(line, ": ");
				if (array.Length < 2) continue;

				switch (array[0])
				{
				case "daySky":
					daySky = array[1];
					break;

				case "duskSky":
					duskSky = array[1];
					break;

				case "nightSky":
					nightSky = array[1];
					break;

				case "floorLevel":
					floorLevel = int.Parse(array[1].Trim());
					break;

				case "DistantBuilding":
					backgroundElementText.Add("RF" + line);
					break;
				case "Building":
				case "Floor":
				case "Rubble":
				case "Smoke":
					backgroundElementText.Add(line);
					break;
				}

			}
		}

		public List<string> ToLines()
		{
			throw new NotImplementedException();
		}
	}
	public struct CloudsBackgroundData : IBackgroundData
	{

		public float? startAltitude;
		public float? endAltitude;
		public float? cloudsStartDepth;
		public float? cloudsEndDepth;
		public float? distantCloudsEndDepth;
		public float? cloudsCount;
		public float? distantCloudsCount;

		public string daySky;

		public string duskSky;

		public string nightSky;

		public List<string> backgroundElementText { get; set; }

		public CloudsBackgroundData()
		{
			startAltitude = null;
			endAltitude = null;
			cloudsStartDepth = null;
			cloudsEndDepth = null;
			distantCloudsEndDepth = null;
			daySky = "AtC_Sky";
			duskSky = "AtC_DuskSky";
			nightSky = "AtC_NightSky";
			backgroundElementText = new();
		}

		public void FromString(string filePath)
		{
			if (!File.Exists(filePath)) return;

			backgroundElementText = new();

			foreach (string line in File.ReadAllLines(filePath))
			{
				Debug.Log("aboveview from string");
				string[] array = Regex.Split(line, ": ");
				if (array.Length < 2) continue;

				switch (array[0])
				{
				case "startAltitude":
					startAltitude = float.Parse(array[1].Trim());
					Debug.Log("strat");
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
					Debug.Log(line);
					backgroundElementText.Add(line);
					break;
				}

			}
		}

		public List<string> ToLines()
		{
			List<string> lines = new List<string>();
			if (startAltitude != null) lines.Add($"startAltitude: {startAltitude}");
			if (endAltitude != null) lines.Add($"endAltitude: {endAltitude}");
			if (cloudsStartDepth != null) lines.Add($"cloudsStartDepth: {cloudsStartDepth}");
			if (cloudsEndDepth != null) lines.Add($"cloudsEndDepth: {cloudsEndDepth}");
			if (distantCloudsEndDepth != null) lines.Add($"distantCloudsEndDepth: {distantCloudsEndDepth}");

			return lines;
		}
	}

	public class RoomBackgroundData
	{
		public string backgroundName;

		public Vector2 roomOffset;

		public Vector2 backgroundOffset;

		public BackgroundTemplateType type;

		public IBackgroundData? backgroundData;
		public RoomBackgroundData()
		{
			roomOffset = Vector2.zero;
			backgroundOffset = Vector2.zero;
			type = BackgroundTemplateType.None;
			backgroundData = null;
			backgroundName = "";
		}

		public void FromName(string name)
		{
			if (PathFromName(name, out string path))
			{
				backgroundName = name;
				FromFilePath(path);
			}
		}

		public void FromFilePath(string filePath)
		{
			if (!File.Exists(filePath)) return;

			foreach (string line in File.ReadAllLines(filePath))
			{
				string[] array = Regex.Split(line, ": ");
				if (array.Length != 2) continue;

				switch (array[0])
				{
				case "Type":
					Debug.Log("type is " + array[1]);
					type = (BackgroundTemplateType)ExtEnumBase.Parse(typeof(BackgroundTemplateType), array[1], false);
					backgroundData = TypeToData(type);
					backgroundData?.FromString(filePath);
					break;
				}
			}
		}

		public void ToString()
		{
			List<string> lines = new List<string>();
			lines.Add($"Type: {type}");
			if (backgroundOffset.x != 0f) lines.Add($"OffsetX: {backgroundOffset.x}");
			if (backgroundOffset.y != 0f) lines.Add($"OffsetY: {backgroundOffset.y}");


		}
	}

	public class BackgroundTemplateType : ExtEnum<BackgroundTemplateType>
	{
		public BackgroundTemplateType(string value, bool register = false) : base(value, register)
		{
		}
		public static readonly BackgroundTemplateType None = new BackgroundTemplateType("None", true);

		public static readonly BackgroundTemplateType AboveCloudsView = new BackgroundTemplateType("AboveCloudsView", true);

		public static readonly BackgroundTemplateType RoofTopView = new BackgroundTemplateType("RoofTopView", true);

		public static readonly BackgroundTemplateType VoidSeaScene = new BackgroundTemplateType("VoidSeaScene", true);
	}

	public static Vector2 PosToDrawPos(Vector2 input, float depth)
	{
		return input / depth;
	}

	public static string ToDataString(this BackgroundScene.BackgroundSceneElement element)
	{
		Vector2 pos = PosToDrawPos(element.pos, element.depth);
		switch (element)
		{
		case AboveCloudsView.DistantBuilding el:
			return $"DistantBuilding: {el.assetName}, {pos.x}, {pos.y}, {el.depth}, {el.atmosphericalDepthAdd}";

		case AboveCloudsView.DistantLightning el:
			return $"DistantLightning: {el.assetName}, {pos.x}, {pos.y}, {el.depth}, {el.minusDepthForLayering}";

		case AboveCloudsView.FlyingCloud el:
			return $"FlyingCloud: {pos.x}, {pos.y}, {el.depth}, {el.index}, {el.flattened}, {el.alpha}, {el.shaderInputColor}";

		default:
			return "";
		}
	}

	public static List<string> elementsToString(BackgroundScene self)
	{
		List<string> result = new List<string>();
		foreach (BackgroundScene.BackgroundSceneElement element in self.elements)
		{
			string elementText = element.ToDataString();
			if (elementText != "") result.Add(elementText);
		}
		return result;
	}
}
