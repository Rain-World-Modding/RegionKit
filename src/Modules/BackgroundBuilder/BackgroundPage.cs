using System.IO;
using System.Linq;
using DevInterface;
using RegionKit.Modules.DevUIMisc;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using Watcher;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class BuilderPageHooks
{
	public static void Apply()
	{
		On.DevInterface.DevUI.SwitchPage += DevUI_SwitchPage;
		On.DevInterface.DevUI.ctor += DevUI_ctor;
		On.DevInterface.Slider.SliderNub.Update += SliderNub_Update;
	}

	public static void Undo()
	{
		On.DevInterface.DevUI.ctor -= DevUI_ctor;
		On.DevInterface.DevUI.SwitchPage -= DevUI_SwitchPage;
		On.DevInterface.Slider.SliderNub.Update -= SliderNub_Update;
	}

	private static void SliderNub_Update(On.DevInterface.Slider.SliderNub.orig_Update orig, Slider.SliderNub self)
	{
		//stop other things from happening when sliding
		DevUINode? oldDraggedNode = self.owner?.draggedNode;
		orig(self);
		if (oldDraggedNode != null)
		{
			self.held = false;
			if (self.owner != null)
			{ self.owner.draggedNode = oldDraggedNode; }
		}
	}

	private static void DevUI_ctor(On.DevInterface.DevUI.orig_ctor orig, DevUI self, RainWorldGame game)
	{
		orig(self, game);

		AddBackgroundToPagesList(self);
	}

	private static void DevUI_SwitchPage(On.DevInterface.DevUI.orig_SwitchPage orig, DevUI self, int newPage)
	{
		AddBackgroundToPagesList(self);

		if (newPage == self.pages.IndexOf("Background"))
		{
			self.ClearSprites();
			self.activePage = new BackgroundPage(self, "Background_Page", null!, "Background");
		}

		else { orig(self, newPage); }
	}

	private static void AddBackgroundToPagesList(DevUI self)
	{

		if (!self.pages.Contains("Background"))
		{
			var list = self.pages.ToList();
			int ind = list.IndexOf("Relationships");
			if (ind == -1)
				ind = list.Count();
			list.Insert(ind, "Background");
			self.pages = list.ToArray();
		}
	}
}

public class BackgroundPage : Page
{
	// Token: 0x060028F3 RID: 10483 RVA: 0x0031DEDC File Offset: 0x0031C0DC
	public BackgroundPage(DevUI owner, string IDstring, DevUINode parentNode, string name) : base(owner, IDstring, parentNode, name)
	{
		subNodes.Add(new GenericSlider(owner, "XOffset", this, new Vector2(120f, 660f), "XOffset", true, 60f, RoomSettings.BackgroundData().roomOffset.x, stringWidth: 32) { minValue = -5000, maxValue = 5000, defaultValue = RoomSettings.parent.BackgroundData().roomOffset.x });

		subNodes.Add(new GenericSlider(owner, "YOffset", this, new Vector2(120f, 640f), "YOffset", true, 60f, RoomSettings.BackgroundData().roomOffset.y, stringWidth: 32) { minValue = -5000, maxValue = 5000, defaultValue = RoomSettings.parent.BackgroundData().roomOffset.y });

		backgroundSave = new PanelSelectButton(owner, "BackgroundSave", this, new Vector2(260f, 620f), 30f, "...", backgroundSaves(), "Select Background", "...") { panelPos = new Vector2(420f, 190f) };
		backgroundSave.itemDescription = "background file to save to/load from";
		subNodes.Add(backgroundSave);

		saveName = new BackgroundFileStringControl(owner, "saveName", this, new Vector2(120f, 620f), 128f, RoomSettings.BackgroundData().backgroundName, BackgroundFileStringControl.TextIsValidBackground)
		{
			toolTipTextOverride = "Input the name of the background file to save/load"
		};
		subNodes.Add(saveName);

		Button buttonLoad = new Button(owner, "Load", this, new Vector2(120f, 600f), 60f, "Load");
		API.Iggy.AddTooltip(buttonLoad, () => new("Load background scene setup from file", 10, buttonLoad));
		subNodes.Add(buttonLoad);


		subNodes.Add(new Button(owner, "Save", this, new Vector2(200f, 600f), 60f, "Save"));


		backgroundType = new ExtEnumCycler<BackgroundTemplateType>(owner, "BackTypeCycle", this, new Vector2(170f, 580f), 120f, RoomSettings.BackgroundData().type, "Type");
		subNodes.Add(backgroundType);

		dragMode = new ExtEnumCycler<ElementDragMode>(owner, "DragMode", this, new Vector2(326f, 692f), 60f, ElementDragMode.None, "Drag Mode", 80f);
		subNodes.Add(dragMode);

		RefreshAllNodes();
	}

