using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DevInterface;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using Watcher;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class BackgroundElementData
{
	public static CustomBgElement MakeBlank(string line, Data.BGSceneData data)
	{
		line = data.PrependAlias(line) + line;
		return Registry.ElementMakerRegistry[line].Invoke();
	}

	public static bool TryMakeDataFromElement(this BackgroundScene.BackgroundSceneElement element, Data.BGSceneData data, out CustomBgElement dataElement)
	{
		dataElement = null!;
		if (!Registry.SceneElementString.TryGetValue(element.GetType(), out string s))
		{
			return false;
		}
		dataElement = MakeBlank(s, data);
		dataElement.DataFromElement(element);
		return true;
	}

	public static bool TryGetBgElementFromString(string line, Data.BGSceneData data, out CustomBgElement element)
	{
		element = null!;

		string[] array = Regex.Split(line, ":").Select(p => p.Trim()).ToArray();
		if (array.Length < 2) return false;

		string[] args = Regex.Split(array[1], ", ");
		if (args.Length < 1) return false;

		try
		{
			if (!Registry.ElementMakerRegistry.ContainsKey(data.PrependAlias(array[0]) + array[0]))
				return false;

			element = MakeBlank(array[0], data);
			element.DataFromArgs(args);
		}
		catch (Exception e) { LogError($"BackgroundBuilder: error loading background element from string [{line}]\n{e}"); return false; }

		if (array.Length > 2)
		{ element.ParseExtraTags(array.Skip(2).ToList()); }

		return element != null;
	}

	public abstract class CustomBgElement
	{
		public CustomBgElement()
		{
			pos = Vector2.zero;
			depth = 100f;
		}

		public abstract void DataFromArgs(string[] args);
		public virtual void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			pos = element.pos;
			depth = element.depth;
		}

		protected Vector2 pos;

		protected float depth;

		public bool deleted = false;

		public Vector2? anchorPos = null;
		public Vector2? spriteScale = null;
		public float? spriteAlpha = null;
		public string? spriteShader = null;
		public Color? spriteColor = null;
		public ContainerCodes? container = null;
		public bool lockX = false;
		public bool lockY = false;

		public BackgroundScene.BackgroundSceneElement? sceneElement = null;

		public List<string> unrecognizedTags = new();

		public BG_ElementGroup? group = null;

		public virtual Vector2 Pos
		{
			get
			{
				return pos + (group?.Pos ?? Vector2.zero);
			}

			set
			{
				pos = value - (group?.Pos ?? Vector2.zero);
				if (sceneElement != null)
				{
					sceneElement.pos = ScenePos;
				}
			}
		}

		public virtual float Depth
		{
			get
			{
				return depth + (group?.Depth ?? 0);
			}

			set
			{
				depth = value - (group?.Depth ?? 0);
				if (sceneElement != null)
				{
					sceneElement.CData().DepthUpdate = true;
					sceneElement.depth = Depth;
					Pos = Pos;
				}
			}
		}

		public abstract string Serialize(); // Saves data for this individual element

		public virtual string SerializeTags()
		{
			List<string> tags = new();
			if (anchorPos is Vector2 v) tags.Add($"anchor|{v.x}, {v.y}");
			if (spriteScale is Vector2 f)
			{
				if (f.x == f.y) tags.Add($"scale|{f.x}");
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

		public abstract string DevUIName();

		//public abstract PositionedDevUINode MakeDevUI(); // Called when opening the dev menu to allow editing
		public abstract BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self); // Called when opening the scene

		public virtual Vector2 ScenePos => DefaultNeutralPos(Pos, Depth);

		public virtual Vector2 ReverseScenePos(Vector2 movement) => movement;

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
				if (float.TryParse(value, out float f))
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

		public virtual Vector2 DevUIHandlePos(RoomCamera rCam)
		{
			if (sceneElement != null)
				return sceneElement.DrawPos(rCam.pos, rCam.hDisplace);
			return Pos;
		}

		public virtual void Dragged(Vector2 movement, bool controlPressed)
		{
			if (controlPressed)
			{
				Depth += movement.y;
				//if (sceneElement is AboveCloudsView.DistantBuilding dbuilding)
				//{

				//	dbuilding.atmosphericalDepthAdd += movement.x;
				//}

				//else if (sceneElement is RoofTopView.DistantBuilding rfdbuilding)
				//{
				//	rfdbuilding.atmosphericalDepthAdd += movement.x;
				//}

			}
			else
			{
				Pos += ReverseScenePos(movement);
			}
		}

		public static readonly Vector2 DefaultPanelSize = new Vector2(200f, 86f);
		public virtual BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = new BackgroundPage.ElementDataPanel(owner, "", parent, pos, size, this);
			panel.AddLabelledUINode("Asset: ", 40f, new BackgroundPage.ElementAssetSelectButton(owner, "asset", panel, Vector2.zero, 148f, "...", "Select Asset", BackgroundPage.ElementAssetSelectPanel.SelectGroup.Illustrations));
			panel.NewRow();
			panel.AddLabelledUINode("Group: ", 40f, new ElementGroupSelectButton(owner, "group", panel, Vector2.zero, 148f, group!));
			panel.AddLabelledStringControl("X: ", this.pos.x.ToString(), "pos_x", 16f, 73f, StringControl.TextIsFloat);
			panel.AddLabelledStringControl("Y: ", this.pos.y.ToString(), "pos_y", 16f, 73f, StringControl.TextIsFloat);
			panel.AddLabelledStringControl("Depth: ", this.depth.ToString(), "depth", 40f, 71f, StringControl.TextIsPositiveFloat);

			return panel;
		}

		public virtual void RefreshDevUI(BackgroundPage.ElementDataPanel panel)
		{

			if (panel.TryGetExistingNode<StringControl>("pos_x", out _, out var xNode) && xNode != ManagedStringControl.activeStringControl)
			{
				xNode.actualValue = xNode.Text = this.pos.x.ToString();
			}
			if (panel.TryGetExistingNode<StringControl>("pos_y", out _, out var yNode) && yNode != ManagedStringControl.activeStringControl)
			{
				yNode.actualValue = yNode.Text = this.pos.y.ToString();
			}
			if (panel.TryGetExistingNode<StringControl>("depth", out _, out var dNode) && dNode != ManagedStringControl.activeStringControl)
			{
				dNode.actualValue = dNode.Text = depth.ToString();
			}
		}

		public virtual void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender is StringControl stringControl && type == StringControl.StringFinish)
			{
				if (stringControl.IDstring == "pos_x")
				{
					pos.x = float.Parse(stringControl.actualValue);
					Pos = Pos;
				}
				if (stringControl.IDstring == "pos_y")
				{
					pos.y = float.Parse(stringControl.actualValue);
					Pos = Pos;
				}
				if (stringControl.IDstring == "depth")
				{
					depth = float.Parse(stringControl.actualValue);
					Depth = Depth;
				}
			}
			if (sender is ElementGroupSelectButton button && button.actualValue != group)
			{
				var sceneDataElements = sender.RoomSettings.BackgroundData().sceneData.backgroundElements;
				if (group != null)
					group.RemoveElement(this);
				else
					sceneDataElements.Remove(this);
				if (button.actualValue != null)
					button.actualValue.AddElement(this);
				else
					sceneDataElements.Add(this);
			}
		}


		public virtual bool ElementClicked(RoomCamera rCam) => false;

		//public abstract string DevUIName();
	}

	public class BG_ElementGroup : CustomBgElement, Data.IListBackgroundElements
	{
		public BG_ElementGroup()
		{
			name = "new group";
		}

		public override void DataFromArgs(string[] args)
		{
			name = args[0];
			if (args.Length >= 3)
				pos = new Vector2(float.Parse(args[1]), float.Parse(args[2]));
			else
				pos = Vector2.zero;

			if (args.Length >= 4)
				depth = float.Parse(args[3]);
			else
				depth = 0;
		}

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			throw new NotImplementedException("groups don't have associated elements!");
		}

		public override Vector2 Pos
		{
			get
			{
				return base.Pos;
			}

			set
			{
				base.Pos = value;
				foreach (var element in elements)
					element.Pos = element.Pos;
			}
		}

		public override float Depth
		{
			get
			{
				return base.Depth;
			}

			set
			{
				base.Depth = value;
				foreach (var element in elements)
					element.Depth = element.Depth;
			}
		}

		public List<CustomBgElement> elements = new();
		public string name;
		IEnumerable<CustomBgElement> Data.IListBackgroundElements.customBgElements => elements;

		public void AddElement(CustomBgElement element)
		{
			this.elements.Add(element);
			element.group = this;
		}

		public void RemoveElement(CustomBgElement element)
		{
			this.elements.Remove(element);
			element.group = null;
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			throw new NotImplementedException("for groups, use MakeSceneElements() instead!");
		}

		public List<BackgroundScene.BackgroundSceneElement> MakeSceneElements(BackgroundScene self)
		{
			List<BackgroundScene.BackgroundSceneElement> sceneElements = new();

			foreach (var s in elements)
			{
				if (s is BG_ElementGroup group)
				{
					sceneElements.AddRange(group.MakeSceneElements(self));
				}
				else
				{
					s.sceneElement = s.MakeSceneElement(self);
					sceneElements.Add(s.sceneElement);
				}
			}

			return sceneElements;
		}

		public override string Serialize()
		{
			return SerializeHeader() + GROUP_NEWLINE + string.Join(GROUP_NEWLINE, SerializeElements()) + GROUP_NEWLINE;
		}

		public string SerializeHeader()
		{
			string groupString = "GROUP: " + name;
			if (Pos != Vector2.zero || Depth != 0)
			{
				groupString += $", {Pos.x}, {Pos.y}";

				if (Depth != 0)
					groupString += $", {Depth}";
			}
			return groupString;
		}

		public IEnumerable<string> SerializeElements()
		{
			foreach (var e in elements)
			{
				string s = e.Serialize();
				if (!s.Contains(GROUP_NEWLINE))
					yield return GROUP_EMBED_STRING + s;
				else
				{
					string[] array = Regex.Split(s, GROUP_NEWLINE);
					foreach(string s2 in array)
						yield return GROUP_EMBED_STRING + s2;
				}
			}
		}

		public const string GROUP_NEWLINE = "\r\n";

		public const string GROUP_EMBED_STRING = "- ";

		public string RecursiveEmbedString()
		{
			string parentGroupString = group?.RecursiveEmbedString() ?? "";
			return parentGroupString + GROUP_EMBED_STRING;
		}

		public override Vector2 DevUIHandlePos(RoomCamera rCam)
		{
			int count = 0;
			Vector2 pos = Vector2.zero;
			foreach (var e in elements)
			{
				if (e.sceneElement != null)
				{
					pos += e.DevUIHandlePos(rCam);
					count++;
				}
			}

			if (count > 0)
			{
				pos /= count;
				return pos;
			}

			return base.DevUIHandlePos(rCam);
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size);

			if (panel.TryGetExistingNode<BackgroundPage.ElementAssetSelectButton>("asset", out var label, out var node))
			{
				if(label != null)
					label.Text = "Name: ";
				Vector2 nodePos = node.pos;
				float width = node.size.x;
				panel.subNodes.Add(new StringControl(owner, "name", panel, nodePos, width, this.name, StringControl.TextIsAny));
				node.ClearSprites();
				panel.subNodes.Remove(node);
			}

			return panel;
		}

		public override void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender is StringControl control && control.IDstring == "name")
			{
				this.name = control.actualValue;
				//UpdateSceneElement("name");
			}
			else
				base.SingalFromDevUI(type, sender, message);
		}

		public override string DevUIName()
		{
			return "GROUP: " + name;
		}

		public override bool ElementClicked(RoomCamera rCam)
		{
			foreach (CustomBgElement groupElement in elements)
			{
				if (groupElement.ElementClicked(rCam))
					return true;
			}
			return false;
		}
	}

	public abstract class GraphicBgElement : CustomBgElement
	{
		public GraphicBgElement(string assetName)
		{
			this.assetName = assetName;
		}

		public override void DataFromArgs(string[] args)
		{
			assetName = args[0];
			pos = new Vector2(float.Parse(args[1]), float.Parse(args[2]));
			depth = float.Parse(args[3]);
		}

		protected string assetName;
		public virtual string AssetName
		{
			get => assetName;
			set
			{
				assetName = value;
				RefreshAsset();
			}
		}

		public abstract void RefreshAsset();

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = new BackgroundPage.ElementDataPanel(owner, "", parent, pos, size, this);
			panel.AddLabelledUINode("Asset: ", 40f, new BackgroundPage.ElementAssetSelectButton(owner, "asset", panel, Vector2.zero, 148f, AssetName, "Select Asset", BackgroundPage.ElementAssetSelectPanel.SelectGroup.Illustrations));
			panel.NewRow();
			panel.AddLabelledUINode("Group: ", 40f, new ElementGroupSelectButton(owner, "group", panel, Vector2.zero, 148f, group!));
			panel.AddLabelledStringControl("X: ", this.pos.x.ToString(), "pos_x", 16f, 73f, StringControl.TextIsFloat);
			panel.AddLabelledStringControl("Y: ", this.pos.y.ToString(), "pos_y", 16f, 73f, StringControl.TextIsFloat);
			panel.AddLabelledStringControl("Depth: ", this.depth.ToString(), "depth", 40f, 71f, StringControl.TextIsPositiveFloat);

			return panel;
		}

		public override void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender is BackgroundPage.ElementAssetSelectButton button)
			{
				AssetName = button.actualValue;
				return;
			}
			else
				base.SingalFromDevUI(type, sender, message);
		}

		public override bool ElementClicked(RoomCamera rCam)
		{
			if (sceneElement == null)
				return false;

			FSprite? sprite = GetAssetSprite;
			if (sprite == null || (sprite._atlas.texture is not Texture2D tex)) return false;

			Vector2 offset = new();
			if (sceneElement.scene is AboveCloudsView acv)
			{ offset = new Vector2(0, acv.yShift); }

			Vector2 mouseOnSpritePos = MouseOnElementPos(rCam, tex.width, offset);

			if (mouseOnSpritePos.x < 0 || mouseOnSpritePos.x > tex.width || mouseOnSpritePos.y < 0 || mouseOnSpritePos.y > tex.height) return false;
			if (tex.GetPixel((int)mouseOnSpritePos.x, (int)mouseOnSpritePos.y).a <= 0.5f) return false;

			return true;
		}

		public virtual Vector2 MouseOnElementPos(RoomCamera cam, float texWidth, Vector2 shift = new())
		{
			var mousePos = new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
			Vector2 drawPos = sceneElement?.DrawPos(cam.pos + shift, cam.hDisplace) ?? this.Pos;
			return mousePos - drawPos + new Vector2(texWidth / 2, 0);
		}

		public virtual FSprite? GetAssetSprite => new FSprite(AssetName, true);
	}


	public class BG_SimpleElement : GraphicBgElement
	{
		public BG_SimpleElement() : base("Futile_White") { }

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			if (element is CustomBackgroundElements.SimpleBackgroundElement el)
			{
				assetName = el.assetName;
			}
		}

		public override void RefreshAsset()
		{
			if (sceneElement is CustomBackgroundElements.SimpleBackgroundElement element)
			{
				element.assetName = AssetName;
				element.scene.LoadGraphic(AssetName, true, false);
				element.CData().ReInitiateSprites = true;
			}
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			return new CustomBackgroundElements.SimpleBackgroundElement(self, AssetName, Pos, Depth);
		}

		public override Vector2 ScenePos => Pos;

		public override Vector2 ReverseScenePos(Vector2 movement) => movement * Depth;

		public override string Serialize() => $"SimpleElement: {AssetName}, {pos.x}, {pos.y}, {depth}";

		public override string DevUIName()
		{
			return "SimpleElement: " + AssetName;
		}

	}

	public class BG_Illustration : GraphicBgElement
	{
		public BG_Illustration() : base("AtC_Sky") { }

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			if (element is BackgroundScene.Simple2DBackgroundIllustration el)
			{
				assetName = el.illustrationName;

				if (el is BackgroundScene.AdditiveBackgroundIllustration)
					spriteShader = "BackgroundAdditive";
			}
		}

		public override void RefreshAsset()
		{
			if (sceneElement is BackgroundScene.Simple2DBackgroundIllustration illustration)
			{
				illustration.illustrationName = AssetName;
				illustration.scene.LoadGraphic(AssetName, true, true);
				illustration.CData().ReInitiateSprites = true;
			}
		}

		public override Vector2 Pos
		{
			get => base.Pos;
			set
			{
				base.Pos = value;
				if (sceneElement != null)
					sceneElement.CData().ReInitiateSprites = true;
			}
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			return new BackgroundScene.Simple2DBackgroundIllustration(self, AssetName, Pos) { depth = Depth };
		}

		public override Vector2 ScenePos => Pos;
		public override Vector2 ReverseScenePos(Vector2 movement) => movement;

		public override Vector2 DevUIHandlePos(RoomCamera rCam)
		{
			return Pos;
		}

		public override string Serialize() => $"SimpleIllustration: {AssetName}, {pos.x}, {pos.y}, {depth}";

		public override string DevUIName()
		{
			return "SimpleIllustration: " + AssetName;
		}
	}
	#region AboveCloudsView
	public class ACV_DistantBuilding : GraphicBgElement
	{
		public ACV_DistantBuilding() : base("AtC_Structure1")
		{
			atmoDepthAdd = 0f;
		}

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			pos = element.pos / element.depth;
			if (element is AboveCloudsView.DistantBuilding building)
			{
				assetName = building.assetName;
				atmoDepthAdd = building.atmosphericalDepthAdd;
				spriteScale = building.scale == 1f ? null : new(building.scale, building.scale);
			}
			else if (element is RoofTopView.DistantBuilding)
				Custom.LogWarning("acv on rooftop somehow");
		}

		public override void DataFromArgs(string[] args)
		{
			base.DataFromArgs(args);
			atmoDepthAdd = float.Parse(args[4]);
		}
		public override void RefreshAsset()
		{
			if (sceneElement is AboveCloudsView.DistantBuilding building)
			{
				building.assetName = AssetName;
				building.scene.LoadGraphic(AssetName, true, false);
				building.CData().ReInitiateSprites = true;
			}
		}

		public float atmoDepthAdd;

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AboveCloudsView acv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new AboveCloudsView.DistantBuilding(acv, AssetName, DefaultNeutralPos(Pos, Depth), Depth, atmoDepthAdd);
		}

		public override string Serialize() => $"DistantBuilding: {AssetName}, {pos.x}, {pos.y}, {depth}, {atmoDepthAdd}";

		public override string DevUIName()
		{
			return "DistantBuilding: " + AssetName;
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size + new Vector2(0f, 20f));

			if (panel.TryGetExistingNode<BackgroundPage.ElementAssetSelectButton>("asset", out var label, out var node))
			{
				node.defaultSelectGroup = BackgroundPage.ElementAssetSelectPanel.SelectGroup.ATC;
			}

			panel.AddLabelledStringControl("atmoDepthAdd", atmoDepthAdd.ToString(), "atmoDepthAdd", 90, 90, StringControl.TextIsFloat);

			return panel;
		}

		public override void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender is StringControl control && type == StringControl.StringFinish)
			{
				if (control.IDstring == "atmoDepthAdd")
				{
					this.atmoDepthAdd = float.Parse(control.actualValue);
					if (sceneElement is AboveCloudsView.DistantBuilding building)
						building.atmosphericalDepthAdd = atmoDepthAdd;
					return;
				}
			}

			base.SingalFromDevUI(type, sender, message);
		}

	}

	public class ACV_DistantLightning : GraphicBgElement
	{
		public ACV_DistantLightning() : base("AtC_Light1")
		{
			minusDepthForLayering = 0f;
		}

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			if (element is AboveCloudsView.DistantLightning lightning)
			{
				pos = element.pos / (lightning.restoredDepth ? element.depth : element.depth + lightning.minusDepthForLayering);
				assetName = lightning.assetName;
				minusDepthForLayering = lightning.minusDepthForLayering;
			}
		}

		public override void DataFromArgs(string[] args)
		{
			base.DataFromArgs(args);
			minusDepthForLayering = float.Parse(args[4]);
		}
		public override void RefreshAsset()
		{
			if (sceneElement is AboveCloudsView.DistantLightning lightning)
			{
				lightning.assetName = AssetName;
				lightning.scene.LoadGraphic(AssetName, true, false);
				lightning.CData().ReInitiateSprites = true;
			}
		}

		public override float Depth
		{
			get => base.Depth;
			set
			{
				base.Depth = value;

				if (sceneElement is AboveCloudsView.DistantLightning lightning)
				{
					lightning.depth = Depth - minusDepthForLayering;
					lightning.restoredDepth = false; //ideally this shouldn't flip until Depth is reapplied in the graphics update
					lightning.CData().DepthUpdate = true;
				}
			}
		}


		private float minusDepthForLayering;

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AboveCloudsView acv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new AboveCloudsView.DistantLightning(acv, AssetName, DefaultNeutralPos(Pos, Depth), Depth, minusDepthForLayering);
		}

		public override string Serialize() => $"DistantLightning: {AssetName}, {pos.x}, {pos.y}, {depth}, {minusDepthForLayering}";

		public override void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender is StringControl control && type == StringControl.StringFinish)
			{
				if (control.IDstring == "modifyDepth")
				{
					minusDepthForLayering = float.Parse(control.actualValue);

					if (sceneElement is AboveCloudsView.DistantLightning lightning)
					{
						lightning.minusDepthForLayering = minusDepthForLayering;
						lightning.depth = Depth - minusDepthForLayering;
						lightning.restoredDepth = false; //ideally this shouldn't flip until Depth is reapplied in the graphics update
						lightning.CData().DepthUpdate = true;
					}
					return;
				}
			}

			base.SingalFromDevUI(type, sender, message);
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size + new Vector2(0f, 20f));

			if (panel.TryGetExistingNode<BackgroundPage.ElementAssetSelectButton>("asset", out var label, out var node))
				node.defaultSelectGroup = BackgroundPage.ElementAssetSelectPanel.SelectGroup.ATC;

			panel.AddLabelledStringControl("modifyDepth", minusDepthForLayering.ToString(), "modifyDepth", 90, 90, StringControl.TextIsFloat);

			return panel;
		}

		public override string DevUIName()
		{
			return "DistantLightning: " + AssetName;
		}

		public override bool ElementClicked(RoomCamera rCam) => false; //not clickable
	}

	public class ACV_FlyingCloud : CustomBgElement
	{
		public ACV_FlyingCloud()
		{
			this.flattened = 0.5f;
			this.alpha = 0.5f;
			this.shaderInputColor = 0.5f;
		}

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			pos = element.pos / element.depth;
			if (element is AboveCloudsView.FlyingCloud cloud)
			{
				flattened = cloud.flattened;
				alpha = cloud.alpha;
				shaderInputColor = cloud.shaderInputColor;
			}
		}

		public override void DataFromArgs(string[] args)
		{
			pos = new Vector2(float.Parse(args[0]), float.Parse(args[1]));
			depth = float.Parse(args[2]); 
			flattened = float.Parse(args[3]); 
			alpha = float.Parse(args[4]); 
			shaderInputColor = float.Parse(args[5]);
		}
		//int index; is always zero
		float flattened;
		float alpha;
		float shaderInputColor;

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AboveCloudsView acv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new AboveCloudsView.FlyingCloud(acv, DefaultNeutralPos(Pos, Depth), Depth, 0, flattened, alpha, shaderInputColor);
		}

		public override string Serialize() => $"FlyingCloud: {pos.x}, {pos.y}, {depth}, {flattened}, {alpha}, {shaderInputColor}";

		public override Vector2 ScenePos => DefaultNeutralPos(Pos, Depth);

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = new BackgroundPage.ElementDataPanel(owner, "", parent, pos, size + new Vector2(0f, 40f), this);

			panel.AddLabelledUINode("Group: ", 40f, new Button(owner, "group", panel, Vector2.zero, 148f, group?.name ?? "None"));
			panel.AddLabelledStringControl("X: ", this.pos.x.ToString(), "pos_x", 16f, 73f);
			panel.AddLabelledStringControl("Y: ", this.pos.y.ToString(), "pos_y", 16f, 73f);
			panel.AddLabelledStringControl("Depth: ", this.depth.ToString(), "depth", 40f, 71f);
			panel.NewRow();
			panel.subNodes.Add(new GenericSlider(owner, "flattened", panel, panel.GetLastBuildPos(), "flattened", false, 62f, flattened) { valueRounding = 2, displayRounding = 2 });
			panel.NewRow();
			panel.subNodes.Add(new GenericSlider(owner, "alpha", panel, panel.GetLastBuildPos(), "alpha", false, 62f, alpha) { valueRounding = 2, displayRounding = 2 });
			panel.NewRow();
			panel.subNodes.Add(new GenericSlider(owner, "inputColor", panel, panel.GetLastBuildPos(), "input color", false, 62f, shaderInputColor) { valueRounding = 2, displayRounding = 2 });

			return panel;
		}

		public override void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			AboveCloudsView.FlyingCloud? cloud = sceneElement as AboveCloudsView.FlyingCloud;
			if (sender is GenericSlider slider)
			{
				if (sender.IDstring == "flattened")
				{
					flattened = slider.actualValue;
					if(cloud != null) cloud.flattened = flattened;
				}
				if (sender.IDstring == "alpha")
				{
					alpha = slider.actualValue;
					if (cloud != null) cloud.alpha = alpha;
				}
				if (sender.IDstring == "inputColor")
				{
					shaderInputColor = slider.actualValue;
					if (cloud != null) cloud.shaderInputColor = shaderInputColor;
				}
				return;
			}

			base.SingalFromDevUI(type, sender, message);
		}

		public override string DevUIName()
		{
			return "FlyingCloud";
		}
	}

	public class ACV_HorizonFog : GraphicBgElement
	{
		//int index; is always zero

		public ACV_HorizonFog() : base("pnk_fog") { }

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			if (element is AboveCloudsView.HorizonFog fog)
			{
				assetName = fog.illustrationName;
			}
		}

		public override void RefreshAsset()
		{
			if (sceneElement is AboveCloudsView.HorizonFog horizonFog)
			{
				horizonFog.illustrationName = AssetName;
				horizonFog.scene.LoadGraphic(AssetName, true, true);
				horizonFog.CData().ReInitiateSprites = true;
			}
		}


		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AboveCloudsView acv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new AboveCloudsView.HorizonFog(acv, AssetName, Pos, Depth);
		}

		public override Vector2 ScenePos => Pos;
		public override string Serialize() => $"HorizonFog: {AssetName}, {pos.x}, {pos.y}, {depth}";

		public override string DevUIName()
		{
			return "HorizonFog: " + AssetName;
		}
	}
	#endregion AboveCloudsView

	#region RoofTopView
	public class RTV_Floor : GraphicBgElement
	{
		public RTV_Floor() : base("floor")
		{
			fromDepth = 1f;
			toDepth = 12f;
		}

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			if (element is RoofTopView.Floor floor)
			{
				pos = new Vector2(element.pos.x, element.pos.y - (element.scene as RoofTopView)!.floorLevel);
				assetName = floor.assetName;
				toDepth = floor.toDepth;
				fromDepth = floor.fromDepth;
			}
		}

		public override void DataFromArgs(string[] args)
		{
			base.DataFromArgs(args);
			fromDepth = float.Parse(args[3]);
			toDepth = float.Parse(args[4]);
		}
		public override void RefreshAsset()
		{
			if (sceneElement is RoofTopView.Floor floor)
			{
				floor.assetName = AssetName;
				floor.scene.LoadGraphic(AssetName, true, false);
				floor.CData().ReInitiateSprites = true;
			}
		}

		public float fromDepth;
		public float toDepth;

		public float midDepth => Mathf.Lerp(fromDepth, toDepth, 0.5f);

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new RoofTopView.Floor(rtv, AssetName, new Vector2(0f, rtv.floorLevel) + Pos, fromDepth, toDepth);
		}

		public override Vector2 ScenePos => new Vector2(0f, (sceneElement?.scene as RoofTopView)?.floorLevel ?? 0f) + Pos;

		public override Vector2 ReverseScenePos(Vector2 movement) => movement * midDepth;

		public override string Serialize() => $"Floor: {AssetName}, {pos.x}, {pos.y}, {fromDepth}, {toDepth}";

		public override Vector2 DevUIHandlePos(RoomCamera rCam)
		{
			if (sceneElement != null)
				return sceneElement.scene.DrawPos(Pos, midDepth, rCam.pos, rCam.hDisplace);
			return Pos;
		}

		public override string DevUIName()
		{
			return "Floor: " + AssetName;
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size);

			if (panel.TryGetExistingNode<BackgroundPage.ElementAssetSelectButton>("asset", out var label, out var node))
				node.defaultSelectGroup = BackgroundPage.ElementAssetSelectPanel.SelectGroup.RFV;

			float offset = 0f;
			if (panel.TryGetExistingNode<StringControl>("depth", out var label2, out var node2))
			{
				if(label2 != null) label2.Text = "From: ";
				offset = node2.size.x - 48f;
				node2.size.x = 48f;
				node2.fSprites[0].scaleX = node2.size.x;
				node2.Refresh();
			}

			panel.SetBuildPos(panel.GetLastBuildPos() + new Vector2(-offset, 0f));
			panel.AddLabelledStringControl("To: ", toDepth.ToString(), "depth2", 40f, 48f, StringControl.TextIsPositiveFloat);

			return panel;
		}

		public override void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			RoofTopView.Floor? floor = sceneElement as RoofTopView.Floor;
			if (sender is StringControl stringControl)
			{
				if (sender.IDstring == "depth")
				{
					fromDepth = float.Parse(stringControl.actualValue);
					depth = fromDepth;
					if (floor != null) floor.fromDepth = fromDepth;
				}
				if (sender.IDstring == "depth2")
				{
					toDepth = float.Parse(stringControl.actualValue);
					if (floor != null) floor.toDepth = toDepth;
				}
				return;
			}

			base.SingalFromDevUI(type, sender, message);
		}
	}
	public class RTV_DistantBuilding : GraphicBgElement
	{
		public RTV_DistantBuilding() : base("RF_CityA")
		{
			atmoDepthAdd = 0f;
		}

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			if (element is RoofTopView.DistantBuilding building)
			{
				pos = new Vector2(element.pos.x / element.depth, element.pos.y - (element.scene as RoofTopView)!.floorLevel);
				assetName = building.assetName;
				atmoDepthAdd = building.atmosphericalDepthAdd;
			}
		}

		public override void DataFromArgs(string[] args)
		{
			base.DataFromArgs(args);
			atmoDepthAdd = float.Parse(args[4]);
		}

		public override void RefreshAsset()
		{
			if (sceneElement is RoofTopView.DistantBuilding building)
			{
				building.assetName = AssetName;
				building.scene.LoadGraphic(AssetName, true, false);
				building.CData().ReInitiateSprites = true;
			}
		}

		public float atmoDepthAdd;

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new RoofTopView.DistantBuilding(rtv, AssetName, new Vector2(DefaultNeutralPos(Pos, Depth).x, rtv.floorLevel + Pos.y), Depth, atmoDepthAdd);
		}

		public override Vector2 ScenePos => new Vector2(DefaultNeutralPos(Pos, Depth).x, ((sceneElement?.scene as RoofTopView)?.floorLevel ?? 0f) + Pos.y);

		public override Vector2 ReverseScenePos(Vector2 movement) => movement * new Vector2(1f, Depth);

		public override string Serialize() => $"DistantBuilding: {AssetName}, {pos.x}, {pos.y}, {depth}, {atmoDepthAdd}";

		public override string DevUIName()
		{
			return "DistantBuilding: " + AssetName;
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size + new Vector2(0f, 20f));

			if (panel.TryGetExistingNode<BackgroundPage.ElementAssetSelectButton>("asset", out var label, out var node))
				node.defaultSelectGroup = BackgroundPage.ElementAssetSelectPanel.SelectGroup.ATC;

			panel.AddLabelledStringControl("atmoDepthAdd", atmoDepthAdd.ToString(), "atmoDepthAdd", 90, 90, StringControl.TextIsFloat);

			return panel;
		}

		public override void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender is StringControl control && type == StringControl.StringFinish)
			{
				if (control.IDstring == "atmoDepthAdd")
				{
					this.atmoDepthAdd = float.Parse(control.actualValue);
					if (sceneElement is RoofTopView.DistantBuilding building)
						building.atmosphericalDepthAdd = atmoDepthAdd;
					return;
				}
			}

			base.SingalFromDevUI(type, sender, message);
		}

	}
	public class RTV_Building : GraphicBgElement
	{
		public RTV_Building() : base("city1")
		{
			scale = 1f;
		}

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			if (element is RoofTopView.Building building)
			{
				pos = new Vector2(element.pos.x / element.depth, element.pos.y - (element.scene as RoofTopView)!.floorLevel);
				assetName = building.assetName;
				scale = building.scale;
			}
		}

		public override void DataFromArgs(string[] args)
		{
			base.DataFromArgs(args);
			scale = float.Parse(args[4]);
		}

		public override void RefreshAsset()
		{
			if (sceneElement is RoofTopView.Building building)
			{
				building.assetName = AssetName;
				building.scene.LoadGraphic(AssetName, false, true);
				building.CData().ReInitiateSprites = true;
			}
		}

		public float scale;

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);
			return new RoofTopView.Building(rtv, AssetName, new Vector2(DefaultNeutralPos(new Vector2(Pos.x, Pos.y), Depth).x, rtv.floorLevel + Pos.y), Depth, scale);
		}

		public override Vector2 ScenePos => new Vector2(DefaultNeutralPos(new Vector2(Pos.x, Pos.y), Depth).x, ((sceneElement?.scene as RoofTopView)?.floorLevel ?? 0f) + Pos.y);

		public override string Serialize() => $"Building: {AssetName}, {pos.x}, {pos.y}, {depth}, {scale}";

		public override Vector2 ReverseScenePos(Vector2 movement) => movement * new Vector2(1f, Depth);

		public override string DevUIName()
		{
			return "Building: " + AssetName;
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size);

			if (panel.TryGetExistingNode<BackgroundPage.ElementAssetSelectButton>("asset", out var label, out var node))
				node.defaultSelectGroup = BackgroundPage.ElementAssetSelectPanel.SelectGroup.RFV;

			float offset = 0f;
			if (panel.TryGetExistingNode<StringControl>("depth", out var label2, out var node2))
			{
				offset = node2.size.x - 48f;
				node2.size.x = 48f;
				node2.fSprites[0].scaleX = node2.size.x;
				node2.Refresh();
			}

			panel.SetBuildPos(panel.GetLastBuildPos() + new Vector2(-offset, 0f));
			panel.AddLabelledStringControl("scale", scale.ToString(), "scale", 40f, 48f, StringControl.TextIsPositiveFloat);

			return panel;
		}

		public override void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender is StringControl control && type == StringControl.StringFinish)
			{
				if (control.IDstring == "scale")
				{
					this.scale = float.Parse(control.actualValue);
					if (sceneElement is RoofTopView.Building building)
					{
						building.scale = scale;
						building.CData().ReInitiateSprites = true;
					}
					return;
				}
			}

			base.SingalFromDevUI(type, sender, message);
		}
	}
	public class RTV_DistantGhost : CustomBgElement
	{
		public RTV_DistantGhost() { }

		public override void DataFromArgs(string[] args)
		{
			pos = new Vector2(float.Parse(args[0]), float.Parse(args[1]));
			depth = float.Parse(args[2]);
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new RoofTopView.DistantGhost(rtv, DefaultNeutralPos(Pos, Depth), Depth, 0);
		}

		public override string Serialize() => $"DistantBuilding: {pos.x}, {pos.y}, {depth}";

		public override string DevUIName()
		{
			return "DistantGhost";
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size + new Vector2(0f, -20f));

			panel.RemoveAndCollapse("asset");
			return panel;
		}
	}
	public class RTV_DustWave : GraphicBgElement
	{
		public RTV_DustWave() : base("RF_CityA") { }

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			if (element is RoofTopView.DustWave dust)
			{
				pos = new Vector2(element.pos.x, element.pos.y - (element.scene as RoofTopView)!.floorLevel);
				assetName = dust.assetName;
			}
		}

		public override void RefreshAsset()
		{
			if (sceneElement is RoofTopView.DustWave dust)
			{
				dust.assetName = AssetName;
				dust.scene.LoadGraphic(AssetName, true, false);
				dust.CData().ReInitiateSprites = true;
			}
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RoofTopView rtv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);

			return new RoofTopView.DustWave(rtv, AssetName, new Vector2(Pos.x, rtv.floorLevel + Pos.y), Depth, 0f);
		}

		public override Vector2 ScenePos => new Vector2(0f, (sceneElement?.scene as RoofTopView)?.floorLevel ?? 0f) + Pos;

		public override string Serialize() => $"DustWave: {AssetName}, {pos.x}, {pos.y}, {depth}";

		public override string DevUIName()
		{
			return "DustWave: " + AssetName;
		}
	}
	public class RTV_Smoke : CustomBgElement
	{
		public RTV_Smoke()
		{
			this.flattened = 0.5f;
			this.alpha = 0.5f;
			this.shaderInputColor = 0.5f;
			this.shaderType = true;
		}

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			//base.DataFromElement(element);
			pos = element.pos;
			depth = element.depth;
			if (element is RoofTopView.Smoke rtv_smoke)
			{
				pos = new Vector2(element.pos.x, element.pos.y - (element.scene as RoofTopView)!.floorLevel);
				flattened = rtv_smoke.flattened;
				alpha = rtv_smoke.alpha;
				shaderInputColor = rtv_smoke.shaderInputColor;
				shaderType = rtv_smoke.shaderType;
			}
			if (element is AncientUrbanView.Smoke auv_smoke)
			{
				pos = new Vector2(element.pos.x, element.pos.y - (element.scene as AncientUrbanView)!.floorLevel);
				flattened = auv_smoke.flattened;
				alpha = auv_smoke.alpha;
				shaderInputColor = auv_smoke.shaderInputColor;
				shaderType = auv_smoke.shaderType;
			}
		}

		public override void DataFromArgs(string[] args)
		{
			pos = new Vector2(float.Parse(args[0]), float.Parse(args[1]));
			depth = float.Parse(args[2]);
			flattened = float.Parse(args[3]);
			alpha = float.Parse(args[4]);
			shaderInputColor = float.Parse(args[5]);
			shaderType = bool.Parse(args[6]);
		}

		float flattened;
		float alpha;
		float shaderInputColor;
		bool shaderType;
		public string? spriteName = null;

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is RoofTopView rtv) return new RoofTopView.Smoke(rtv, new Vector2(Pos.x, rtv.floorLevel + Pos.y), Depth, 0, flattened, alpha, shaderInputColor, shaderType);
			if (self is AncientUrbanView auv) return new AncientUrbanView.Smoke(auv, new Vector2(Pos.x, auv.floorLevel + Pos.y), Depth, 0, flattened, alpha, shaderInputColor, shaderType);
			throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);
		}

		public override Vector2 ScenePos
		{
			get
			{
				float floorLevel = 0f;
				if(sceneElement?.scene is RoofTopView rtv)
					floorLevel = rtv.floorLevel;
				else if(sceneElement?.scene is AncientUrbanView auv)
					floorLevel = auv.floorLevel;
				return new Vector2(Pos.x, floorLevel + Pos.y);
			}
		}

		public override string Serialize() => $"Smoke: {pos.x}, {pos.y}, {depth}, {flattened}, {alpha}, {shaderInputColor}, {shaderType}";

		public override string DevUIName()
		{
			return "Smoke";
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = new BackgroundPage.ElementDataPanel(owner, "", parent, pos, size + new Vector2(0f, 60f), this);

			panel.AddLabelledUINode("Group: ", 40f, new Button(owner, "group", panel, Vector2.zero, 148f, group?.name ?? "None"));
			panel.AddLabelledStringControl("X: ", this.pos.x.ToString(), "pos_x", 16f, 73f);
			panel.AddLabelledStringControl("Y: ", this.pos.y.ToString(), "pos_y", 16f, 73f);
			panel.AddLabelledStringControl("Depth: ", this.depth.ToString(), "depth", 40f, 71f);
			panel.NewRow();
			panel.subNodes.Add(new GenericSlider(owner, "flattened", panel, panel.GetLastBuildPos(), "flattened", false, 62f, flattened) { valueRounding = 2, displayRounding = 2 });
			panel.NewRow();
			panel.subNodes.Add(new GenericSlider(owner, "alpha", panel, panel.GetLastBuildPos(), "alpha", false, 62f, alpha) { valueRounding = 2, displayRounding = 2 });
			panel.NewRow();
			panel.subNodes.Add(new GenericSlider(owner, "inputColor", panel, panel.GetLastBuildPos(), "input color", false, 62f, shaderInputColor) { valueRounding = 2, displayRounding = 2 });
			panel.NewRow();
			panel.subNodes.Add(new Button(owner, "shaderType", panel, panel.GetLastBuildPos(), 62f, ShaderTypeName));

			return panel;
		}

		string ShaderTypeName => shaderType ? "Dust" : "Cloud";
		public override void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			RoofTopView.Smoke? rtv_smoke = sceneElement as RoofTopView.Smoke;
			AncientUrbanView.Smoke? auv_smoke = sceneElement as AncientUrbanView.Smoke;
			if (sender is GenericSlider slider)
			{
				if (sender.IDstring == "flattened")
				{
					flattened = slider.actualValue;
					if (rtv_smoke != null) rtv_smoke.flattened = flattened;
					if (auv_smoke != null) auv_smoke.flattened = flattened;
				}
				if (sender.IDstring == "alpha")
				{
					alpha = slider.actualValue;
					if (rtv_smoke != null) rtv_smoke.alpha = alpha;
					if (auv_smoke != null) auv_smoke.alpha = alpha;
				}
				if (sender.IDstring == "inputColor")
				{
					shaderInputColor = slider.actualValue;
					if (rtv_smoke != null) rtv_smoke.shaderInputColor = shaderInputColor;
					if (auv_smoke != null) auv_smoke.shaderInputColor = shaderInputColor;
				}
				return;
			}
			else if (sender is Button button && sender.IDstring == "shaderType")
			{
				shaderType = !shaderType;

				if (rtv_smoke != null) rtv_smoke.shaderType = shaderType;
				if (auv_smoke != null) auv_smoke.shaderType = shaderType;
				if(sceneElement != null) sceneElement.CData().ReInitiateSprites = true;
				button.Text = ShaderTypeName;
			}

				base.SingalFromDevUI(type, sender, message);
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

	public class AUV_Building : GraphicBgElement
	{
		public AUV_Building() : base("au_buildingp_1")
		{
			this.scale = 1f;
			this.rotation = 0f;
			this.thickness = 1f;
		}

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			if (element is AncientUrbanView.Building building)
			{
				pos = new Vector2(element.pos.x / element.depth, element.pos.y - (element.scene as AncientUrbanView)!.floorLevel);
				assetName = building.assetName;
				scale = building.scale;
				rotation = building.rotation;
				thickness = building.thickness;
			}
		}

		public override void DataFromArgs(string[] args)
		{
			base.DataFromArgs(args);
			scale = float.Parse(args[4]);
			rotation = float.Parse(args[5]); 
			thickness = float.Parse(args[6]);
		}

		public override void RefreshAsset()
		{
			if (sceneElement is AncientUrbanView.Building building)
			{
				building.assetName = AssetName;
				building.scene.LoadGraphic(AssetName, true, false);
				building.CData().ReInitiateSprites = true;
			}
		}

		public float scale;

		public float rotation;

		public float thickness;

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AncientUrbanView auv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);
			self.LoadGraphic(AssetName, true, false);
			return new AncientUrbanView.Building(auv, AssetName, new Vector2(DefaultNeutralPos(new Vector2(Pos.x, Pos.y), Depth).x, auv.floorLevel + Pos.y), Depth, scale, rotation, thickness);
		}

		public override string Serialize() => $"Building: {AssetName}, {pos.x}, {pos.y}, {depth}, {scale}, {rotation}, {thickness}";

		public override Vector2 ScenePos => new Vector2(DefaultNeutralPos(new Vector2(Pos.x, Pos.y), Depth).x, ((sceneElement?.scene as AncientUrbanView)?.floorLevel ?? 0f) + Pos.y);

		public override Vector2 ReverseScenePos(Vector2 movement) => movement * new Vector2(1f, Depth);

		public override string DevUIName()
		{
			return "Building: " + AssetName;
		}
		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size + new Vector2(0f, 40f));

			if (panel.TryGetExistingNode<BackgroundPage.ElementAssetSelectButton>("asset", out var label, out var node))
				node.defaultSelectGroup = BackgroundPage.ElementAssetSelectPanel.SelectGroup.AUV;

			float offset = 0;
			if (panel.TryGetExistingNode<StringControl>("depth", out var label2, out var node2))
			{
				offset = node2.size.x - 48f;
				node2.size.x = 48f;
				node2.fSprites[0].scaleX = node2.size.x;
				node2.Refresh();
			}

			panel.SetBuildPos(panel.GetLastBuildPos() + new Vector2(-offset, 0f));
			panel.AddLabelledStringControl("scale", scale.ToString(), "scale", 40f, 48f, StringControl.TextIsPositiveFloat);

			panel.NewRow();
			panel.subNodes.Add(new GenericSlider(owner, "rotation", panel, panel.GetLastBuildPos(), "rotation", false, 62f, rotation) { valueRounding = 2, displayRounding = 0, maxValue = 360f });
			panel.NewRow();
			panel.AddLabelledStringControl("thickness", thickness.ToString(), "thickness",62f, 62f, StringControl.TextIsPositiveFloat);

			return panel;
		}

		public override void SingalFromDevUI(DevUISignalType type, DevUINode sender, string message)
		{
			AncientUrbanView.Building? building = sceneElement as AncientUrbanView.Building;
			if (sender is StringControl control && type == StringControl.StringFinish)
			{
				if (control.IDstring == "scale")
				{
					this.scale = float.Parse(control.actualValue);
					if (building != null)
					{
						building.scale = scale;
						building.CData().ReInitiateSprites = true;
					}
					return;
				}

				if (control.IDstring == "thickness")
				{
					this.thickness = float.Parse(control.actualValue);
					if (building != null)
					{
						building.thickness = thickness;
						building.CData().ReInitiateSprites = true;
					}
					return;
				}
			}

			else if (sender is GenericSlider slider && sender.IDstring == "rotation")
			{
				this.rotation = slider.actualValue;
				if (building != null)
				{
					building.rotation = rotation;
					building.CData().ReInitiateSprites = true;
				}

			}

				base.SingalFromDevUI(type, sender, message);
		}
	}
	public class AUV_SmokeGradient : CustomBgElement
	{
		public AUV_SmokeGradient() { }

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			base.DataFromElement(element);
			pos = new Vector2(element.pos.x, element.pos.y - (element.scene as AncientUrbanView)!.floorLevel);
		}

		public override void DataFromArgs(string[] args)
		{
			pos = new Vector2(float.Parse(args[0]), float.Parse(args[1]));
			depth = float.Parse(args[2]);
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not AncientUrbanView auv) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);
			return new AncientUrbanView.SmokeGradient(auv, new Vector2(Pos.x, auv.floorLevel + Pos.y), Depth);
		}

		public override Vector2 ScenePos => new Vector2(Pos.x, ((sceneElement?.scene as AncientUrbanView)?.floorLevel ?? 0f) + Pos.y);

		public override Vector2 ReverseScenePos(Vector2 movement) => movement * new Vector2(1f, Depth);

		public override string Serialize() => $"SmokeGradient: {pos.x}, {pos.y}, {depth}";

		public override string DevUIName()
		{
			return "SmokeGradient";
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size + new Vector2(0f, -20f));

			panel.RemoveAndCollapse("asset");
			return panel;
		}
	}

	public class RWS_RotWorm : CustomBgElement
	{
		public RWS_RotWorm() { }

		public override void DataFromArgs(string[] args)
		{
			pos = new Vector2(float.Parse(args[0]), float.Parse(args[1]));
			depth = float.Parse(args[2]);
		}

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			return new RotWorm(self, Pos, Depth);
		}

		public override Vector2 ScenePos => Pos;

		public override string Serialize() => $"RotWorm: {pos.x}, {pos.y}, {depth}";

		public override string DevUIName()
		{
			return "RotWorm";
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size + new Vector2(0f, -20f));

			panel.RemoveAndCollapse("asset");
			return panel;
		}
	}
	public class RWS_PebbsGrid : CustomBgElement
	{
		public RWS_PebbsGrid()
		{
			this.scale = 1f;
			this.perpendicular = false;
		}

		public override void DataFromElement(BackgroundScene.BackgroundSceneElement element)
		{
			//base.DataFromElement(element);
			pos = element.pos;
			depth = element.depth;
			if (element is RotWormScene.PebbsGrid grid)
			{
				scale = grid.scale;
				perpendicular = grid.perpendicular;
			}
		}

		public override void DataFromArgs(string[] args)
		{
			pos = new Vector2(float.Parse(args[0]), float.Parse(args[1]));
			depth = float.Parse(args[2]);
			scale = float.Parse(args[3]);
			perpendicular = bool.Parse(args[4]);
		}

		float scale;
		bool perpendicular;

		public override BackgroundScene.BackgroundSceneElement MakeSceneElement(BackgroundScene self)
		{
			if (self is not RotWormScene rws) throw new BackgroundBuilderException(BackgroundBuilderError.WrongVanillaBgScene);
			return new RotWormScene.PebbsGrid(rws, Pos.x, Pos.y, Depth, scale, perpendicular);
		}
		public override Vector2 ScenePos => Pos;
		public override string Serialize() => $"PebbsGrid: {pos.x}, {pos.y}, {depth}, {scale}, {perpendicular}";

		public override string DevUIName()
		{
			return "PebbsGrid";
		}

		public override BackgroundPage.ElementDataPanel MakeDevUI(DevUI owner, DevUINode parent, Vector2 pos, Vector2 size)
		{
			var panel = base.MakeDevUI(owner, parent, pos, size);

			panel.RemoveAndCollapse("asset");

			float offset = 0f;
			if (panel.TryGetExistingNode<StringControl>("depth", out var label2, out var node2))
			{
				offset = node2.size.x - 48f;
				node2.size.x = 48f;
				node2.fSprites[0].scaleX = node2.size.x;
				node2.Refresh();
			}

			panel.SetBuildPos(panel.GetLastBuildPos() + new Vector2(-offset, 0f));
			panel.AddLabelledStringControl("scale", scale.ToString(), "scale", 40f, 48f, StringControl.TextIsPositiveFloat);

			return panel;
		}
	}

	/// <summary>
	/// A simplification of BackgroundScene.PosFromDrawPosAtNeutralCam
	/// </summary>
	public static Vector2 DefaultNeutralPos(Vector2 pos, float depth) => pos * depth;

}
