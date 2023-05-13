using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DevInterface;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using System.Linq;
using System.ComponentModel;
using RegionKit.Modules.DevUIMisc;
using System.Text.RegularExpressions;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class BuilderPage
{
	public static void Apply()
	{
		On.DevInterface.DevUI.ctor += DevUI_ctor;
		On.DevInterface.DevUI.SwitchPage += DevUI_SwitchPage;
		On.DevInterface.Slider.SliderNub.Update += SliderNub_Update;
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

	public static void Undo()
	{
		On.DevInterface.DevUI.ctor -= DevUI_ctor;
		On.DevInterface.DevUI.SwitchPage -= DevUI_SwitchPage;
		On.DevInterface.Slider.SliderNub.Update -= SliderNub_Update;
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
			self.activePage = new BackgroundPage(self, "Background_Page", null!, "Background Settings");
		}

		else { orig(self, newPage); }
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

	public class BackgroundPage : Page
	{
		// Token: 0x060028F3 RID: 10483 RVA: 0x0031DEDC File Offset: 0x0031C0DC
		public BackgroundPage(DevUI owner, string IDstring, DevUINode parentNode, string name) : base(owner, IDstring, parentNode, name)
		{
			subNodes.Add(new GenericSlider(owner, "XOffset", this, new Vector2(120f, 660f), "XOffset", true, 60f, minValue: -5000, maxValue: 5000, initialValue: RoomSettings.BackgroundData().roomOffset.x));

			subNodes.Add(new GenericSlider(owner, "YOffset", this, new Vector2(120f, 640f), "YOffset", true, 60f, minValue: -5000, maxValue: 5000, initialValue: RoomSettings.BackgroundData().roomOffset.y));

			backgroundSave = new PanelSelectButton(owner, "BackgroundSave", this, new Vector2(260f, 620f), 30f, "...", backgroundSaves(), "Select Background");
			subNodes.Add(backgroundSave);
			saveName = new BackgroundFileStringControl(owner, "saveName", this, new Vector2(120f, 620f), 130f, RoomSettings.BackgroundData().backgroundName, StringControl.TextIsValidFilename);
			subNodes.Add(saveName);
			subNodes.Add(new Button(owner, "refresh", this, new Vector2(300f, 620f), 60f, "refresh"));

			subNodes.Add(new Button(owner, "Load", this, new Vector2(120f, 600f), 60f, "Load"));

			subNodes.Add(new Button(owner, "Save", this, new Vector2(200f, 600f), 60f, "Save"));


			backgroundType = new ExtEnumCycler<Data.BackgroundTemplateType>(owner, "BackTypeCycle", this, new Vector2(170f, 580f), 120f, RoomSettings.BackgroundData().type, "Type");
			subNodes.Add(backgroundType);

			elementData = new BackgroundElementUINode(owner, "elementData", this, new Vector2(120f, 340f));
			subNodes.Add(elementData);
			RefreshSpecificNode();
		}

		public override void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			if (type == DevUISignalType.ButtonClick)
			{
				if (sender.IDstring == backgroundType.IDstring)
				{
					RoomSettings.BackgroundData().type = backgroundType.Type;
					SwitchRoomBackground(owner.room, backgroundType.Type);
					RefreshSpecificNode();
				}

				else if (sender.IDstring == backgroundSave.IDstring)
				{
					backgroundSave.values = backgroundSaves();
				}

				else if (backgroundSave.IsSubButtonID(sender.IDstring, out string subButtonID))
				{
					if (Data.PathFromName(subButtonID, out _))
					{
						saveName.Text = subButtonID;
						saveName.actualValue = subButtonID;
					}
				}

				else if (sender.IDstring == "refresh")
				{
					SwitchRoomBackground(owner.room, RoomSettings.BackgroundData().type, true);
					RefreshSpecificNode();
				}

				else if (sender.IDstring == "Load")
				{
					if (Data.PathFromName(saveName.actualValue, out _))
					{
						RoomSettings.BackgroundData().FromName(saveName.actualValue);
						SwitchRoomBackground(owner.room, RoomSettings.BackgroundData().type, true);
						RefreshSpecificNode();
					}
				}
			}

			else if (type == GenericSlider.SliderUpdate)
			{
				if (sender.IDstring == "XOffset" && sender is GenericSlider slider)
				{
					RoomSettings.BackgroundData().roomOffset.x = slider.actualValue;
				}

				else if (sender.IDstring == "YOffset" && sender is GenericSlider slider2)
				{
					RoomSettings.BackgroundData().roomOffset.y = slider2.actualValue;
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

		public void RefreshSpecificNode()
		{
			if (SpecificSettingsNode != null)
			{
				foreach (DevUINode node in SpecificSettingsNode.subNodes.ToList())
				{
					SpecificSettingsNode.subNodes.Remove(node);
					node.ClearSprites();
				}

				subNodes.Remove(SpecificSettingsNode);
				SpecificSettingsNode = null;
			}
			Data.BackgroundTemplateType type = RoomSettings.BackgroundData().type;
			backgroundType.Type = type;
			backgroundType.Refresh();
			RefreshElementNode();

			if (type == Data.BackgroundTemplateType.AboveCloudsView)
			{
				Debug.Log("atcUI");
				SpecificSettingsNode = new AboveCloudsUINode(owner, "atcUI", this, new Vector2(120f, 540f));
			}

			if (SpecificSettingsNode != null)
			{ subNodes.Add(SpecificSettingsNode); }
		}

		public void RefreshElementNode()
		{
			elementData.ClearSprites();
			subNodes.Remove(elementData);

			elementData = new BackgroundElementUINode(owner, "elementData", this, new Vector2(120f, 340f));
			subNodes.Add(elementData);
		}

		public BackgroundElementUINode elementData;

		public PanelSelectButton backgroundSave;

		public ExtEnumCycler<Data.BackgroundTemplateType> backgroundType;

		public StringControl saveName;

		public DevUINode? SpecificSettingsNode;

		public class AboveCloudsUINode : PositionedDevUINode, IDevUISignals
		{
			public AboveCloudsUINode(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
			{
				if (RoomSettings.BackgroundData().backgroundData is not Data.CloudsBackgroundData data)
				{ return; }

				FindAboveClouds();

				Vector2 ppos = Vector2.zero;

				subNodes.Add(new GenericSlider(owner, "startAltitude", this, ppos, "startAltitude", true, 80f, 2000f, 0f, 5000f, data.startAltitude / 10f ?? aboveCloudsView?.startAltitude / 10f ?? 2000f));
				ppos.y -= 20f;
				subNodes.Add(new GenericSlider(owner, "endAltitude", this, ppos, "endAltitude", true, 80f, 3140f, 0f, 5000f, data.endAltitude / 10f ?? aboveCloudsView?.endAltitude / 10f ?? 3140f));
				ppos.y -= 20f;
				subNodes.Add(new DevUILabel(owner, "cloudLabel", this, ppos, 110f, "Cloud Settings"));
				ppos.y -= 20f;
				subNodes.Add(new GenericSlider(owner, "cloudsStartDepth", this, ppos, "StartDepth", true, 80f, 5f, 0f, 200f, data.cloudsStartDepth ?? aboveCloudsView?.cloudsStartDepth ?? 5f));
				ppos.y -= 20f;
				subNodes.Add(new GenericSlider(owner, "cloudsEndDepth", this, ppos, "EndDepth", true, 80f, 40f, 0f, 200f, data.cloudsEndDepth ?? aboveCloudsView?.cloudsEndDepth ?? 40f));
				ppos.y -= 20f;
				subNodes.Add(new GenericSlider(owner, "distantCloudsEndDepth", this, ppos, "DistantEndDepth", true, 80f, 200f, 0f, 1000f, data.distantCloudsEndDepth ?? aboveCloudsView?.distantCloudsEndDepth ?? 200f));
				ppos.y -= 20f;
				subNodes.Add(new GenericSlider(owner, "closeCloudsCount", this, ppos, "CloseCount", true, 80f, 7f, 0f, 20f, data.cloudsCount ?? 7f));
				ppos.y -= 20f;
				subNodes.Add(new GenericSlider(owner, "distantCloudsCount", this, ppos, "DistantCount", true, 80f, 11f, 0f, 50f, data.distantCloudsCount ?? 11f));

			}

			AboveCloudsView? aboveCloudsView;

			public void FindAboveClouds()
			{
				foreach (UAD uad in owner.room.updateList)
				{
					if (uad is AboveCloudsView acv)
					{ aboveCloudsView = acv; }
				}
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (RoomSettings.BackgroundData().backgroundData is not Data.CloudsBackgroundData data)
				{ return; }

				if (type == GenericSlider.SliderUpdate)
				{
					if (sender is GenericSlider slider)
					{
						switch (sender.IDstring)
						{
						case "startAltitude":
							data.startAltitude = slider.actualValue * 10f;
							break;

						case "endAltitude":
							data.endAltitude = slider.actualValue * 10f;
							break;

						case "cloudsStartDepth":
							data.cloudsStartDepth = slider.actualValue;
							if (aboveCloudsView != null) BackgroundUpdates.CloudDepthAdjust(aboveCloudsView);
							break;

						case "cloudsEndDepth":
							data.cloudsEndDepth = slider.actualValue;
							if (aboveCloudsView != null) BackgroundUpdates.CloudDepthAdjust(aboveCloudsView);
							break;

						case "distantCloudsEndDepth":
							data.distantCloudsEndDepth = slider.actualValue;
							if (aboveCloudsView != null) BackgroundUpdates.CloudDepthAdjust(aboveCloudsView);
							break;

						case "closeCloudsCount":
							data.cloudsCount = slider.actualValue;
							if (aboveCloudsView != null) BackgroundUpdates.CloudAmountAdjust(aboveCloudsView);
							break;

						case "distantCloudsCount":
							data.distantCloudsCount = slider.actualValue;
							if (aboveCloudsView != null) BackgroundUpdates.CloudAmountAdjust(aboveCloudsView);
							break;
						}
					}
				}

				RoomSettings.BackgroundData().backgroundData = data;
			}
		}

		public class RoofTopUINode : DevUINode, IDevUISignals
		{
			public RoofTopUINode(DevUI owner, string IDstring, DevUINode parentNode) : base(owner, IDstring, parentNode)
			{
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
			}
		}

		public class BackgroundElementUINode : PositionedDevUINode, IDevUISignals
		{
			public BackgroundElementUINode(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
			{
				FindBackgroundScene();

				Vector2 ppos = Vector2.zero;

				subNodes.Add(new Button(owner, "delete_element", this, ppos + new Vector2(80f, 0f), 60f, "Delete"));

				newElement = new PanelSelectButton(owner, "new_element", this, ppos, 60f, "New", ElementTypes().ToArray(), "Select Type", new Vector2(420f, -200f), new Vector2(250f, 150f), 220f, 1);
				subNodes.Add(newElement);

				ppos.y -= 20f;

				elementName = new DevUILabel(owner, "elementName", this, ppos, 200f, "");
				subNodes.Add(elementName);
				elementSelect = new PanelSelectButton(owner, "elementSelect", this, ppos + new Vector2(220f, 0f), 30f, "...", ElementStrings().Keys.ToArray(), "Select Element", new Vector2(420f, -200f), new Vector2(280f, 420f), 240f, 1);
				subNodes.Add(elementSelect);
				elementArgs = new Dictionary<DevUINode, object>();
				RefreshElementArgs();
			}

			public override void Update()
			{
				if (refreshArgs)
				{
					refreshArgs = false;
					RefreshElementArgs();
				}

				base.Update();
			}

			public bool refreshArgs = false;

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (type == DevUISignalType.ButtonClick)
				{
					if (sender.IDstring == elementSelect.IDstring)
					{
						elementSelect.values = ElementStrings().Keys.ToArray();
					}

					else if (elementSelect.IsSubButtonID(sender.IDstring, out string subButtonID))
					{
						elementName.Text = subButtonID;
						selectedElement = ElementStrings()[subButtonID];
						refreshArgs = true;
					}

					else if (sender.IDstring == "delete_element" && selectedElement != null)
					{
						selectedElement.Destroy();
						backgroundScene?.elements.Remove(selectedElement);
						selectedElement = null;
						refreshArgs = true;
					}

					else if (newElement.IsSubButtonID(sender.IDstring, out string subButtonID2))
					{
						string text = Init.DefaultElementString(subButtonID2);

						BackgroundScene.BackgroundSceneElement? bselement = Init.ElementFromString(backgroundScene!, text, out string tag);
						if (bselement != null)
						{
							RoomSettings.BackgroundData().backgroundData!.backgroundElementText.Add(text);
							backgroundScene!.AddElement(bselement);
							selectedElement = bselement;
							refreshArgs = true;
						}
					}

					else if (selectedElement != null && sender is ElementAssetSelect elementAsset)
					{
						switch (selectedElement)
						{
						case AboveCloudsView.DistantBuilding t:
							t.assetName = elementAsset.label.Text;
							t.scene.LoadGraphic(t.assetName, true, false);
							t.CData().ReInitiateSprites = true;
							break;

						case AboveCloudsView.DistantLightning t:
							t.assetName = elementAsset.label.Text;
							t.scene.LoadGraphic(t.assetName, true, false);
							t.CData().ReInitiateSprites = true;
							break;
						}

						RoomSettings.BackgroundData().backgroundData!.backgroundElementText = Data.elementsToString(backgroundScene!);
					}
				}

				else if (selectedElement != null && type == StringControl.StringFinish && sender is StringControl stringControl)
				{
					switch (sender.IDstring)
					{
					case "depth":
						selectedElement.depth = float.Parse(stringControl.actualValue);
						selectedElement.CData().DepthUpdate = true;
						break;

					case "posx":
						selectedElement.pos.x = backgroundScene!.PosFromDrawPosAtNeutralCamPos(new Vector2(float.Parse(stringControl.actualValue), 0f), selectedElement.depth).x;
						break;

					case "posy":
						selectedElement.pos.y = backgroundScene!.PosFromDrawPosAtNeutralCamPos(new Vector2(0f, float.Parse(stringControl.actualValue)), selectedElement.depth).y;
						break;

					default:
						switch (selectedElement)
						{
						case AboveCloudsView.DistantBuilding t:
							if (sender.IDstring == "atmodepthadd")
							{ t.atmosphericalDepthAdd = float.Parse(stringControl.actualValue); }
							break;

						case AboveCloudsView.DistantLightning t:
							if (sender.IDstring == "minusDepth")
							{ t.minusDepthForLayering = float.Parse(stringControl.actualValue); }
							break;

						case AboveCloudsView.FlyingCloud:

							break;
						}
						break;
					}

					RoomSettings.BackgroundData().backgroundData!.backgroundElementText = Data.elementsToString(backgroundScene!);
				}
			}

			public void RefreshElementArgs()
			{
				foreach (DevUINode node in elementArgs.Keys)
				{
					subNodes.Remove(node);
					node.ClearSprites();
				}

				elementArgs = new Dictionary<DevUINode, object>();

				if (selectedElement == null) return;

				string[] args = Regex.Split(Regex.Split(selectedElement.ToDataString(), ": ")[1], ", ");

				Vector2 ppos = new Vector2(0f, -40f);
				foreach (KeyValuePair<string, object> kvp in Init.ArgTypes(args, selectedElement))
				{
					ppos.x = 160f;
					ppos.y -= 20f;
					DevUINode? node = null;
					switch (kvp.Value)
					{
					case float f:
						node = new StringControl(owner, kvp.Key, this, ppos, 80f, f.ToString(), StringControl.TextIsFloat);
						break;

					case int i:
						node = new StringControl(owner, kvp.Key, this, ppos, 80f, i.ToString(), StringControl.TextIsInt);
						break;

					case string t:
						if (kvp.Key == "name")
						{
							node = new ElementAssetSelect(owner, "Asset_Name", this, ppos);
							(node as ElementAssetSelect)!.label.Text = t;
						}

						else
							node = new StringControl(owner, kvp.Key, this, ppos, 80f, t, StringControl.TextIsAny);

						break;

					case bool b:
						node = new BoolButton(owner, kvp.Key, this, ppos, 80f, b);
						break;
					}

					if (node != null)
					{
						elementArgs.Add(node, kvp.Value);
						elementArgs.Add(new DevUILabel(owner, kvp.Key + "_Label", this, ppos - new Vector2(160f, 0f), 120f, kvp.Key), null!);
					}
				}

				foreach (DevUINode node in elementArgs.Keys)
				{ subNodes.Add(node); }
			}




			public void FindBackgroundScene()
			{
				foreach (UAD uad in owner.room.updateList)
				{
					if (uad is BackgroundScene bgs)
					{ backgroundScene = bgs; }
				}
			}

			public Dictionary<string, BackgroundScene.BackgroundSceneElement> ElementStrings()
			{
				Dictionary<string, BackgroundScene.BackgroundSceneElement> result = new();

				if (backgroundScene == null) return result;

				foreach (BackgroundScene.BackgroundSceneElement element in backgroundScene.elements)
				{
					string elementName = "";
					string depth = element.depth.ToString() + " - ";
					switch (element)
					{
					case AboveCloudsView.DistantBuilding el:
						elementName = "building - " + el.assetName;
						break;

					case AboveCloudsView.DistantLightning el:
						elementName = "Lightning - " + el.assetName;
						break;

					case AboveCloudsView.FlyingCloud el:
						elementName = "FlyingCloud - " + el.index;
						break;

					default:
						continue;
					}

					string elementString = depth + elementName;
					int index = 1;
					while (result.ContainsKey(elementString))
					{
						index++;
						elementString = depth + elementName + " - " + index.ToString();
					}

					result.Add(elementString, element);
				}
				return result;
			}


			public static List<string> ElementTypes()
			{
				List<string> result = new();
				result.Add("DistantBuilding");
				result.Add("DistantLightning");
				result.Add("FlyingCloud");
				return result;
			}

			BackgroundScene? backgroundScene;
			public PanelSelectButton newElement;
			public DevUILabel elementName;
			public PanelSelectButton elementSelect;

			public BackgroundScene.BackgroundSceneElement? selectedElement;

			Dictionary<DevUINode, object> elementArgs;
		}

		public static void SwitchRoomBackground(Room self, Data.BackgroundTemplateType type, bool refresh = false)
		{
			AboveCloudsView? aboveCloudsView = null;
			RoofTopView? roofTopView = null;
			VoidSea.VoidSeaScene? voidSeaView = null;
			foreach (UAD uad in self.updateList)
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
			if (aboveCloudsView != null && (type != Data.BackgroundTemplateType.AboveCloudsView || refresh))
			{
				Debug.Log("removing acv");
				aboveCloudsView.Destroy();
				self.RemoveObject(aboveCloudsView);
				aboveCloudsView = null;
			}

			if (roofTopView != null && (type != Data.BackgroundTemplateType.RoofTopView || refresh))
			{
				Debug.Log("removing rtv");
				roofTopView.Destroy();
				self.RemoveObject(roofTopView);
				roofTopView = null;
			}

			if (voidSeaView != null && (type != Data.BackgroundTemplateType.VoidSeaScene || refresh))
			{
				voidSeaView.Destroy();
				voidSeaView.RemoveFromRoom();
				voidSeaView = null;
			}

			if (aboveCloudsView == null && type == Data.BackgroundTemplateType.AboveCloudsView)
			{
				self.AddObject(new AboveCloudsView(self, new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.AboveCloudsView, 0f, false)));
			}

			if (roofTopView == null && type == Data.BackgroundTemplateType.RoofTopView)
			{
				self.AddObject(new RoofTopView(self, new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.RoofTopView, 0f, false)));
			}

			if (voidSeaView == null && type == Data.BackgroundTemplateType.VoidSeaScene)
			{
				//owner.room.AddObject(new VoidSea.VoidSeaScene(owner.room));
			}
		}

		public class BackgroundFileStringControl : StringControl
		{
			public BackgroundFileStringControl(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text, IsTextValid del) : base(owner, IDstring, parentNode, pos, width, text, del)
			{
			}

			protected override void TrySetValue(string newValue, bool endTransaction)
			{
				if (isTextValid(newValue))
				{
					actualValue = newValue;
					if (Data.PathFromName(actualValue, out _))
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
		}

		public class ElementAssetSelect : PositionedDevUINode, IDevUISignals
		{
			public ElementAssetSelect(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
			{
				Vector2 ppos = Vector2.zero;

				label = new DevUILabel(owner, "Asset_Label", this, ppos, 120f, "");
				subNodes.Add(label);
				ppos.x += 140f;

				select = new PanelSelectButton(owner, "Asset_Select", this, ppos, 30f, "...", ElementNames().ToArray(), "Select Element", new Vector2(420f, -200f), new Vector2(280f, 420f), 240f, 1);
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


		public static string[] backgroundSaves() => AssetManager.ListDirectory("RegionKit-Backgrounds").Select(i => Path.GetFileNameWithoutExtension(i)).ToArray();
	}

	public static bool checkForBackgroundPage(DevUI devUI)
	{
		if (devUI == null) return false;
		if (devUI.activePage == null) return false;
		if (devUI.activePage is not BackgroundPage) return false;
		return true;
	}
}

