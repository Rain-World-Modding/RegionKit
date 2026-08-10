using System.Text;
using System.Text.RegularExpressions;
using DevInterface;
using Pom;
using static RegionKit.Modules.IO._Enums;

#nullable disable

namespace RegionKit.Modules.IO
{

	public class IOType
	{
		public class IODataHolder
		{
			public bool InputType;

			public string MessageID;

			public string IOType;

			public float Delay;

			public IODataHolder(bool input, string messageID, string IOType, float delay)
			{
				InputType = input;
				MessageID = messageID;
				this.IOType = IOType;
				Delay = delay;
			}

			public override string ToString()
			{
				return $"[{InputType}, {MessageID}, {IOType}, {Delay}]";
			}
		}
		public class IOData : ManagedData
		{
			public List<(string Message, bool isInput)> MyMessages;

			public List<IODataHolder> IOHolder;
			new protected int FieldsWhenSerialized => fields.Length + (NeedsControlPanel ? 2 : 0) + IOHolder.Count;

			public IOData(PlacedObject owner, ManagedField[] customFields) : base(owner, customFields)
			{
				NeedsControlPanel = true;
				IOHolder = new List<IODataHolder>();
				MyMessages = new List<(string, bool)>();
			}

			public void AddData(IODataHolder data)
			{
				IOHolder.Add(data);
				MyMessages.Add((data.MessageID, data.InputType));
			}

			public void AddData(IODataHolder data, int index)
			{
				IOHolder.Insert(index, data);
				MyMessages.Insert(index, (data.MessageID, data.InputType));
			}

			public void RemoveData(int index)
			{
				IOHolder.RemoveAt(index);
				MyMessages.RemoveAt(index);
			}

			public void UpdateMessages(IODataHolder data, string LastMessage)
			{
				if (data.MessageID != LastMessage)
				{
					if (MyMessages.Contains((LastMessage, data.InputType)))
					{
						MyMessages.Remove((LastMessage, data.InputType));
					}
				}

				if (!MyMessages.Contains((data.MessageID, data.InputType)))
				{
					MyMessages.Add((data.MessageID, data.InputType));
				}
			}

			public override string ToString()
			{
				StringBuilder data = new StringBuilder();

				for (int d = 0; d < IOHolder.Count; d++)
				{
					data.Append("~");
					data.Append("[" + IOHolder[d].InputType + "-" + IOHolder[d].MessageID + "-" + IOHolder[d].IOType + "-" + IOHolder[d].Delay + "]");
				}

				return base.ToString() + data.ToString();
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				if (!s.Contains("[")) return;

				string data = s.Remove(s.LastIndexOf("]"), s.Length - s.LastIndexOf("]")).Remove(0, s.IndexOf("["));
				string[] holders = Regex.Split(data, "~");

				for (int i = 0; i < holders.Length; i++)
				{
					if (holders[i].Contains("["))
					{
						holders[i] = holders[i].Remove(holders[i].IndexOf("["), 1);
					}
					if (holders[i].Contains("]"))
					{
						holders[i] = holders[i].Remove(holders[i].IndexOf("]"), 1);
					}

					string[] Items = Regex.Split(holders[i], "-");
					AddData(new IODataHolder(bool.Parse(Items[0].ToString()), Items[1].ToString(), Items[2].ToString(), float.Parse(Items[3].ToString())));
				}
			}
		}

		public class IOPanel : Panel, IDevUISignals
		{
			public bool ButtonIsBeingClicked = false;

			public class IOButton : Button
			{
				public bool Enabled;

				public readonly bool Input;

				private InputType _inputType;

				private OutputType _outputType;

				public object Type
				{
					get
					{
						if (Input)
						{
							return _inputType;
						}

						return _outputType;
					}
				}

				public IOButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text, InputType inputType) : base(owner, IDstring, parentNode, pos, width, text)
				{
					_inputType = inputType;
					Input = true;
					Enabled = false;
				}

				public IOButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text, OutputType outputType) : base(owner, IDstring, parentNode, pos, width, text)
				{
					_outputType = outputType;
					Input = false;
					Enabled = false;
				}

