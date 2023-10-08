using System.IO;
using DevInterface;

namespace RegionKit.Modules.GateCustomization;

internal static class GateDataRepresentations
{
	// Names shortend to fit within the EnumField
	public enum HeaterData
	{
		Nrml,
		Brokn,
		Hiddn
	}

	internal abstract class GateDataRepresentation : ManagedRepresentation // This is a mess but I wanted dividers between the fields and this works I guess
	{
		protected int[] dividers = null;
		public GateDataRepresentation(PlacedObject.Type type, ObjectsPage objPage, PlacedObject pObj) : base(type, objPage, pObj)
		{
		}

		protected override void MakeControls()
		{
			if (dividers == null)
			{
				base.MakeControls();
				return;
			}

			ManagedData data = (ManagedData)pObj.data;
			if (data.NeedsControlPanel)
			{
				ManagedControlPanel panel = new ManagedControlPanel(this.owner, "ManagedControlPanel", this, data.panelPos, Vector2.zero, pObj.type.ToString());

				this.panel = panel;
				this.subNodes.Add(panel);

				Vector2 uiSize = new Vector2(0f, 0f);
				Vector2 uiPos = new Vector2(3f, 3f);

				float largestDisplayname = 0f;

				for (int i = 0; i < data.fields.Length; i++) // up down
				{
					if (data.fields[i] is ManagedFieldWithPanel field && field.NeedsControlPanel)
					{
						largestDisplayname = Mathf.Max(largestDisplayname, field.SizeOfDisplayname());
					}
				}

				for (int i = data.fields.Length - 1; i >= 0; i--) // down up
				{
					if (data.fields[i] is ManagedFieldWithPanel field && field.NeedsControlPanel)
					{
						uiSize.x = Mathf.Max(uiSize.x, field.SizeOfPanelUiMinusName().x);
					}
				}
				panel.size = uiSize + new Vector2(3 + largestDisplayname, 1);

				for (int i = data.fields.Length - 1; i >= 0; i--) // down up
				{
					if (data.fields[i] is ManagedFieldWithPanel field && field.NeedsControlPanel)
					{
						PositionedDevUINode? node = field.MakeControlPanelNode(data, panel, largestDisplayname);
						if (node is null)
						{
							throw new InvalidDataException();

						}

						for (int j = 0; j < dividers.Length; j++)
						{
							if (dividers[j] == i)
							{
								panel.subNodes.Add(new Panel.HorizontalDivider(panel.owner, $"Divider_{dividers[j]}", panel, uiPos.y + 5));
								uiSize.y += 10;
								uiPos.y += 10;
							}
						}

						panel.managedNodes[field.key] = node;
						panel.managedFields[field.key] = field;
						panel.subNodes.Add(node);
						node.pos = uiPos;
						//uiSize.x = Mathf.Max(uiSize.x, field.SizeOfPanelUiMinusName().x);
						uiSize.y += field.SizeOfPanelUiMinusName().y;
						uiPos.y += field.SizeOfPanelUiMinusName().y;
					}
				}
				panel.size = uiSize + new Vector2(3 + largestDisplayname, 1);
			}

			for (int i = 0; i < data.fields.Length; i++)
			{
				ManagedField field = data.fields[i];
				DevUINode? node = field.MakeAditionalNodes(data, this);
				if (node != null)
				{
					this.subNodes.Add(node);
					this.managedNodes[field.key] = node;
				}
			}

		}
	}

	internal class CommonGateDataRepresentation : GateDataRepresentation
	{
		public CommonGateDataRepresentation(PlacedObject.Type type, ObjectsPage objPage, PlacedObject pObj) : base(type, objPage, pObj)
		{
			fSprites.Add(new FSprite("DoorPositions", true));
			doorPositionsSprite = fSprites.Count - 1;

			fSprites[doorPositionsSprite].alpha = 0.4f;

			owner.placedObjectsContainer.AddChild(fSprites[doorPositionsSprite]);

			fLabels.Add(new FLabel(Custom.GetFont(), ""));
			label = fLabels.Count - 1;
			owner.placedObjectsContainer.AddChild(fLabels[label]);
		}

		public override void Refresh()
		{
			base.Refresh();

			if (doorPositionsSprite > -1)
			{
				fSprites[doorPositionsSprite].x = owner.room.MiddleOfTile(pObj.pos).x - owner.room.game.cameras[0].pos.x - 10f;
				fSprites[doorPositionsSprite].y = owner.room.MiddleOfTile(pObj.pos).y - owner.room.game.cameras[0].pos.y;
			}

			if (label > -1)
			{
				IntVector2 tilePosition = owner.room.GetTilePosition(pObj.pos);

				fLabels[label].x = pObj.pos.x + 40 - owner.room.game.cameras[0].pos.x;
				fLabels[label].y = pObj.pos.y - owner.room.game.cameras[0].pos.y;

				fLabels[label].text = $"{tilePosition.x}, {tilePosition.y}";
			}

			if (owner.room.regionGate != null)
			{
				for (int i = 0; i < 2; i++)
				{
					owner.room.regionGate.karmaGlyphs[i].UpdateDefaultColor();
				}

				RegionGateCWT.GetData(owner.room.regionGate).commonGateData = pObj.data as ManagedData;
			}
		}

