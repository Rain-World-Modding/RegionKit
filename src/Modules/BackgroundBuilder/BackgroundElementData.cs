using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static RegionKit.Modules.BackgroundBuilder.Data;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class BackgroundElementData
{
	public static bool TryGetBgElementFromString(string line, out CustomBgElement element)
	{
		element = null!;
		string[] array = Regex.Split(line, ": ");
		if (array.Length < 2) return false;

		string[] args = Regex.Split(array[1], ", ");
		if (args.Length < 1) return false;

		try
		{
			switch (array[0])
			{
			case "DistantBuilding":
				element = new ACV_DistantBuilding(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4]));
				break;
			case "DistantLightning":
				element = new ACV_DistantLightning(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4]));
				break;
			case "FlyingCloud":
				element = new ACV_FlyingCloud(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2]), float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]));
				break;

			case "RF_DistantBuilding":
				element = new RTV_DistantBuilding(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4]));
				break;

			case "Floor":
				element = new RTV_Floor(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4]));
				break;

			case "Building":
				element = new RTV_Building(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4]));
				break;

			case "DistantGhost":
				element = new RTV_DistantGhost(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2]));
				break;

			case "DustWave":
				element = new RTV_DustWave(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]));
				break;

			case "Smoke":
				element = new RTV_Smoke(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2]), float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]), bool.Parse(args[6]));
				break;
			}
		}
		catch (Exception e) { LogError($"BackgroundBuilder: error loading background element from string [{line}]\n{e}"); return false; }

		if (array.Length > 2)
		{ Array.Copy(array, 2, element.tags, 0, array.Length - 2); }

		return element != null;
	}

	public static CustomBgElement DataFromElement(this BackgroundScene.BackgroundSceneElement element)
	{
		switch (element)
		{
		case AboveCloudsView.DistantBuilding el:
			return new ACV_DistantBuilding(el.assetName, el.ScenePosToNeutral(), el.depth, el.atmosphericalDepthAdd);

		case AboveCloudsView.DistantLightning el:
			return new ACV_DistantLightning(el.assetName, el.ScenePosToNeutral(), el.depth, el.minusDepthForLayering);

		case AboveCloudsView.FlyingCloud el:
			return new ACV_FlyingCloud(el.ScenePosToNeutral(), el.depth, el.flattened, el.alpha, el.shaderInputColor);

		case RoofTopView.Floor el:
			return new RTV_Floor(el.assetName, el.ScenePosToNeutral(), el.fromDepth, el.toDepth);

		case RoofTopView.DistantBuilding el:
			return new RTV_DistantBuilding(el.assetName, el.ScenePosToNeutral(), el.depth, el.atmosphericalDepthAdd);

		case RoofTopView.Building el:
			return new RTV_Building(el.assetName, el.ScenePosToNeutral(), el.depth, el.scale);

		case RoofTopView.DistantGhost el:
			return new RTV_DistantGhost(el.ScenePosToNeutral(), el.depth);

		case RoofTopView.DustWave el:
			return new RTV_DustWave(el.assetName, el.ScenePosToNeutral(), el.depth);

		case RoofTopView.Smoke el:
			return new RTV_Smoke(el.pos, el.depth, el.flattened, el.alpha, el.shaderInputColor, el.shaderType);

		default:
			throw new BackgroundBuilderException(BackgroundBuilderError.InvalidVanillaBgElement); //this should never happen
		}
	}

	public abstract class CustomBgElement
	{
		public Vector2 pos;

		public float depth;

		public BackgroundScene.BackgroundSceneElement? element;

		public string[] tags;

		protected CustomBgElement(Vector2 pos, float depth)
		{
			this.pos = pos;
			this.depth = depth;
			tags = new string[0];
		}

		public abstract string Serialize(); // Saves data for this individual element

		//public abstract PositionedDevUINode MakeDevUI(); // Called when opening the dev menu to allow editing
		public abstract BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self); // Called when opening the scene

		public abstract void UpdateSceneElement(); // Applies changes to the scene element, called when loaded or the dev UI changes

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

			return new RoofTopView.Floor(rtv, assetName, DefaultNeutralPos(pos, depth), fromDepth, toDepth);
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

			return new RoofTopView.DistantBuilding(rtv, assetName, DefaultNeutralPos(pos, depth), depth, atmoDepthAdd);
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

			return new RoofTopView.Building(rtv, assetName, DefaultNeutralPos(pos, depth), depth, scale);
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
	}

	#endregion RoofTopView

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
