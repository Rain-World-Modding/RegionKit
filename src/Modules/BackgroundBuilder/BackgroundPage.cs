using System.IO;
using System.Linq;
using DevInterface;
using RegionKit.Modules.DevUIMisc;
using RegionKit.Modules.DevUIMisc.GenericNodes;

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
		orig(self);
		if (self.owner.draggedNode != null)
		{
			self.held = false;
			return;
		}
		if (self.held)
		{ self.owner.draggedNode = self; }
	}

	private static void DevUI_ctor(On.DevInterface.DevUI.orig_ctor orig, DevUI self, RainWorldGame game)
	{
		orig(self, game);

		if (!self.pages.Contains("Background"))
		{
			var list = self.pages.ToList();
			list.Add("Background");
			self.pages = list.ToArray();
		}
	}

	private static void DevUI_SwitchPage(On.DevInterface.DevUI.orig_SwitchPage orig, DevUI self, int newPage)
	{
		if (!self.pages.Contains("Background"))
		{
			var list = self.pages.ToList();
			list.Add("Background");
			self.pages = list.ToArray();
		}

		if (newPage == self.pages.IndexOf("Background"))
		{
			self.ClearSprites();
			self.activePage = new BackgroundPage(self, "Background_Page", null!, "Background");
		}

		else { orig(self, newPage); }
	}
}

public class BackgroundPage : Page
{
	// Token: 0x060028F3 RID: 10483 RVA: 0x0031DEDC File Offset: 0x0031C0DC
	public BackgroundPage(DevUI owner, string IDstring, DevUINode parentNode, string name) : base(owner, IDstring, parentNode, name)
	{
		subNodes.Add(new GenericSlider(owner, "XOffset", this, new Vector2(120f, 660f), "XOffset", true, 60f, RoomSettings.BackgroundData().roomOffset.x, stringWidth: 32) { minValue = -5000, maxValue = 5000, defaultValue = RoomSettings.parent.BackgroundData().roomOffset.x });

		subNodes.Add(new GenericSlider(owner, "YOffset", this, new Vector2(120f, 640f), "YOffset", true, 60f, RoomSettings.BackgroundData().roomOffset.y, stringWidth: 32) { minValue = -5000, maxValue = 5000, defaultValue = RoomSettings.parent.BackgroundData().roomOffset.y });

		backgroundSave = new PanelSelectButton(owner, "BackgroundSave", this, new Vector2(260f, 620f), 30f, "...", backgroundSaves(), "Select Background") { panelPos = new Vector2(420f, 190f) };
		backgroundSave.itemDescription = "background file to save to/load from";
		subNodes.Add(backgroundSave);

		saveName = new BackgroundFileStringControl(owner, "saveName", this, new Vector2(120f, 620f), 128f, RoomSettings.BackgroundData().backgroundName, BackgroundFileStringControl.TextIsValidBackground)
		{
			toolTipTextOverride = "Input the name of the background file to save/load"
		};
		subNodes.Add(saveName);
		//subNodes.Add(new Button(owner, "refresh", this, new Vector2(300f, 620f), 60f, "refresh"));

		Button buttonLoad = new Button(owner, "Load", this, new Vector2(120f, 600f), 60f, "Load");
		API.Iggy.AddTooltip(buttonLoad, () => new("Load background scene setup from file", 10, buttonLoad));
		subNodes.Add(buttonLoad);


		//subNodes.Add(new Button(owner, "Save", this, new Vector2(200f, 600f), 60f, "Save"));


		//backgroundType = new ExtEnumCycler<BackgroundTemplateType>(owner, "BackTypeCycle", this, new Vector2(170f, 580f), 120f, RoomSettings.BackgroundData().type, "Type");
		//subNodes.Add(backgroundType);
	}

	public PanelSelectButton backgroundSave;

	//public ExtEnumCycler<BackgroundTemplateType> backgroundType;

	public StringControl saveName;

	public DevUINode? SpecificSettingsNode;

