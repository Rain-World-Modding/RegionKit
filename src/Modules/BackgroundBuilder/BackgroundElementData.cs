using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static RegionKit.Modules.BackgroundBuilder.Data;
using static RegionKit.Modules.BackgroundBuilder.CustomBackgroundElements;
using System.Globalization;
using Watcher;

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
				"SimpleElement" => new BG_SimpleElement(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3])),
				"SimpleIllustration" => new BG_Illustration(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3])),
				"FlyingCloud" => new ACV_FlyingCloud(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2]), float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5])),
				"HorizonFog" => new ACV_HorizonFog(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3])),
				"RF_DistantBuilding" => new RTV_DistantBuilding(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4])),
				"Floor" => new RTV_Floor(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4])),
				"Building" => new RTV_Building(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4])),
				"DistantGhost" => new RTV_DistantGhost(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2])),
				"DustWave" => new RTV_DustWave(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3])),
				"Smoke" => new RTV_Smoke(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2]), float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]), bool.Parse(args[6])),
				"AU_Smoke" => new RTV_Smoke(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2]), float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]), bool.Parse(args[6])),
				"AU_Building" => new AUV_Building(args[0], new Vector2(float.Parse(args[1]), float.Parse(args[2])), float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]), float.Parse(args[6])),
				"SmokeGradient" => new AUV_SmokeGradient(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2])),
				"PebbsGrid" => new RWS_PebbsGrid(new Vector2(float.Parse(args[0]), float.Parse(args[1])), float.Parse(args[2]), float.Parse(args[3]), bool.Parse(args[4])),
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
			BackgroundScene.AdditiveBackgroundIllustration el => new BG_Illustration(el.illustrationName, el.pos, el.depth) { spriteShader = "BackgroundAdditive" },
			BackgroundScene.Simple2DBackgroundIllustration el => new BG_Illustration(el.illustrationName, el.pos, el.depth),
			SimpleBackgroundElement el => new BG_SimpleElement(el.assetName, el.pos, el.depth),
			AboveCloudsView.DistantBuilding el => new ACV_DistantBuilding(el.assetName, el.ScenePosToNeutral(), el.depth, el.atmosphericalDepthAdd) 
			{ spriteScale = el.scale == 1f ? null : new(el.scale, el.scale) }, //if scale isn't default, set it here
			AboveCloudsView.DistantLightning el => new ACV_DistantLightning(el.assetName, el.ScenePosToNeutral(), el.depth, el.minusDepthForLayering),
			AboveCloudsView.FlyingCloud el => new ACV_FlyingCloud(el.ScenePosToNeutral(), el.depth, el.flattened, el.alpha, el.shaderInputColor),
			AboveCloudsView.HorizonFog el => new ACV_HorizonFog(el.illustrationName, el.ScenePosToNeutral(), el.depth),
			RoofTopView.Floor el => new RTV_Floor(el.assetName, el.ScenePosToNeutral(), el.fromDepth, el.toDepth),
			RoofTopView.DistantBuilding el => new RTV_DistantBuilding(el.assetName, el.ScenePosToNeutral(), el.depth, el.atmosphericalDepthAdd),
			RoofTopView.Building el => new RTV_Building(el.assetName, el.ScenePosToNeutral(), el.depth, el.scale),
			RoofTopView.DistantGhost el => new RTV_DistantGhost(el.ScenePosToNeutral(), el.depth),
			RoofTopView.DustWave el => new RTV_DustWave(el.assetName, el.ScenePosToNeutral(), el.depth),
			RoofTopView.Smoke el => new RTV_Smoke(el.pos, el.depth, el.flattened, el.alpha, el.shaderInputColor, el.shaderType),
			AncientUrbanView.Smoke el => new RTV_Smoke(el.pos, el.depth, el.flattened, el.alpha, el.shaderInputColor, el.shaderType),
			AncientUrbanView.SmokeGradient el => new AUV_SmokeGradient(el.pos, el.depth),
			AncientUrbanView.Building el => new AUV_Building(el.assetName, el.pos, el.depth, el.scale, el.rotation, el.thickness),
			RotWorm el => new RWS_RotWorm(el.pos, el.depth),
			RotWormScene.PebbsGrid el => new RWS_PebbsGrid(el.pos / RotWormScene.sceneScale, el.depth, el.scale, el.perpendicular),
			_ => throw new BackgroundBuilderException(BackgroundBuilderError.InvalidVanillaBgElement),//this should never happen
		};
	}

	public abstract class CustomBgElement
	{
		public Vector2 pos;

		public float depth;

		public Vector2? anchorPos = null;
		public Vector2? spriteScale = null;
		public float? spriteAlpha = null;
		public string? spriteShader = null;
		public Color? spriteColor = null;
		public ContainerCodes? container = null;
		public bool lockX = false;
		public bool lockY = false;

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
			if (spriteScale is Vector2 f) 
			{ 
				if(f.x == f.y) tags.Add($"scale|{f.x}");
				else tags.Add($"scale|{f.x}, {f.y}"); 
			}
			if (spriteAlpha is float a) tags.Add($"alpha|{a}");
			if (spriteColor is Color o) tags.Add($"color|{Custom.colorToHex(o)}");
			if (spriteShader is string s) tags.Add($"shader|{s}");
			if (container is ContainerCodes c) tags.Add($"container|{c}");
			if (lockX is true) tags.Add($"lock|X");
			if (lockY is true) tags.Add($"lock|Y");
			if (tags.Count == 0) return "";
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
			case "anchor":
				string[] array2 = Regex.Split(value, ",").Select(p => p.Trim()).ToArray();
				if (array2.Length >= 2 && float.TryParse(array2[0], out float x) && float.TryParse(array2[1], out float y))
				{
					anchorPos = new(x, y);
					return true;
				}
				else return false;
			case "scale":
				if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float f))
				{ spriteScale = new(f, f); return true; }

				string[] array3 = Regex.Split(value, ",").Select(p => p.Trim()).ToArray();
				if (array3.Length >= 2 && float.TryParse(array3[0], out float x2) && float.TryParse(array3[1], out float y2))
				{
					spriteScale = new(x2, y2);
					return true;
				}
				return false;
			case "alpha":
				spriteAlpha = float.Parse(value);
				return true;
			case "shader":
				spriteShader = value;
				return true;
			case "color":
				spriteColor = hexToColor(value);
				return true;
			case "container":
				if (Enum.TryParse(value, false, out ContainerCodes result))
				{
					container = result;
					return true;
				}
				return false;
			case "lock":
				if (value == "X")
				{ lockX = true; return true; }
				if (value == "Y")
				{ lockY = true; return true; }
				return false;
			default: return false;
			}
		}

		public virtual void UpdateElementSprites(BackgroundScene.BackgroundSceneElement self, RoomCamera.SpriteLeaser sLeaser)
		{
			foreach (FSprite sprite in sLeaser.sprites)
			{
				if (anchorPos is Vector2 anchor)
				{
					sprite.SetAnchor(anchor);
				}
				if (spriteScale is Vector2 scale)
				{
					sprite.scaleX = scale.x;
					sprite.scaleY = scale.y;
					if (self is RoofTopView.Building building)
					{
						sLeaser.sprites[0].color = new Color(building.elementSize.x * building.scale / 4000f, building.elementSize.y * building.scale / 1500f, 1f / (self.depth / 20f));
					}
				}

				if (spriteAlpha is float a)
				{
					sprite.alpha = a;
				}
				if (spriteColor is Color c)
				{
					sprite.color = c;
				}
				if (spriteShader is string s && self.room?.game != null)
				{
					sprite.shader = self.room.game.rainWorld.Shaders[s];
				}
				if (lockX)
				{ sprite.x = self.pos.x; }
				if (lockY)
				{ sprite.y = self.pos.y; }
			}
		}
	}

	public class BG_SimpleElement : CustomBgElement
	{
		string assetName;
		public BG_SimpleElement(string assetName, Vector2 pos, float depth) : base(pos, depth)
		{
			this.assetName = assetName;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			return new SimpleBackgroundElement(self, assetName, pos, depth);
		}

		public override string Serialize() => $"SimpleElement: {assetName}, {pos.x}, {pos.y}, {depth}";

		public override void UpdateSceneElement()
		{

		}
	}
	public class BG_Illustration : CustomBgElement
	{
		string illustrationName;
		public BG_Illustration(string illustrationName, Vector2 pos, float depth) : base(pos, depth)
		{
			this.illustrationName = illustrationName;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			return new BackgroundScene.Simple2DBackgroundIllustration(self, illustrationName, pos) { depth = depth };
		}

		public override string Serialize() => $"SimpleIllustration: {illustrationName}, {pos.x}, {pos.y}, {depth}";

		public override void UpdateSceneElement()
		{

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
	public class ACV_HorizonFog : CustomBgElement
	{
		//int index; is always zero
		string illustrationName;

		public ACV_HorizonFog(string illustrationName, Vector2 pos, float depth) : base(pos, depth)
		{
			this.illustrationName = illustrationName;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AboveCloudsView acv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new AboveCloudsView.HorizonFog(acv, illustrationName, pos, depth);
		}

		public override string Serialize() => $"HorizonFog: {illustrationName}, {pos.x}, {pos.y}, {depth}";

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
			if (self is RoofTopView rtv) return new RoofTopView.Smoke(rtv, new Vector2(pos.x, rtv.floorLevel + pos.y), depth, 0, flattened, alpha, shaderInputColor, shaderType);
			if (self is AncientUrbanView auv) return new AncientUrbanView.Smoke(auv, new Vector2(pos.x, auv.floorLevel + pos.y), depth, 0, flattened, alpha, shaderInputColor, shaderType);
			throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);
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

	public class AUV_Building : CustomBgElement
	{

		public string assetName;

		public float scale;

		public float rotation;

		public float thickness;

		public AUV_Building(string assetName, Vector2 pos, float depth, float scale, float rotation, float thickness) : base(pos, depth)
		{
			this.assetName = assetName;
			this.scale = scale;
			this.rotation = rotation;
			this.thickness = thickness;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AncientUrbanView auv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);
			return new AncientUrbanView.Building(auv, assetName, new Vector2(DefaultNeutralPos(new Vector2(pos.x, pos.y), depth).x, auv.floorLevel + pos.y), depth, scale, rotation, thickness);
		}

		public override string Serialize() => $"Building: {assetName}, {pos.x}, {pos.y}, {depth}, {scale}, {rotation}, {thickness}";

		public override void UpdateSceneElement()
		{

		}
	}
	public class AUV_SmokeGradient : CustomBgElement
	{
		public AUV_SmokeGradient(Vector2 pos, float depth) : base(pos, depth)
		{
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AncientUrbanView auv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);
			return new AncientUrbanView.SmokeGradient(auv, new Vector2(DefaultNeutralPos(new Vector2(pos.x, pos.y), depth).x, auv.floorLevel + pos.y), depth);
		}

		public override string Serialize() => $"SmokeGradient: {pos.x}, {pos.y}, {depth}";

		public override void UpdateSceneElement()
		{

		}
	}

	public class RWS_RotWorm : CustomBgElement
	{
	public RWS_RotWorm(Vector2 pos, float depth) : base(pos, depth)
		{
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			return new RotWorm(self, pos, depth);
		}

		public override string Serialize() => $"RotWorm: {pos.x}, {pos.y}, {depth}";

		public override void UpdateSceneElement()
		{

		}
	}
	public class RWS_PebbsGrid : CustomBgElement
	{
		float scale;
		bool perpendicular;
		public RWS_PebbsGrid(Vector2 pos, float depth, float scale, bool perpendicular) : base(pos, depth)
		{
			this.scale = scale;
			this.perpendicular = perpendicular;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RotWormScene rws) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);
			return new RotWormScene.PebbsGrid(rws, pos.x, pos.y, depth, scale, perpendicular);
		}

		public override string Serialize() => $"PebbsGrid: {pos.x}, {pos.y}, {depth}, {scale}, {perpendicular}";

		public override void UpdateSceneElement()
		{

		}

	}

	/// <summary>
	/// A simplification of BackgroundScene.PosFromDrawPosAtNeutralCam
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