	internal Dictionary<Handle, BackgroundElementData.CustomBgElement> handles = new();

	public PanelSelectButton backgroundSave;

	public ExtEnumCycler<BackgroundTemplateType> backgroundType;

	public ExtEnumCycler<ElementDragMode> dragMode;

	public StringControl saveName;

	public DevUINode? SpecificSettingsNode;

	public ElementListPanel? ElementListNode;

	public List<ElementDataPanel> elementDataPanels = new();

	public override void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (type == DevUISignalType.ButtonClick)
		{
			if (sender.IDstring == backgroundType.IDstring)
			{
				//RoomSettings.BackgroundData().type = backgroundType.Type;
				SwitchRoomBackground(owner.room, backgroundType.Type);
				RefreshAllNodes();
			}

			if (sender.IDstring == "Save_Settings")
			{ RoomSettings.Save(); }

			else if (sender == backgroundSave && message == "")
			{
				backgroundSave.values = backgroundSaves();
			}

			else if (sender == backgroundSave)
			{
				if (Data.TryGetPathFromName(message, out _))
				{
					saveName.Text = message;
					saveName.actualValue = message;
					saveName.Refresh();
				}
				else if (message == "<Default>")
				{
					saveName.Text = message;
					saveName.actualValue = "";
					saveName.Refresh();
				}
				backgroundSave.Text = "...";
			}

			else if (sender.IDstring == "Load")
			{
				RoomSettings.BackgroundData().FromTimeline(saveName.actualValue, owner.game.TimelinePoint);
				SwitchRoomBackground(owner.room, RoomSettings.BackgroundData().type, true);
				backgroundType.Type = RoomSettings.BackgroundData().type;
				backgroundType.Refresh();

				RefreshAllNodes();
			}

			else if (sender.IDstring == "Save")
			{
				Debug.Log($"\n\nBACKGROUND OUTPUT\n\n{string.Join("\n", RoomSettings.BackgroundData().Serialize())}\n\n");
			}

			else if (sender == dragMode)
			{
				ReloadHandles();
			}

			else if (sender is ElementListPanel.GroupButton.DropdownButton)
			{
				ReloadHandles();
			}

			else if (sender is ElementListPanel.ElementDataButton elementDataButton)
			{
				if (sender is ElementListPanel.AddButton s)
				{
					Vector2 newPos = ElementListNode?.pos ?? new Vector2(1000f, 100f);
					newPos += new Vector2(-200f, 100f);
					var names = GetAvailableElementNames();
					Vector2 size = new Vector2(155f, Mathf.Min(names.Length, 20) * 20f + 60f);
					subNodes.Add(new AddNewPanel(owner, this, newPos, size, names, "addPanel", "Add New Element", s.element as BackgroundElementData.BG_ElementGroup));
				}
				else
				{
					AddOrRemoveElementDataPanel(elementDataButton.element);
				}

			}

			else if (sender.parentNode is AddNewPanel addNew && sender is Button button)
			{
				addNew.ClearSprites();
				subNodes.Remove(addNew);
				if (button.Text != "Cancel")
				{

					var a = BackgroundElementData.MakeBlank(button.Text, RoomSettings.BackgroundData().sceneData);
					RoomSettings.BackgroundData().sceneData.AddNewBackgroundElement(a, addNew.group);
					AddOrRemoveElementDataPanel(a);
					//RefreshSpecificNode();
					ElementListNode?.RefreshElements();
					ReloadHandles();
				}
			}

			else if (sender is ElementListPanel.ElementDataButton.DeleteButton && sender.parentNode is ElementListPanel.ElementDataButton elementButton)
			{
				RoomSettings.BackgroundData().sceneData.RemoveBackgroundElement(elementButton.element);
				//RefreshSpecificNode();
				ElementListNode?.RefreshElements();
				ReloadHandles();
			}
		}