				public IOButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text) : base(owner, IDstring, parentNode, pos, width, text)
				{
					Enabled = false;
				}

				public override void Update()
				{
					if (Enabled)
					{
						base.Update();
					}
				}

				public override void Refresh()
				{
					base.Refresh();

					for (int f = 0; f < fSprites.Count; f++)
					{
						fSprites[f].isVisible = Enabled;
					}

					for (int f = 0; f < fLabels.Count; f++)
					{
						fLabels[f].isVisible = Enabled;

						fLabels[f].alignment = FLabelAlignment.Center;

						if (fLabels[f].text == "+" || fLabels[f].text == "-")
						{
							fLabels[f].SetPosition(absPos.x + size.x / 2, absPos.y - 4f);
							fLabels[f].scale = 1.75f;
						}
						else
						{
							fLabels[f].SetPosition(absPos.x + size.x / 2, absPos.y);
							fLabels[f].scale = 1f;
						}
					}
				}

				public override void Clicked()
				{
					if (!(parentNode is InputOutputPanel))
					{
						base.Clicked();
					}
					else if (!(parentNode.parentNode as IOPanel).ButtonIsBeingClicked)
					{
						(parentNode.parentNode as IOPanel).ButtonIsBeingClicked = true;
						base.Clicked();
					}
				}
			}

			public class InputOutputPanel : RectangularDevUINode, IDevUISignals
			{
				public class IOMessageBox : StringControl
				{
					public bool Enabled;
					public IOMessageBox(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text) : base(owner, IDstring + "-Inputbox", parentNode, pos, width, text, IsValidMethod)
					{
						Enabled = false;
					}

					public static bool IsValidMethod(StringControl self, string value) => !value.Contains("-") || !value.Contains("[") || !value.Contains("]") || value.Length > 11;

					public override void Update()
					{
						if (Enabled)
						{
							base.Update();
						}
					}

					public override void Refresh()
					{
						base.Refresh();

						for (int f = 0; f < fSprites.Count; f++)
						{
							fSprites[f].isVisible = Enabled;
						}

						for (int f = 0; f < fLabels.Count; f++)
						{
							fLabels[f].isVisible = Enabled;
						}
					}
				}
				public class DelaySlider : Slider
				{
					public bool Enabled;
					public float delay;
					public DelaySlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, bool inheritButton, float titleWidth) : base(owner, IDstring, parentNode, pos, title, inheritButton, titleWidth)
					{
						Enabled = false;
					}

					public override void Update()
					{
						if (Enabled)
						{
							base.Update();
						}
					}
					public override void Refresh()
					{
						base.Refresh();

						for (int f = 0; f < fSprites.Count; f++)
						{
							fSprites[f].isVisible = Enabled;
						}

						for (int f = 0; f < fLabels.Count; f++)
						{
							fLabels[f].isVisible = Enabled;
						}

						for (int i = 0; i < subNodes.Count; i++)
						{
							for (int f = 0; f < subNodes[i].fSprites.Count; f++)
							{
								subNodes[i].fSprites[f].isVisible = Enabled;
							}

							for (int f = 0; f < subNodes[i].fLabels.Count; f++)
							{
								subNodes[i].fLabels[f].isVisible = Enabled;
							}
						}

						float nubPos = 0f;

						nubPos = delay;
						NumberText = (Mathf.Round(delay * 100) / 10).ToString();

						RefreshNubPos(nubPos);
					}

					public override void NubDragged(float nubPos)
					{
						RefreshNubPos(nubPos);

						delay = nubPos;

						parentNode.parentNode.Refresh();
						Refresh();
					}
				}

				public class IOTypeSelectPanel : Panel, IDevUISignals
				{
					public bool Enabled;
					public FSprite line;

					public List<IOButton> Buttons;

					public IOTypeSelectPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, string title) : base(owner, IDstring, parentNode, pos, size, title)
					{
						size.y = 5;

						Enabled = false;

						Buttons = new List<IOButton>();

						fSprites.Add(line = new FSprite("pixel"));
						line.anchorY = 0f;
						if (owner != null)
						{
							Futile.stage.AddChild(line);
						}

						if ((parentNode as InputOutputPanel).input)
						{
							BuildInputPanel(((parentNode as InputOutputPanel).parentNode as IOPanel).placedObject.type);
						}
						else
						{
							BuildOutputPanel(((parentNode as InputOutputPanel).parentNode as IOPanel).placedObject.type);
						}

						for (int i = 0; i < Buttons.Count; i++)
						{
							subNodes.Add(Buttons[i]);
						}
					}

					public void BuildInputPanel(PlacedObject.Type pObj)
					{
						if (pObj == PlacedObjectType.IOLightSource)
						{
							CreateInputButton(InputType.Input_Toggle);
							CreateInputButton(InputType.Input_On);
							CreateInputButton(InputType.Input_Off);
						}

						if (pObj == PlacedObjectType.Counter)
						{
							CreateInputButton(InputType.Input_AddValue);
							CreateInputButton(InputType.Input_RemoveValue);
							CreateInputButton(InputType.Input_ResetValue);
						}

						if (pObj == PlacedObjectType.CycleCooldown)
						{
							CreateInputButton(InputType.Input_Trigger);
						}
					}

					public void BuildOutputPanel(PlacedObject.Type pObj)
					{
						if (pObj == PlacedObjectType.Trigger)
						{
							CreateOutputButton(OutputType.Output_Trigger);
						}

						if (pObj == PlacedObjectType.IOLightSource)
						{
							CreateOutputButton(OutputType.Output_Toggle);
							CreateOutputButton(OutputType.Output_On);
							CreateOutputButton(OutputType.Output_Off);
						}

						if (pObj == _Enums.PlacedObjectType.Counter)
						{
							CreateOutputButton(OutputType.Output_Trigger);
						}

						if (pObj == _Enums.PlacedObjectType.CycleCooldown)
						{
							CreateOutputButton(OutputType.Output_Trigger);
						}
					}

					public void CreateInputButton(InputType type)
					{
						size.y += 20;
						Buttons.Add(new IOButton(this.owner, type.ToString(), this, new Vector2(5f, size.y - 20f), size.x - 10f, NameForInputType(type, ((parentNode as InputOutputPanel).parentNode as IOPanel).placedObject.type), type));
					}

					public void CreateOutputButton(OutputType type)
					{
						size.y += 20;
						Buttons.Add(new IOButton(this.owner, type.ToString(), this, new Vector2(5f, size.y - 20f), size.x - 10f, NameForOutputType(type, ((parentNode as InputOutputPanel).parentNode as IOPanel).placedObject.type), type));
					}

					public void Signal(DevUISignalType type, DevUINode sender, string message)
					{
						if (sender is IOButton b)
						{
							if (b.Type is InputType)
							{
								(parentNode as InputOutputPanel).SetType(b.Type as InputType);
								Enabled = false;
							}
							else
							{
								(parentNode as InputOutputPanel).SetType(b.Type as OutputType);
								Enabled = false;
							}
						}

					}

					public override void Update()
					{
						if (Enabled)
						{
							base.Update();
						}

						foreach (IOButton b in Buttons)
						{
							b.Enabled = Enabled;
						}
					}
					public override void Refresh()
					{
						base.Refresh();


						Vector2 nodepos = new Vector2((parentNode as InputOutputPanel).size.x + 18f, (parentNode as InputOutputPanel).TypeAdder.pos.y + 6f);

						MoveSprite(fSprites.IndexOf(line), (parentNode as InputOutputPanel).absPos + nodepos);
						line.scaleY = (pos - nodepos).magnitude;
						line.scaleX = 1f;
						line.rotation = Custom.VecToDeg(pos - nodepos);

						for (int f = 0; f < fSprites.Count; f++)
						{
							fSprites[f].isVisible = Enabled;
						}

						for (int f = 0; f < fLabels.Count; f++)
						{
							fLabels[f].isVisible = Enabled;
						}

						for (int i = 0; i < subNodes.Count; i++)
						{
							for (int f = 0; f < subNodes[i].fSprites.Count; f++)
							{
								subNodes[i].fSprites[f].isVisible = Enabled;
							}

							for (int f = 0; f < subNodes[i].fLabels.Count; f++)
							{
								subNodes[i].fLabels[f].isVisible = Enabled;
							}
						}
					}
				}

				public bool Enabled;

				public IOMessageBox messageID;
				public IOTypeSelectPanel IOType;
				public bool input;

				public IOButton DeleteButton;
				public IOButton TypeAdder;
				public FLabel label;

				public IODataHolder Data;
				public DelaySlider Delay;

				public FSprite LineNode;

				public InputOutputPanel(bool isInput, DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size)
					: this(owner, IDstring, parentNode, pos, size, new IODataHolder(isInput, "Message" + ((parentNode as IOPanel).IO.Count + 1), "", 0f))
				{

				}

				public InputOutputPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, IODataHolder data) : base(owner, IDstring, parentNode, pos, size)
				{
					if (((parentNode as IOPanel).placedObject.data as IOData).IOHolder.Contains(data))
					{
						Data = ((parentNode as IOPanel).placedObject.data as IOData).IOHolder[((parentNode as IOPanel).placedObject.data as IOData).IOHolder.IndexOf(data)];
					}
					else
					{
						Data = data;
						((parentNode as IOPanel).placedObject.data as IOData).AddData(Data);
					}

					Enabled = false;
					parentNode.subNodes.Add(this);
					this.input = Data.InputType;

					subNodes.Add(DeleteButton = new IOButton(this.owner, IDstring + "-Delete", this, new Vector2(size.x - 1f, 65f), 16f, "-"));
					fLabels.Add(label = new FLabel(Custom.GetFont(), input ? "Input" : "Output"));
					fLabels[0].color = Color.red;
					fLabels[0].alignment = FLabelAlignment.Center;

					fLabels.Add(label = new FLabel(Custom.GetFont(), "MessageID:"));
					fLabels[1].color = Color.black;

					fSprites.Add(new FSprite("pixel"));
					fSprites[0].color = new Color(1f, 1f, 1f);
					fSprites[0].alpha = 0.5f;

					fSprites.Add(new FSprite("pixel"));
					fSprites[1].color = new Color(1f, 1f, 1f);
					fSprites[1].alpha = 0.5f;

					fSprites.Add(new FSprite("pixel"));
					fSprites[2].color = new Color(0.5f, 0.5f, 0.5f);

					fSprites.Add(LineNode = new FSprite("SemiCircle16"));
					fSprites[3].color = new Color(1f, 1f, 1f);
					fSprites[3].scale = 0.75f;

					for (int i = 0; i < fSprites.Count; i++)
					{
						fSprites[i].anchorX = 0f;
						fSprites[i].anchorY = 0f;
						if (owner != null)
						{
							Futile.stage.AddChild(fSprites[i]);
						}
					}

					for (int i = 0; i < fLabels.Count; i++)
					{
						fLabels[i].anchorX = 0f;
						fLabels[i].anchorY = 0f;
						if (owner != null)
						{
							Futile.stage.AddChild(fLabels[i]);
						}
					}

					subNodes.Add(messageID = new IOMessageBox(this.owner, IDstring + "-ID", this, new Vector2(size.x - size.x / 4f * 2f + 15f, 45f), size.x / 4f * 2f, Data.MessageID));
					subNodes.Add(IOType = new IOTypeSelectPanel(this.owner, IDstring + "-SELECT", this, new Vector2(size.x, 0f), new Vector2(110f, 100f), "TYPE"));
					subNodes.Add(TypeAdder = new IOButton(this.owner, IDstring + "-TypeA", this, new Vector2(0f, 5f), size.x + 16f, "Set " + (input ? "Input" : "Output") + " Type:"));
					if (input)
					{
						subNodes.Add(Delay = new DelaySlider(this.owner, IDstring + "-Delay", this, new Vector2(0, 25f), "Delay:", false, size.x / 3.5f));
					}
				}

				public void SetType(_Enums.InputType inputType)
				{
					((parentNode as IOPanel).placedObject.data as IOData).IOHolder[(parentNode as IOPanel).IO.IndexOf(this)].IOType = inputType.ToString();
				}

				public void SetType(_Enums.OutputType outputType)
				{
					((parentNode as IOPanel).placedObject.data as IOData).IOHolder[(parentNode as IOPanel).IO.IndexOf(this)].IOType = outputType.ToString();
				}

				public override void Update()
				{
					base.Update();

					DeleteButton.Enabled = Enabled;
					messageID.Enabled = Enabled;
					TypeAdder.Enabled = Enabled;

					if (!Enabled)
					{
						IOType.Enabled = false;
					}

					if (input)
					{
						Delay.Enabled = Enabled;
					}
				}

				public override void Refresh()
				{
					base.Refresh();

					TypeAdder.Text = "Set " + (input ? "Input" : "Output") + " Type: " + ((parentNode as IOPanel).placedObject.data as IOData).IOHolder[(parentNode as IOPanel).IO.IndexOf(this)].IOType;

					if (fSprites.Count == 0 && fLabels.Count == 0)
					{
						return;
					}

					fSprites[0].scaleX = size.x - 5f;
					fSprites[0].scaleY = size.y;

					fSprites[1].scaleX = size.x - size.x / 4f * 2f + 10f;
					fSprites[1].scaleY = size.y;

					fSprites[2].scaleX = size.x + 25f;
					fSprites[2].scaleY = 1f;

					MoveSprite(0, absPos + new Vector2(0f, 65f));
					MoveSprite(1, absPos + new Vector2(0f, 45f));
					MoveSprite(2, absPos - new Vector2(5f, 0f));
					MoveSprite(3, absPos + new Vector2(size.x + 15f, TypeAdder.pos.y + 2f));

					MoveLabel(0, absPos + new Vector2(0f, 65f));
					MoveLabel(1, absPos + new Vector2(0f, 45f));


					for (int f = 0; f < fSprites.Count; f++)
					{
						fSprites[f].isVisible = Enabled;
					}

					fSprites[2].isVisible = (parentNode as IOPanel).IO.Count > 1 ? Enabled : false;

					for (int f = 0; f < fLabels.Count; f++)
					{
						fLabels[f].isVisible = Enabled;
					}

					string lastMessage = ((parentNode as IOPanel).placedObject.data as IOData).IOHolder[(parentNode as IOPanel).IO.IndexOf(this)].MessageID;
					((parentNode as IOPanel).placedObject.data as IOData).IOHolder[(parentNode as IOPanel).IO.IndexOf(this)].MessageID = messageID.Text;
					((parentNode as IOPanel).placedObject.data as IOData).UpdateMessages(Data, lastMessage);

					if (input)
					{
						((parentNode as IOPanel).placedObject.data as IOData).IOHolder[(parentNode as IOPanel).IO.IndexOf(this)].Delay = Delay.delay;
					}
				}

				public void Signal(DevUISignalType type, DevUINode sender, string message)
				{
					if (Enabled && sender.IDstring == IDstring + "-Delete")
					{
						Delete();
					}

					if (Enabled && sender.IDstring == IDstring + "-TypeA")
					{
						IOType.Enabled = !IOType.Enabled;
					}
				}

				public void Delete()
				{
					parentNode.subNodes.Remove(this);

					((parentNode as IOPanel).placedObject.data as IOData).RemoveData((parentNode as IOPanel).IO.IndexOf(this));
					(parentNode as IOPanel).IO.RemoveAt((parentNode as IOPanel).IO.IndexOf(this));

					ClearSprites();
					(parentNode as IOPanel).UpdateIO();
				}
			}

			public bool Enabled;
			public FSprite line;
			public bool IsInput;

			public PlacedObject placedObject;
			private PlacedObjectRepresentation representation;

			public IOButton AddItem;
			public IOButton ItemType;
			public HorizontalDivider Divide;

			public List<InputOutputPanel> IO = new List<InputOutputPanel>();
			public int lastIOCount;

			public static float baseSize = 25f;
			public static float sizeStep = 85f;

			public IOPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float sizeX, string name, PlacedObject pObj, PlacedObjectRepresentation rep) : base(owner, IDstring, parentNode, pos, new Vector2(sizeX, baseSize), name)
			{
				placedObject = pObj;
				representation = rep;

				Enabled = false;

				fSprites.Add(line = new FSprite("pixel"));
				line.anchorY = 0f;
				if (owner != null)
				{
					Futile.stage.AddChild(line);
				}

				subNodes.Add(AddItem = new IOButton(this.owner, "AddItem", this, new Vector2(5f, size.y - 20f), 85f, "+"));
				subNodes.Add(ItemType = new IOButton(this.owner, "Type", this, new Vector2(95f, size.y - 20f), 85f, "Input"));
				subNodes.Add(Divide = new HorizontalDivider(this.owner, "Divide", this, 10f));

				BuildIOPanels();
			}

			public void BuildIOPanels()
			{
				foreach (IODataHolder data in (placedObject.data as IOData).IOHolder)
				{
					IO.Add(new InputOutputPanel(this.owner, "IO" + (IO.Count + 1), this, new Vector2(5f, 5f + (sizeStep * IO.Count)), new Vector2(size.x - 25f, 16f), data));
				}

				UpdateIO();
			}

			public override void Update()
			{
				if (Enabled)
				{
					base.Update();
				}
				else
				{
					for (int num = subNodes.Count - 1; num >= 0; num--)
					{
						subNodes[num].Update();
					}
				}

				size = new Vector2(185f, (sizeStep * (IO.Count + 1)) - (IO.Count > 0 ? 55f : 60f));
				Divide.pos = new Vector2(Divide.pos.x, size.y - 25f);

				AddItem.pos = new Vector2(5f, size.y - 20f);
				ItemType.pos = new Vector2(95f, size.y - 20f);

				if (ButtonIsBeingClicked)
				{
					bool flag = false;
					foreach (InputOutputPanel panel in IO)
					{
						if ((panel.subNodes[0] as IOButton).down)
						{
							flag = true;
							break;
						}
					}
					ButtonIsBeingClicked = flag;
				}
			}

			public void UpdateIO()
			{
				pos.y = pos.y + (sizeStep * (lastIOCount - IO.Count));

				for (int i = 0; i < IO.Count; i++)
				{
					IO[i].pos = new Vector2(5f, 5f + (sizeStep * i));
				}

				lastIOCount = IO.Count;

				Divide.fSprites[0].scaleX = size.x;
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (!Enabled) return;

				if (sender.IDstring == "AddItem")
				{
					IO.Add(new InputOutputPanel(IsInput, this.owner, "IO" + (IO.Count + 1), this, new Vector2(5f, 5f + (sizeStep * IO.Count)), new Vector2(size.x - 25f, 16f)));
					UpdateIO();
				}

				if (sender.IDstring == "Type")
				{
					IsInput = !IsInput;
				}
			}

			public override void Refresh()
			{
				base.Refresh();

				ItemType.Text = IsInput ? "Input" : "Output";

				Vector2 nodepos = new Vector2((parentNode as Panel).size.x + 1f, (representation as IORepresentation).IOButton.pos.y + 8f);

				MoveSprite(fSprites.IndexOf(line), (parentNode as Panel).absPos + nodepos);
				line.scaleY = (pos - nodepos).magnitude;
				line.scaleX = 1f;
				line.rotation = Custom.VecToDeg(pos - nodepos);

				for (int f = 0; f < fSprites.Count; f++)
				{
					fSprites[f].isVisible = Enabled;
				}

				for (int f = 0; f < fLabels.Count; f++)
				{
					fLabels[f].isVisible = Enabled;
				}

				AddItem.Enabled = Enabled;
				ItemType.Enabled = Enabled;

				Divide.fSprites[0].isVisible = Enabled && IO.Count != 0;

				foreach (InputOutputPanel panel in IO)
				{
					panel.Enabled = Enabled;
				}
			}
		}

		public class IORepresentation : ManagedRepresentation, IDevUISignals
		{
			public class Connection
			{
				public FSprite Sprite;

				public DevUI DevUI;

				public PlacedObjectRepresentation PointA;

				public PlacedObjectRepresentation PointB;

				public Connection(DevUI DevUI, PlacedObjectRepresentation PointA, PlacedObjectRepresentation PointB)
				{
					this.DevUI = DevUI;
					this.PointA = PointA;
					this.PointB = PointB;

					Sprite = new FSprite("pixel");
					Sprite.anchorX = 0;
					Sprite.scaleY = 2;
					Sprite.color = Color.red;
					Sprite.alpha = 2f / 3f;

					Futile.stage.AddChild(Sprite);
					if (Sprite._container.GetChildIndex(PointA.fSprites[0]) < Sprite._container.GetChildIndex(PointB.fSprites[0]))
					{
						Sprite.MoveBehindOtherNode(PointA.fSprites[0]);
					}
					else
					{
						Sprite.MoveBehindOtherNode(PointB.fSprites[0]);
					}
				}

				public void Refresh()
				{
					Sprite.SetPosition(PointA.pos);
					Sprite.rotation = Custom.AimFromOneVectorToAnother(PointA.pos, PointB.pos) - 90f;
					Sprite.scaleX = Custom.Dist(PointA.pos, PointB.pos);

					if (DevUI.activePage is ObjectsPage p)
					{
						Sprite.isVisible = p.CustomData().ShowConnections;
					}
				}

				public void ClearSprite()
				{
					Sprite.RemoveFromContainer();
					Sprite = null;
				}
			}

			public Dictionary<(PlacedObjectRepresentation PointA, PlacedObjectRepresentation PointB), Connection> Connections;

			public IOPanel IOPanel;

			public Button IOButton;

			public FSprite lineNode;

			public bool Enabled;
			public IORepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
			{
				Enabled = false;
				Connections = new Dictionary<(PlacedObjectRepresentation PointA, PlacedObjectRepresentation PointB), Connection>();

				subNodes.Add(IOButton = new Button(owner, "IOButton", this.panel, new Vector2(5, 5), panel.size.x - 5, "I/O"));
				subNodes.Add(IOPanel = new IOPanel(owner, "IOPanel", this.panel, new Vector2(panel.size.x + 10f, 5f), 175f, "I/O", pObj, this));

				subNodes[subNodes.IndexOf(IOButton)].fLabels[0].x = subNodes[subNodes.IndexOf(IOButton)].fSprites[0].width / 2;

				panel.fSprites.Add(lineNode = new FSprite("SemiCircle16"));
				lineNode.scale = 0.75f;
				if (owner != null)
				{
					Futile.stage.AddChild(lineNode);
				}
			}

			public virtual void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (sender.IDstring == IOButton.IDstring)
				{
					Enabled = !Enabled;
					IOPanel.Enabled = Enabled;
				}
			}

			public override void ClearSprites()
			{
				base.ClearSprites();

				for (int i = 0; i < Connections.Count; i++)
				{
					Connections.ElementAt(i).Value.ClearSprite();
				}
			}

			public override void Refresh()
			{
				base.Refresh();

				panel.MoveSprite(panel.fSprites.IndexOf(lineNode), panel.absPos + new Vector2(panel.size.x, IOButton.pos.y + 8f));

				foreach (DevUINode node in (parentNode as ObjectsPage).subNodes)
				{
					if (node is IORepresentation rep)
					{
						for (int o = 0; o < rep.IOPanel.IO.Count; o++)
						{
							if (rep.IOPanel.IO[o].input && (pObj.data as IOData).MyMessages.Contains((rep.IOPanel.IO[o].Data.MessageID, false)))
							{
								if (!Connections.ContainsKey((this, rep)))
								{
									Connections.Add((this, rep), new Connection(owner, this, rep));
								}
								else
								{
									Connections.TryGetValue((this, rep), out Connection c);
									c.Refresh();
								}

								break;
							}
							else if (Connections.ContainsKey((this, rep)))
							{
								Connections[(this, rep)].ClearSprite();
								Connections.Remove((this, rep));
								break;
							}
						}

						if (rep.IOPanel.IO.Count == 0)
						{
							if (Connections.ContainsKey((this, rep)))
							{
								Connections[(this, rep)].ClearSprite();
								Connections.Remove((this, rep));
							}
						}
					}
				}
			}
		}
	}
}