		protected override void MakeControls()
		{
			this.dividers = new int[] { 2, 4 };
			base.MakeControls();
		}

		private int doorPositionsSprite = -1;
		private int label = -1;
	}

	internal class WaterGateDataRepresentation : GateDataRepresentation
	{
		public WaterGateDataRepresentation(PlacedObject.Type type, ObjectsPage objPage, PlacedObject pObj) : base(type, objPage, pObj)
		{
			fSprites.Add(new FSprite("WaterTank", true));
			waterTankSprite = fSprites.Count - 1;

			fSprites[waterTankSprite].alpha = 0.4f;

			owner.placedObjectsContainer.AddChild(fSprites[waterTankSprite]);
		}

		public override void Refresh()
		{
			base.Refresh();

			if (waterTankSprite > -1)
			{
				fSprites[waterTankSprite].x = owner.room.MiddleOfTile(pObj.pos).x - owner.room.game.cameras[0].pos.x + 10f;
				fSprites[waterTankSprite].y = owner.room.MiddleOfTile(pObj.pos).y - owner.room.game.cameras[0].pos.y + 100f;

				ManagedData data = pObj.data as ManagedData;

				if (data.GetValue<bool>("water"))
				{
					fSprites[waterTankSprite].alpha = 0.4f;
				}
				else
				{
					fSprites[waterTankSprite].alpha = 0.0f;
				}
			}

			if (owner.room.regionGate != null)
			{
				RegionGateCWT.GetData(owner.room.regionGate).waterGateData = pObj.data as ManagedData;
			}
		}

		protected override void MakeControls()
		{
			this.dividers = new int[] { 1 };
			base.MakeControls();
		}

		private int waterTankSprite = -1;
	}

	internal class ElectricGateDataRepresentation : GateDataRepresentation
	{
		public ElectricGateDataRepresentation(PlacedObject.Type type, ObjectsPage objPage, PlacedObject pObj) : base(type, objPage, pObj)
		{
			fSprites.Add(new FSprite("LampNumbers", true));
			lampNumbersSprite = fSprites.Count - 1;

			fSprites[lampNumbersSprite].alpha = 0.4f;

			owner.placedObjectsContainer.AddChild(fSprites[lampNumbersSprite]);
		}

		public override void Refresh()
		{
			base.Refresh();

			ManagedData data = pObj.data as ManagedData;

			if (owner.room.regionGate != null)
			{
				ManagedData commonGateData = RegionGateCWT.GetData(owner.room.regionGate).commonGateData;
				if (commonGateData != null)
				{
					fSprites[lampNumbersSprite].x = owner.room.MiddleOfTile(commonGateData.GetPosition(owner.room)).x - owner.room.game.cameras[0].pos.x - 10f;
					fSprites[lampNumbersSprite].y = owner.room.MiddleOfTile(commonGateData.GetPosition(owner.room)).y - owner.room.game.cameras[0].pos.y;

					fSprites[lampNumbersSprite].alpha = 0.6f;
				}
				else
				{
					fSprites[lampNumbersSprite].alpha = 0.0f;
				}

				if (owner.room.regionGate is ElectricGate)
				{
					if (data.GetValue<bool>("lampColorOverride"))
					{
						for (int i = 0; i < 4; i++)
						{
							for (int j = 0; j < 2; j++)
							{
								(owner.room.regionGate as ElectricGate).lamps[i, j].color = Color.HSVToRGB(
									data.GetValue<float>("lampHue"),
									data.GetValue<float>("lampSaturation"),
									1f
									//data.GetValue<float>("lampBrightness")
									);
							}
						}
					}
					else
					{
						for (int i = 0; i < 4; i++)
						{
							for (int j = 0; j < 2; j++)
							{
								(owner.room.regionGate as ElectricGate).lamps[i, j].color = new Color(1f, (j == 0) ? 0.4f : 0.6f, 0f);
							}
						}
					}
				}

				RegionGateCWT.GetData(owner.room.regionGate).electricGateData = pObj.data as ManagedData;
			}
		}

		protected override void MakeControls()
		{
			this.dividers = new int[] { 0, 4, 7 };
			base.MakeControls();
		}

		private int lampNumbersSprite = -1;
	}
}
