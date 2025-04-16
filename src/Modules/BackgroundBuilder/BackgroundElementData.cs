using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static RegionKit.Modules.BackgroundBuilder.Data;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class BackgroundElementData
{
	public static bool TryGetBgElementFromString(string line, out CustomBgElement element)
	{
		element = null!;
		string[] array = Regex.Split(line, ":").Select(p => p.Trim()).ToArray();
		if (array.Length < 2) return false;

		string[] args = Regex.Split(array[1], ", ");
		if (args.Length < 1) return false;

		try
		{
			element = array[0] switch
			{
				"DistantBuilding" => new ACV_DistantBuilding(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4])),
				"DistantLightning" => new ACV_DistantLightning(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4])),
				"FlyingCloud" => new ACV_FlyingCloud(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2]), float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5])),
				"RF_DistantBuilding" => new RTV_DistantBuilding(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4])),
				"Floor" => new RTV_Floor(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4])),
				"Building" => new RTV_Building(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4])),
				"DistantGhost" => new RTV_DistantGhost(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2])),
				"DustWave" => new RTV_DustWave(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3])),
				"Smoke" => new RTV_Smoke(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2]), float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]), bool.Parse(args[6])),
				_ => null!
			};
		}
		catch (Exception e) { LogError($"BackgroundBuilder: error loading background element from string [{line}]\n{e}"); return false; }

		if (array.Length > 2)
		{ element.ParseExtraTags(array.Skip(2).ToList()); }

		return element != null;
	}

	public static CustomBgElement DataFromElement(this BackgroundScene.BackgroundSceneElement element)
	{
		return element switch
		{
			AboveCloudsView.DistantBuilding el => new ACV_DistantBuilding(el.assetName, el.ScenePosToNeutral(), el.depth, el.atmosphericalDepthAdd),
			AboveCloudsView.DistantLightning el => new ACV_DistantLightning(el.assetName, el.ScenePosToNeutral(), el.depth, el.minusDepthForLayering),
			AboveCloudsView.FlyingCloud el => new ACV_FlyingCloud(el.ScenePosToNeutral(), el.depth, el.flattened, el.alpha, el.shaderInputColor),
			RoofTopView.Floor el => new RTV_Floor(el.assetName, el.ScenePosToNeutral(), el.fromDepth, el.toDepth),
			RoofTopView.DistantBuilding el => new RTV_DistantBuilding(el.assetName, el.ScenePosToNeutral(), el.depth, el.atmosphericalDepthAdd),
			RoofTopView.Building el => new RTV_Building(el.assetName, el.ScenePosToNeutral(), el.depth, el.scale),
			RoofTopView.DistantGhost el => new RTV_DistantGhost(el.ScenePosToNeutral(), el.depth),
			RoofTopView.DustWave el => new RTV_DustWave(el.assetName, el.ScenePosToNeutral(), el.depth),
			RoofTopView.Smoke el => new RTV_Smoke(el.pos, el.depth, el.flattened, el.alpha, el.shaderInputColor, el.shaderType),
			_ => throw new BackgroundBuilderException(BackgroundBuilderError.InvalidVanillaBgElement),//this should never happen
		};
	}

	public abstract class CustomBgElement
	{
		public Vector2 pos;

		public float depth;

		public Vector2? anchorPos = null;

		public float? spriteScale = null;

		public ContainerCodes? container = null;

		public BackgroundScene.BackgroundSceneElement? element = null;

		public List<string> unrecognizedTags;

		protected CustomBgElement(Vector2 pos, float depth)
		{
			this.pos = pos;
			this.depth = depth;
			unrecognizedTags = new();
		}

		public abstract string Serialize(); // Saves data for this individual element

		public virtual string SerializeTags()
		{
			List<string> tags = new();
			if (anchorPos is Vector2 v) tags.Add($"anchor|{v.x}, {v.y}");
			if (spriteScale is float f) tags.Add($"scale|{f}");
			if (container is ContainerCodes c) tags.Add($"container|{c}");
			return " : " + string.Join(" : ", tags.Concat(unrecognizedTags));
		}

		//public abstract PositionedDevUINode MakeDevUI(); // Called when opening the dev menu to allow editing
		public abstract BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self); // Called when opening the scene

		public abstract void UpdateSceneElement(); // Applies changes to the scene element, called when loaded or the dev UI changes

		public virtual void ParseExtraTags(List<string> tags)
		{
			//this logic is bad still, need to find a better system (or just use reflection :3)
			foreach (string tag in tags)
			{
				string[] split = tag.Split('|');
				if (split.Length >= 2 && ParseTag(split[0], split[1]))
				{
					continue;
				}
				unrecognizedTags.Add(tag);
			}
		}

		public virtual bool ParseTag(string tag, string value)
		{
			switch (tag.ToLower())
			{
			case "scale":
				spriteScale = float.Parse(value);
				return true;

			case "anchor":
				string[] array2 = Regex.Split(value, ",").Select(p => p.Trim()).ToArray();
				if (array2.Length >= 2 && float.TryParse(array2[0], out float x) && float.TryParse(array2[1], out float y))
				{
					anchorPos = new(x, y);
					return true;
				}
				else return false;
			case "container":
				if (Enum.TryParse(value, false, out ContainerCodes result))
				{
					container = result;
					return true;
				}
				return false;
			default: return false;
			}
		}
	}
	#region AboveCloudsView
	public class ACV_DistantBuilding : CustomBgElement
	{
		public string assetName;

		public float atmoDepthAdd;

		public ACV_DistantBuilding(string assetName, Vector2 pos, float depth, float atmoDepthAdd) : base(pos, depth)
		{
			this.assetName = assetName;
			this.atmoDepthAdd = atmoDepthAdd;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AboveCloudsView acv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new AboveCloudsView.DistantBuilding(acv, assetName, DefaultNeutralPos(pos, depth), depth, atmoDepthAdd);
		}

		public override string Serialize() => $"DistantBuilding: {assetName}, {pos.x}, {pos.y}, {depth}, {atmoDepthAdd}";

		public override void UpdateSceneElement()
		{
			
		}
	}

	public class ACV_DistantLightning : CustomBgElement
	{
		public string assetName;

		public float minusDepthForLayering;

		public ACV_DistantLightning(string assetName, Vector2 pos, float depth, float minusDepthForlayering) : base(pos, depth)
		{
			this.assetName = assetName;
			this.minusDepthForLayering = minusDepthForlayering;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AboveCloudsView acv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new AboveCloudsView.DistantLightning(acv, assetName, DefaultNeutralPos(pos, depth), depth, minusDepthForLayering);
		}

		public override string Serialize() => $"DistantLightning: {assetName}, {pos.x}, {pos.y}, {depth}, {minusDepthForLayering}";

		public override void UpdateSceneElement()
		{

		}
	}

	public class ACV_FlyingCloud : CustomBgElement
	{
		//int index; is always zero
		float flattened;
		float alpha;
		float shaderInputColor;

		public ACV_FlyingCloud(Vector2 pos, float depth, float flattened, float alpha, float shaderInputColor) : base(pos, depth)
		{
			this.flattened = flattened;
			this.alpha = alpha;
			this.shaderInputColor = shaderInputColor;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AboveCloudsView acv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new AboveCloudsView.FlyingCloud(acv, DefaultNeutralPos(pos, depth), depth, 0, flattened, alpha, shaderInputColor);
		}

		public override string Serialize() => $"FlyingCloud: {pos.x}, {pos.y}, {depth}, {flattened}, {alpha}, {shaderInputColor}";

		public override void UpdateSceneElement()
		{

		}
	}
	#endregion AboveCloudsView

	#region RoofTopView
	public class RTV_Floor : CustomBgElement
	{
		public string assetName;

		public float fromDepth;

		public float toDepth;

		public RTV_Floor(string assetName, Vector2 pos, float fromDepth, float toDepth) : base(pos, toDepth)
		{
			this.assetName = assetName;
			this.fromDepth = fromDepth;
			this.toDepth = toDepth;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new RoofTopView.Floor(rtv, assetName, new Vector2(0f, rtv.floorLevel) + pos, fromDepth, toDepth);
		}

		public override string Serialize() => $"Floor: {assetName}, {pos.x}, {pos.y}, {fromDepth}, {toDepth}";

		public override void UpdateSceneElement()
		{

		}
	}
	public class RTV_DistantBuilding : CustomBgElement
	{
		public string assetName;

		public float atmoDepthAdd;

		public RTV_DistantBuilding(string assetName, Vector2 pos, float depth, float atmoDepthAdd) : base(pos, depth)
		{
			this.assetName = assetName;
			this.atmoDepthAdd = atmoDepthAdd;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new RoofTopView.DistantBuilding(rtv, assetName, new Vector2(DefaultNeutralPos(pos, depth).x, rtv.floorLevel + pos.y), depth, atmoDepthAdd);
		}

		public override string Serialize() => $"DistantBuilding: {assetName}, {pos.x}, {pos.y}, {depth}, {atmoDepthAdd}";

		public override void UpdateSceneElement()
		{

		}
	}
	public class RTV_Building : CustomBgElement
	{
		public string assetName;

		public float scale;

		public RTV_Building(string assetName, Vector2 pos, float depth, float scale) : base(pos, depth)
		{
			this.assetName = assetName;
			this.scale = scale;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);
			return new RoofTopView.Building(rtv, assetName, new Vector2(DefaultNeutralPos(new Vector2(pos.x, pos.y), depth).x, rtv.floorLevel + pos.y), depth, scale);
		}

		public override string Serialize() => $"Building: {assetName}, {pos.x}, {pos.y}, {depth}, {scale}";

		public override void UpdateSceneElement()
		{

		}
	}
	public class RTV_DistantGhost : CustomBgElement
	{
		public RTV_DistantGhost(Vector2 pos, float depth) : base(pos, depth)
		{
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new RoofTopView.DistantGhost(rtv, DefaultNeutralPos(pos, depth), depth, 0);
		}

		public override string Serialize() => $"DistantBuilding: {pos.x}, {pos.y}, {depth}";

		public override void UpdateSceneElement()
		{

		}
	}
	public class RTV_DustWave : CustomBgElement
	{
		public string assetName;

		public RTV_DustWave(string assetName, Vector2 pos, float depth) : base(pos, depth)
		{
			this.assetName = assetName;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new RoofTopView.DustWave(rtv, assetName, new Vector2(pos.x, rtv.floorLevel + pos.y), depth, 0f);
		}

		public override string Serialize() => $"DustWave: {assetName}, {pos.x}, {pos.y}, {depth}";

		public override void UpdateSceneElement()
		{

		}
	}
	public class RTV_Smoke : CustomBgElement
	{
		float flattened;
		float alpha;
		float shaderInputColor;
		bool shaderType;
		public string? spriteName = null;

		public RTV_Smoke(Vector2 pos, float depth, float flattened, float alpha, float shaderInputColor, bool shaderType) : base(pos, depth)
		{
			this.flattened = flattened;
			this.alpha = alpha;
			this.shaderInputColor = shaderInputColor;
			this.shaderType = shaderType;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new RoofTopView.Smoke(rtv, new Vector2(pos.x, rtv.floorLevel + pos.y), depth, 0, flattened, alpha, shaderInputColor, shaderType);
		}

		public override string Serialize() => $"Smoke: {pos.x}, {pos.y}, {depth}, {flattened}, {alpha}, {shaderInputColor}, {shaderType}";

		public override void UpdateSceneElement()
		{

		}

		public override bool ParseTag(string tag, string value)
		{
			if (tag.ToLower() == "spritename")
			{
				spriteName = value;
				return true;
			}
			return base.ParseTag(tag, value);
		}
	}

	#endregion RoofTopView


	/// <summary>
	/// A simplification of BackgroundBuilder.PosFromDrawPosAtNeutralCam
	/// </summary>
	public static Vector2 DefaultNeutralPos(Vector2 pos, float depth) => pos * depth;

	public static Vector2 ScenePosToNeutral(this BackgroundScene.BackgroundSceneElement element)
	{
		switch (element)
		{
		case AboveCloudsView.DistantBuilding:
		case AboveCloudsView.FlyingCloud:
			return element.pos / element.depth;

		case AboveCloudsView.DistantLightning el:
			return element.pos / (el.restoredDepth? element.depth : element.depth + el.minusDepthForLayering);

		case RoofTopView.DistantBuilding:
		case RoofTopView.Building:
			return new Vector2(element.pos.x / element.depth, element.pos.y - (element.scene as RoofTopView)!.floorLevel);

		case RoofTopView.Floor:
		case RoofTopView.DustWave:
		case RoofTopView.Smoke:
			return new Vector2(element.pos.x, element.pos.y - (element.scene as RoofTopView)!.floorLevel);

		default:
			return element.pos;
		}
	}


}