	public override void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (type == DevUISignalType.ButtonClick)
		{
			/*if (sender.IDstring == backgroundType.IDstring)
			{
				//RoomSettings.BackgroundData().type = backgroundType.Type;
				//SwitchRoomBackground(owner.room, backgroundType.Type);
				//RefreshSpecificNode();
			}*/

			if (sender.IDstring == "Save_Settings")
			{ RoomSettings.Save(); }

			else if (sender.IDstring == backgroundSave.IDstring)
			{
				backgroundSave.values = backgroundSaves();
			}

			else if (backgroundSave.IsSubButtonID(sender.IDstring, out string subButtonID))
			{
				if (Data.TryGetPathFromName(subButtonID, out _))
				{
					saveName.Text = subButtonID;
					saveName.actualValue = subButtonID;
					saveName.Refresh();
				}
				else if (subButtonID == "<Default>")
				{
					saveName.Text = subButtonID;
					saveName.actualValue = "";
					saveName.Refresh();
				}
			}

			else if (sender.IDstring == "refresh")
			{
				SwitchRoomBackground(owner.room, RoomSettings.BackgroundData().type, true);
				//RefreshSpecificNode();
			}

			else if (sender.IDstring == "Load")
			{
				RoomSettings.BackgroundData().FromTimeline(saveName.actualValue, owner.game.TimelinePoint);
				SwitchRoomBackground(owner.room, RoomSettings.BackgroundData().type, true);
			}

			else if (sender.IDstring == "Save")
			{
				Debug.Log($"\n\nBACKGROUND OUTPUT\n\n{string.Join("\n", RoomSettings.BackgroundData().Serialize())}\n\n");
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
	public static void SwitchRoomBackground(Room self, BackgroundTemplateType type, bool refresh = false)
	{
		AboveCloudsView? aboveCloudsView = null;
		RoofTopView? roofTopView = null;
		VoidSea.VoidSeaScene? voidSeaView = null;
		foreach (UpdatableAndDeletable uad in self.updateList)
		{
			if (uad is BackgroundScene)
			{
				if (uad is AboveCloudsView acv)
				{ aboveCloudsView = acv; }

				else if (uad is RoofTopView rtv)
				{ roofTopView = rtv; }

				else if (uad is VoidSea.VoidSeaScene vss)
				{ voidSeaView = vss; }
			}
		}
		if (aboveCloudsView != null && (type != BackgroundTemplateType.AboveCloudsView || refresh))
		{
			aboveCloudsView.Destroy();
			self.RemoveObject(aboveCloudsView);
			aboveCloudsView = null;
		}

		if (roofTopView != null && (type != BackgroundTemplateType.RoofTopView || refresh))
		{
			roofTopView.Destroy();
			self.RemoveObject(roofTopView);
			roofTopView = null;
		}

		if (voidSeaView != null && (type != BackgroundTemplateType.VoidSeaScene || refresh))
		{
			voidSeaView.Destroy();
			voidSeaView.RemoveFromRoom();
			voidSeaView = null;
		}

		if (aboveCloudsView == null && type == BackgroundTemplateType.AboveCloudsView)
		{
			self.AddObject(new AboveCloudsView(self, new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.AboveCloudsView, 0f, false)));
		}

		if (roofTopView == null && type == BackgroundTemplateType.RoofTopView)
		{
			self.AddObject(new RoofTopView(self, new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.RoofTopView, 0f, false)));
		}

		if (voidSeaView == null && type == BackgroundTemplateType.VoidSeaScene)
		{
			//owner.room.AddObject(new VoidSea.VoidSeaScene(owner.room));
		}
	}



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

	public class ElementAssetSelect : PositionedDevUINode, IDevUISignals
	{
		public ElementAssetSelect(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
		{
			Vector2 ppos = Vector2.zero;

			label = new DevUILabel(owner, "Asset_Label", this, ppos, 120f, "");
			subNodes.Add(label);
			ppos.x += 140f;

			select = new PanelSelectButton(owner, "Asset_Select", this, ppos, 30f, "...", ElementNames().ToArray(), "Select Element")
			{ panelPos = new Vector2(420f, -200f), panelSize = new Vector2(280f, 420f), panelButtonWidth = 240f, panelColumns = 1 };
			subNodes.Add(select);
		}

		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender.IDstring == select.IDstring)
			{ select.values = ElementNames().ToArray(); }

			if (select.IsSubButtonID(sender.IDstring, out string subButtonID))
			{
				label.Text = subButtonID;
				this.SendSignal(type, this, message);
			}
		}

		public DevUILabel label;

		public PanelSelectButton select;

		public static List<string> ElementNames()
		{
			List<string> result = new();
			foreach (string str in AssetManager.ListDirectory("Illustrations"))
			{
				string str2 = Path.GetFileNameWithoutExtension(str);
				if (str2.ToLower().StartsWith("atc_") || str2.ToLower().StartsWith("rf_"))
					result.Add(str2);
			}

			return result;
		}

	}
	public static string[] backgroundSaves() => AssetManager.ListDirectory(_Module.BGPath).Where(x => File.ReadAllLines(x)[0] != "UNLISTED").Select(i => Path.GetFileNameWithoutExtension(i)).Prepend("<Default>").ToArray();
}
