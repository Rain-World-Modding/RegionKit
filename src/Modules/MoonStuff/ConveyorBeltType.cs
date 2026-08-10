using System.Text.RegularExpressions;
using DevInterface;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class ConveyorBeltType : ManagedObjectType
	{
		public ConveyorBeltType() : base("Conveyor Belt", Objects._Module.GAMEPLAY_POM_CATEGORY, null, typeof(ConveyorBeltData), typeof(ConveyorBeltRepresentation))
		{
		}

		public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
		{
			return new ConveyorBelt(placedObject, room);
		}

		public class ConveyorBeltData : ManagedData
		{
#pragma warning disable 0649
			[FloatField("speed", 0f, 1, 0.5f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "Speed:")]
			public float speed;

			[BooleanField("reversed", false, ManagedFieldWithPanel.ControlType.button, "Reversed:")]
			public bool reversed;
#pragma warning restore 0649

			public IntVector2 Size;
			public int Covers;

			public ConveyorBeltData(PlacedObject owner) : base(owner, null)
			{
				Size = new IntVector2(6, 3);
				Covers = 0;
			}

			public override string ToString() => base.ToString() + "~" + Covers + "~" + Size.x + "~" + Size.y;

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] arr = Regex.Split(s, "~");
				try
				{
					Covers = int.Parse(arr[base.FieldsWhenSerialized + 0]);
					Size.x = int.Parse(arr[base.FieldsWhenSerialized + 1]);
					Size.y = int.Parse(arr[base.FieldsWhenSerialized + 2]);
				}
				catch { }
			}
		}

		public class ConveyorBeltRepresentation : ManagedRepresentation
		{
			public class ConveyorHandle : Handle
			{
				public FSprite left;
				public FSprite right;
				public FSprite top;
				public FSprite bottom;
				public FSprite box;
				public ConveyorHandle(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
				{
					fSprites.Add(left = new FSprite("pixel"));
					owner.placedObjectsContainer.AddChild(left);

					fSprites.Add(right = new FSprite("pixel"));
					owner.placedObjectsContainer.AddChild(right);

					fSprites.Add(top = new FSprite("pixel"));
					owner.placedObjectsContainer.AddChild(top);

					fSprites.Add(bottom = new FSprite("pixel"));
					owner.placedObjectsContainer.AddChild(bottom);

					fSprites.Add(box = new FSprite("pixel"));
					owner.placedObjectsContainer.AddChild(box);
				}

				public override void Move(Vector2 newPos)
				{
					if (newPos.x < 120f)
					{
						return;
					}

					Vector2 vector = (parentNode as DevInterface.PositionedDevUINode).pos + owner.game.cameras[0].pos;
					Vector2 vector2 = newPos + vector;
					IntVector2 value = new IntVector2(Mathf.FloorToInt(vector2.x / 20f), Mathf.FloorToInt(vector2.y / 20f));
					IntVector2 intVector = new IntVector2(Mathf.FloorToInt(vector.x / 20f), Mathf.FloorToInt(vector.y / 20f));
					value -= intVector;
					newPos = value.ToVector2() * 20f;

					((parentNode as ConveyorBeltRepresentation).pObj.data as ConveyorBeltData).Size.x = value.x;
					base.Move(new Vector2(newPos.x + 1, 60f));
				}

				public override void Refresh()
				{
					base.Refresh();

					MoveSprite(fSprites.IndexOf(left), (parentNode as ConveyorBeltRepresentation).absPos);
					fSprites[fSprites.IndexOf(left)].anchorY = 0f;
					fSprites[fSprites.IndexOf(left)].scaleY = pos.y;

					MoveSprite(fSprites.IndexOf(right), absPos);
					fSprites[fSprites.IndexOf(right)].anchorY = 0f;
					fSprites[fSprites.IndexOf(right)].scaleY = -pos.y;

					MoveSprite(fSprites.IndexOf(top), new Vector2((parentNode as ConveyorBeltRepresentation).absPos.x, absPos.y));
					fSprites[fSprites.IndexOf(top)].anchorX = 0f;
					fSprites[fSprites.IndexOf(top)].scaleX = pos.x;

					MoveSprite(fSprites.IndexOf(bottom), (parentNode as ConveyorBeltRepresentation).absPos);
					fSprites[fSprites.IndexOf(bottom)].anchorX = 0f;
					fSprites[fSprites.IndexOf(bottom)].scaleX = pos.x;

					MoveSprite(fSprites.IndexOf(box), (parentNode as ConveyorBeltRepresentation).absPos);
					fSprites[fSprites.IndexOf(box)].anchorX = 0f;
					fSprites[fSprites.IndexOf(box)].anchorY = 0f;
					fSprites[fSprites.IndexOf(box)].scaleX = pos.x;
					fSprites[fSprites.IndexOf(box)].scaleY = pos.y;
					fSprites[fSprites.IndexOf(box)].alpha = 0.05f;
				}
			}

			public class CoverSlider : Slider
			{
				public PlacedObject pObj;
				public int maxCovers;
				public CoverSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, bool inheritButton, float titleWidth)
					: base(owner, IDstring, parentNode, pos, title, inheritButton, titleWidth)
				{
					pObj = (parentNode.parentNode as ConveyorBeltRepresentation).pObj;
				}

				public override void Refresh()
				{
					base.Refresh();
					int a = 3;
					int b = (pObj.data as ConveyorBeltData).Size.x - 3;
					int i;

					for (i = 1; i < (pObj.data as ConveyorBeltData).Size.x - 1; i++)
					{
						float a1 = ((b - a) * 2) / i;
						float a2 = ((b - a) * 1) / i;

						if ((int)((a1 - a2) + 0.5) < 3)
						{
							break;
						}
					}
					maxCovers = Math.Max((Mathf.FloorToInt(((i - 3) / 2f) - 0.5f) * 2) + 1, 0);

					float nubPos = 0f;

					if (IDstring == "CoverSlider")
					{
						nubPos = maxCovers < 1 ? 0f : (pObj.data as ConveyorBeltData).Covers / (float)maxCovers;
						NumberText = Math.Min((pObj.data as ConveyorBeltData).Covers, maxCovers).ToString();
					}

					RefreshNubPos(nubPos);
				}

				public override void NubDragged(float nubPos)
				{
					RefreshNubPos(nubPos);

					if (IDstring == "CoverSlider")
					{
						(pObj.data as ConveyorBeltData).Covers = Math.Max((Mathf.FloorToInt(((nubPos * maxCovers) / 2f) - 0.5f) * 2) + 1, 0);
					}

					parentNode.parentNode.Refresh();
					Refresh();
				}
			}

			public ConveyorHandle handle;
			public CoverSlider coverslider;
			public ConveyorBeltRepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
			{
				panel.size.y = 65f;
				panel.size.x += 5f;

				subNodes.Add(handle = new ConveyorHandle(this.owner, "SizeHandle", this, new Vector2((pObj.data as ConveyorBeltData).Size.x * 20f, 60f)));
				subNodes.Add(coverslider = new CoverSlider(this.owner, "CoverSlider", this.panel, new Vector2(5f, 5f), "Gears:", false, 65f));

				(panel.subNodes[0] as PositionedDevUINode).pos = new Vector2(5f, 45f);
				(panel.subNodes[1] as PositionedDevUINode).pos = new Vector2(5f, 25f);
			}

			public override void Move(Vector2 newPos)
			{
				base.Move(owner.room.MiddleOfTile(pObj.pos) - owner.room.game.cameras[0].pos - new Vector2(10f, 10f));
			}
		}
	}
}