		else if (type == GenericSlider.SliderUpdate)
		{
			if (sender.IDstring == "XOffset" && sender is GenericSlider slider)
			{
				RoomSettings.BackgroundData().roomOffset.x = slider.actualValue;
				RoomSettings.BackgroundData().sceneData.UpdateSceneElement("");
				RoomSettings.BackgroundData().UpdateOffsetInit();
			}

			else if (sender.IDstring == "YOffset" && sender is GenericSlider slider2)
			{
				RoomSettings.BackgroundData().roomOffset.y = slider2.actualValue;
				RoomSettings.BackgroundData().sceneData.UpdateSceneElement("");
				RoomSettings.BackgroundData().UpdateOffsetInit();
			}
		}
		else if (type == StringControl.StringFinish)
		{
			if (sender.IDstring == "str" && sender is StringControl node)
			{
				RoomSettings.BackgroundData().roomOffset.x = float.Parse(node.actualValue);
			}
		}
	}

	public class AddNewPanel : ItemSelectPanel
	{
		internal BackgroundElementData.BG_ElementGroup? group;
		internal AddNewPanel(DevUI owner, DevUINode parentNode, Vector2 pos, Vector2 size, string[] items, string idstring, string title, BackgroundElementData.BG_ElementGroup? group)
			: base(owner, parentNode, pos, items, idstring, title, size: size, columns: 1)
		{
			this.group = group;
		}
	}


	public string[] GetAvailableElementNames()
	{
		List<string> result = new();
		result.Add("Cancel");

		foreach ((var key, var val) in Registry.BackgroundTypeNames)
		{
			if (RoomSettings.BackgroundData().sceneData.ElementAllowedInScene(key))
			{
				result.Add(val);
			}
		}

		return result.ToArray();
	}

	public static void SwitchRoomBackground(Room self, BackgroundTemplateType type, bool refresh = false)
	{
		BackgroundScene? newBackground = null;
		if (Registry.SceneMakerRegistry.TryGetValue(type, out Registry.SceneMaker sceneMaker))
		{
			newBackground = sceneMaker(self);
		}
		List<BackgroundScene> foundScenes = new();
		BackgroundScene? lookingForScene = null;
		foreach (UpdatableAndDeletable uad in self.updateList)
		{
			if (uad is BackgroundScene bgs)
			{
				if (newBackground != null && bgs.GetType() == newBackground.GetType() && !refresh)
					lookingForScene = bgs;
				else
					foundScenes.Add(bgs);
			}
		}
		foreach (var s in foundScenes)
		{
			s.Destroy();
			self.RemoveObject(s);
		}

		if (lookingForScene == null)
		{
			self.AddObject(newBackground);
		}
	}

	internal void AddOrRemoveElementDataPanel(BackgroundElementData.CustomBgElement element)
	{
		for (int i = 0; i < elementDataPanels.Count; i++)
		{
			if (elementDataPanels[i].element == element)
			{
				elementDataPanels[i].ClearSprites();
				subNodes.Remove(elementDataPanels[i]);
				elementDataPanels.RemoveAt(i);
				return;
			}
		}
		var newPanel = element.MakeDevUI(owner, this, new Vector2(100f, 100f), BackgroundElementData.CustomBgElement.DefaultPanelSize);

		elementDataPanels.Add(newPanel);
		subNodes.Add(newPanel);
	}

	public override void Update()
	{
		base.Update();

		UpdateDrag();
	}

	public void RefreshAllNodes()
	{
		ReloadHandles();
		RefreshElementListNode();
		RefreshSpecificNode();

		foreach (var s in elementDataPanels)
		{
			s.ClearSprites();
			subNodes.Remove(s);
		}
		elementDataPanels.Clear();
	}

	public void RefreshSpecificNode()
	{
		if (SpecificSettingsNode != null)
		{
			SpecificSettingsNode.ClearSprites();
			this.subNodes.Remove(SpecificSettingsNode);
			SpecificSettingsNode = null;
		}
		//BackgroundTemplateType type = RoomSettings.BackgroundData().type;
		//backgroundType.Type = type;
		//backgroundType.Refresh();
		//RefreshElementNode();

		SpecificSettingsNode = RoomSettings.BackgroundData().sceneData.MakeDevUI(owner, this);

		if (SpecificSettingsNode != null)
		{ subNodes.Add(SpecificSettingsNode); }
	}

	public void RefreshElementListNode()
	{
		if (ElementListNode != null)
		{
			ElementListNode.ClearSprites();
			this.subNodes.Remove(ElementListNode);
			ElementListNode = null;
		}
		ElementListNode = new ElementListPanel(owner, "element list", this, new Vector2(1000f, 100f), new Vector2(300f, 500f), "Element List", false);

		if (ElementListNode != null)
		{ subNodes.Add(ElementListNode); }
	}




	#region DragFunctionality
	public void ReloadHandles()
	{
		foreach (var m in handles.Keys)
		{
			m.ClearSprites();
			subNodes.Remove(m);
		}
		handles.Clear();

		if (dragMode.Type != ElementDragMode.Handles || ElementListNode == null)
			return;

		foreach (var m in (ElementListNode as ElementListPanel.ICollectElementButtons).GetRevealedElements)
		{
			var h = new BackgroundHandle(owner, "handle", this, m);
			subNodes.Add(h);
			handles.Add(h, m);
		}
	}

	BackgroundElementData.CustomBgElement? bgElement;
	Vector2 oldMousePos;
	bool lastClicked = false;
	public class ElementDragMode : ExtEnum<ElementDragMode>
	{
		public static readonly ElementDragMode Handles = new ElementDragMode(nameof(Handles), true);
		public static readonly ElementDragMode Sprites = new ElementDragMode(nameof(Sprites), true);
		public static readonly ElementDragMode None = new ElementDragMode(nameof(None), true);

		public ElementDragMode(string value, bool register = false) : base(value, register)
		{
		}
	}

	public void UpdateDrag()
	{

		if (dragMode.Type == ElementDragMode.Handles)
		{
			if (owner.draggedNode is Handle handle && handle.dragged && handles.TryGetValue(handle, out var val))
			{
				if (bgElement == null)
				{
					bgElement = val;
					oldMousePos = new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
				}
			}
			else
			{
				bgElement = null;
			}
		}
		else if (dragMode.Type == ElementDragMode.Sprites)
		{
			if (Input.GetMouseButton(0))
			{
				if (!lastClicked && bgElement == null && ElementListNode != null)
				{
					lastClicked = true;
					foreach (var element in (ElementListNode as ElementListPanel.ICollectElementButtons).GetRevealedElements)
					{
						if (element.ElementClicked(owner.game.cameras[0]))
						{
							bgElement = element;
							oldMousePos = new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
							break;
						}
					}
				}
			}
			else
			{
				bgElement = null;
				lastClicked = false;
			}
		}
		else
		{
			bgElement = null;
			lastClicked = false;
		}

		if (bgElement != null)
		{
			bool controlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

			var vector = new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
			Vector2 movement = vector - oldMousePos;
			bgElement.Dragged(movement, controlPressed);
			//if (controlPressed)
			//{
			//	bgElement.Depth += Futile.mousePosition.y - oldMousePos.y;
			//	if (bgElement.sceneElement is AboveCloudsView.DistantBuilding dbuilding)
			//	{

			//		dbuilding.atmosphericalDepthAdd += Futile.mousePosition.x - oldMousePos.x;
			//	}

			//	else if (bgElement.sceneElement is RoofTopView.DistantBuilding rfdbuilding)
			//	{
			//		rfdbuilding.atmosphericalDepthAdd += Futile.mousePosition.x - oldMousePos.x;
			//	}

			//}
			//else
			//{
			//	bgElement.Pos += movement;
			//}

			oldMousePos = Futile.mousePosition;
		}
	}

	internal class BackgroundHandle : Handle
	{
		BackgroundElementData.CustomBgElement bgElement;

		internal BackgroundHandle(DevUI owner, string IDstring, DevUINode parentNode, BackgroundElementData.CustomBgElement bgElement) : base(owner, IDstring, parentNode, Vector2.zero)
		{
			this.bgElement = bgElement;
			Refresh();
		}

		public override void Update()
		{
			bool lastDragged = this.dragged;
			base.Update();
			if (!this.dragged)
			{
				var cam = owner.game.cameras[0];
				var shouldPos = bgElement.DevUIHandlePos(cam);
				if (absPos != shouldPos)
					Refresh();
			}
		}
		public override void Refresh()
		{
			if (!this.dragged)
			{
				var cam = owner.game.cameras[0];
				this.absPos = bgElement.DevUIHandlePos(cam);
			}
			base.Refresh();
		}
	}
	#endregion

	public class BackgroundFileStringControl : StringControl, Modules.Iggy.IGiveAToolTip
	{
		public string? toolTipTextOverride;
		Iggy.ToolTip? Iggy.IGiveAToolTip.ToolTip => new(toolTipTextOverride ?? "Input a value as a string", 10, this);

		bool Iggy.IGeneralMouseOver.MouseOverMe => MouseOver;

		public BackgroundFileStringControl(
			DevUI owner,
			string IDstring,
			DevUINode parentNode,
			Vector2 pos,
			float width,
			string text,
			IsTextValid del) : base(
				owner,
				IDstring,
				parentNode,
				pos,
				width,
				text,
				del)
		{
		}

		protected override void TrySetValue(string newValue, bool endTransaction)
		{
			if (isTextValid(newValue))
			{
				actualValue = newValue;
				if (Data.TryGetPathFromName(actualValue, out _))
					fLabels[0].color = Color.yellow;

				else
					fLabels[0].color = new Color(0.1f, 0.4f, 0.2f);

				this.SendSignal(StringEdit, this, "");
			}
			else
			{
				fLabels[0].color = Color.red;
			}
			if (endTransaction)
			{
				Text = actualValue;
				fLabels[0].color = Color.black;
				Refresh();
				this.SendSignal(StringFinish, this, "");
			}
		}
		public override void Refresh()
		{
			base.Refresh();
			if (actualValue == "")
			{ Text = "<Default>"; }
			else { Text = actualValue; }

			if (actualValue.ToLower() == RoomSettings.parent.BackgroundData().backgroundName.ToLower())
			{ Text = "<T>" + Text; }
		}
		public static bool TextIsValidBackground(string value)
		{ return Data.TryGetPathFromName(value, out _) || value == ""; }
	}

	public class ElementDataPanel : Panel, IDevUISignals
	{
		internal BackgroundElementData.CustomBgElement element;
		FSprite lineSprite;

		readonly Dictionary<string, (DevUILabel?, PositionedDevUINode)> LabelledNodes = new();
		internal ElementDataPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, BackgroundElementData.CustomBgElement element) : base(owner, IDstring, parentNode, pos, size, element.DevUIName())
		{
			buildPos = new Vector2(5f, size.y - 20f);
			this.element = element;
			this.fSprites.Add(lineSprite = new FSprite("pixel", true) { anchorY = 0f });
			owner.placedObjectsContainer.AddChild(lineSprite);
		}

		Vector2 buildPos;
		public void AddLabelledStringControl(string displayName, string defaultValue, string id, float labelWidth, float controlWidth, StringControl.IsTextValid? del = null)
		{
			if (buildPos.x > 5f && labelWidth + controlWidth + buildPos.x + 4f > this.size.x)
				NewRow();

			del ??= StringControl.TextIsAny;
			var l = new DevUILabel(owner, "label_" + id, this, buildPos, labelWidth, displayName);
			subNodes.Add(l);
			buildPos.x += labelWidth + 4f;
			var s = new StringControl(owner, id, this, buildPos, controlWidth, defaultValue, del);
			subNodes.Add(s);
			buildPos.x += controlWidth + 6f;

			LabelledNodes[id] = (l, s);
		}

		public void AddLabelledUINode(string displayName, float labelWidth, RectangularDevUINode node, bool noAutoPos = false)
		{

			if (!noAutoPos && buildPos.x > 5f && labelWidth + node.size.x + buildPos.x + 4f > this.size.x)
				NewRow();

			Vector2 labelPos = noAutoPos ? node.pos - new Vector2(labelWidth + 4f, 0f) : buildPos;
			var s = new DevUILabel(owner, "label_" + node.IDstring, this, labelPos, labelWidth, displayName);
			subNodes.Add(s);
			if (!noAutoPos)
			{
				buildPos.x += labelWidth + 4f;
				node.Move(buildPos);
			}
			subNodes.Add(node);
			if(!noAutoPos)
				buildPos.x += node.size.x + 4f;

			LabelledNodes[node.IDstring] = (s, node);
		}

		public bool TryGetExistingNode<T>(string id, out DevUILabel? label, out T node) where T : DevUINode
		{
			label = null;
			node = null!;

			if (LabelledNodes.TryGetValue(id, out (DevUILabel?, PositionedDevUINode) val) && val.Item2 is T t)
			{
				label = val.Item1;
				node = t;
				return true;
			}
			return false;
		}

		public void RemoveAndCollapse(string id)
		{
			if (TryGetExistingNode(id, out var label, out PositionedDevUINode node))
			{
				if (label != null)
				{
					label.ClearSprites();
					subNodes.Remove(label);
				}
				node.ClearSprites();
				subNodes.Remove(node);

				float height = node.pos.y;

				LabelledNodes.Remove(id);

				foreach ((DevUILabel? label2, PositionedDevUINode? node2) in LabelledNodes.Values)
				{
					if (label2 != null && label2.pos.y < height)
						label2.Move(label2.pos + new Vector2(0f, 20f));
					
					if (node2 != null && node2.pos.y < height)
						node2.Move(node2.pos + new Vector2(0f, 20f));
				}
			}
		}

		public void NewRow()
		{
			buildPos.x = 5f;
			buildPos.y -= 20f;
		}

		public Vector2 GetLastBuildPos() => buildPos;

		public void SetBuildPos(Vector2 newPos) => buildPos = newPos;

		public override void Update()
		{
			base.Update();

			if (element.deleted)
			{
				this.ClearSprites();
				(parentNode as BackgroundPage)?.elementDataPanels.Remove(this);
				parentNode.subNodes.Remove(this);
				return;
			}


				string s = element.DevUIName();
				if (this.Title != s)
					this.Title = s;
				Refresh();
			

			if (element.DevUIHandlePos(owner.game.cameras[0]) != assetPos)
				Refresh();

			if (TryGetExistingNode<DevUILabel>("group", out _, out var node) && node.Text != (element.group?.name ?? "None"))
			{
				node.Text = (element.group?.name ?? "None");
				node.Refresh();
			}
		}

		Vector2 assetPos;

		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			element.SingalFromDevUI(type, sender, message);
		}

		public override void Refresh()
		{
			base.Refresh();
			if (collapsed)
			{
				lineSprite.scaleY = 1f;
				base.MoveSprite(fSprites.IndexOf(lineSprite), new Vector2(-1000f, -1000f));
				return;
			}
			Vector2 from = base.absPos;
			Vector2 to = element.DevUIHandlePos(owner.game.cameras[0]);
			assetPos = to;
			base.MoveSprite(fSprites.IndexOf(lineSprite), from);
			lineSprite.scaleY = Vector2.Distance(from, to);
			lineSprite.rotation = Custom.AimFromOneVectorToAnother(from, to);

			element.RefreshDevUI(this);
		}
	}

	public class ElementAssetSelectButton : PanelSelectButton
	{
		public ElementAssetSelectPanel.SelectGroup defaultSelectGroup;
		public ElementAssetSelectButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text, string panelName, ElementAssetSelectPanel.SelectGroup defaultSelectGroup) : base(owner, IDstring, parentNode, pos, width, text, new string[0], panelName, text)
		{
			this.panelPos = new Vector2(200f, -20f);
			this.defaultSelectGroup = defaultSelectGroup;
		}

		public override void Clicked()
		{
			//sending signal first in case values wanna be changed
			//base.Clicked();

			if (itemSelectPanel != null)
			{
				subNodes.Remove(itemSelectPanel);
				itemSelectPanel.ClearSprites();
				itemSelectPanel = null;
			}
			else
			{
				itemSelectPanel = new ElementAssetSelectPanel(owner, this, panelPos - pos, IDstring + "_Panel", panelName, defaultSelectGroup);
				subNodes.Add(itemSelectPanel);
			}
		}
	}

	public class ElementAssetSelectPanel : ItemSelectPanel
	{
		SelectGroup selectGroup;
		bool updateGroup = false;
		public ElementAssetSelectPanel(DevUI owner, DevUINode parentNode, Vector2 pos, string idstring, string title, SelectGroup selectGroup) : base(owner, parentNode, pos, new string[0], idstring, title, new Vector2(180f, 400f), 170, 1)
		{
			this.perpage -= 3;
			this.selectGroup = selectGroup;
			SwitchGroup(selectGroup);
		}

		public override void Update()
		{
			if (updateGroup)
			{
				SwitchGroup(selectGroup);
				updateGroup = false;
			}

			base.Update();
		}

		public override void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			var newSelectGroup = new SelectGroup(sender.IDstring, false);
			if (newSelectGroup.index != -1)
			{
				if (selectGroup != newSelectGroup)
				{
					selectGroup = newSelectGroup;
					updateGroup = true;
				}
			}
			else
				base.Signal(type, sender, message);
		}

		public void SwitchGroup(SelectGroup selectGroup)
		{
			this.items = GetGroupItems(selectGroup);
			this.PopulateItems(0);
		}

		static int count = 0;
		public override void PopulateItems(int offset)
		{
			base.PopulateItems(offset);

			count++;

			foreach (var s in subNodes)
			{
				if (s.IDstring == "BackPage99289..?/~" || s.IDstring == "NextPage99289..?/~")
					continue;
				if (s is Button button)
					button.Move(button.pos + new Vector2(0f, -60f));
			}

			SelectGroup[] firstRow = [SelectGroup.ATC, SelectGroup.RFV, SelectGroup.PNK, SelectGroup.AUV, SelectGroup.OTR];
			SelectGroup[] secondRow = [SelectGroup.Illustrations, SelectGroup.Sprites];

			Vector2 ppos = new Vector2(5f, size.y - 25f);
			float buttonInterval = (size.x - 10f) / firstRow.Length;
			float buttonSpace = 5f;
			foreach (var g in firstRow)
			{
				subNodes.Add(new Button(owner, g.value, this, ppos, buttonInterval - buttonSpace, g.value));
				ppos.x += buttonInterval;
			}
			ppos.x = 5f;
			ppos.y -= 20f;
			buttonInterval = (size.x - 10f) / secondRow.Length;
			buttonSpace = 5f;
			foreach (var g in secondRow)
			{
				subNodes.Add(new Button(owner, g.value, this, ppos, buttonInterval - buttonSpace, g.value));
				ppos.x += buttonInterval;
			}
		}

		public class SelectGroup : ExtEnum<SelectGroup>
		{
			public static readonly SelectGroup ATC = new(nameof(ATC), true);
			public static readonly SelectGroup RFV = new(nameof(RFV), true);
			public static readonly SelectGroup PNK = new(nameof(PNK), true);
			public static readonly SelectGroup OTR = new(nameof(OTR), true);
			public static readonly SelectGroup AUV = new(nameof(AUV), true);
			public static readonly SelectGroup Illustrations = new(nameof(Illustrations), true);
			public static readonly SelectGroup Sprites = new(nameof(Sprites), true);

			public SelectGroup(string value, bool register = false) : base(value, register)
			{
			}
		}

		public static string[] GetGroupItems(SelectGroup selectGroup)
		{
			List<string> result = new();

			string? startWithString = null;

			if (selectGroup == SelectGroup.ATC)
				startWithString = "atc_";
			if (selectGroup == SelectGroup.RFV)
				startWithString = "rf_";
			if (selectGroup == SelectGroup.PNK)
				startWithString = "pnk_";
			if (selectGroup == SelectGroup.OTR)
				startWithString = "otr_";
			if (selectGroup == SelectGroup.AUV)
				startWithString = "au_";

			if (selectGroup != SelectGroup.Sprites)
			{
				foreach (string str in AssetManager.ListDirectory("Illustrations"))
				{
					string str2 = Path.GetFileNameWithoutExtension(str);
					if(startWithString == null || str2.ToLower().StartsWith(startWithString))
						result.Add(str2);
				}
			}

			if (selectGroup != SelectGroup.Illustrations)
			{
				foreach ((string str, FAtlasElement _) in Futile.atlasManager._allElementsByName)
				{
					string str2 = Path.GetFileNameWithoutExtension(str);
					if (startWithString == null || str2.ToLower().StartsWith(startWithString))
						result.Add(str2);
				}
			}

			return result.ToArray();
		}
	}

	public static string[] backgroundSaves() => AssetManager.ListDirectory(_Module.BGPath).Where(x => File.ReadAllLines(x)[0] != "UNLISTED").Select(i => Path.GetFileNameWithoutExtension(i)).Prepend("<Default>").ToArray();
}
