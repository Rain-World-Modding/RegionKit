using System.Text.RegularExpressions;
using DevInterface;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class SandfallType : ManagedObjectType
	{
		public SandfallType() : base(_Enums.SandFall.value, Objects._Module.DECORATIONS_POM_CATEGORY, null, typeof(SandFallData), typeof(SandFallRepresentation))
		{
		}

		public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
		{
			return new SandFallObject(placedObject, room, room.GetTilePosition(placedObject.pos), (placedObject.data as SandFallData).flow, (placedObject.data as SandFallData).width.x);
		}

		class SandFallObject : SandFall
		{
			public PlacedObject placedObject;

			public SandFallObject(PlacedObject placedObject, Room room, IntVector2 tilePos, float flow, int width) : base(room, tilePos, flow, width)
			{
				this.placedObject = placedObject;
			}

			public override void Update(bool eu)
			{
				base.Update(eu);

				base.tilePos = new IntVector2((int)placedObject.pos.x, (int)placedObject.pos.y);
				base.width = (placedObject.data as SandFallData).width.x;
				base.flow = (placedObject.data as SandFallData).flow;
			}
		}

		public class SandFallData : ManagedData
		{
			#pragma warning disable 0649
			[FloatField("preDelay", -3f, 3f, 0f, displayName: "(UNUSED) Pre Delay:")]
			public float preDelay;

			[FloatField("postDelay", -3f, 3f, 0f, displayName: "(UNUSED) Post Delay:")]
			public float postDelay;

			[FloatField("flow", 0f, 1f, 0.5f, 0.01f, displayName: "Flow:")]
			public float flow;

#pragma warning restore 0649

			public bool Mode;

			public IntVector2 width;

			public SandFallData(PlacedObject owner) : base(owner, null)
			{
				this.Mode = false;
				this.width = new IntVector2(20, 0);
			}

			public override string ToString()
			{
				return base.ToString() + "~" + Mode + "~" + width.x + "~" + width.y;
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] arr = Regex.Split(s, "~");
				try
				{
					Mode = bool.Parse(arr[base.FieldsWhenSerialized + 0]);
					width.x = int.Parse(arr[base.FieldsWhenSerialized + 1]);
					width.y = int.Parse(arr[base.FieldsWhenSerialized + 2]);
				}
				catch { }
			}
		}
		class SandFallRepresentation : ManagedRepresentation, IDevUISignals
		{
			public class SandFallHandle : Handle
			{
				public FSprite line;
				public SandFallHandle(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
				{
					fSprites.Add(line = new FSprite("pixel"));
					owner.placedObjectsContainer.AddChild(line);
					line.anchorY = 0f;
				}

				public override void Move(Vector2 newPos)
				{
					base.Move(new Vector2(owner.room.GetTilePosition(newPos).x * 20, 0f));
				}

				public override void SetColor(Color col)
				{
					base.SetColor(col);

					line.color = col;
				}

				public override void Refresh()
				{
					base.Refresh();

					MoveSprite(fSprites.IndexOf(line), absPos);
					line.scaleY = pos.magnitude;
					line.rotation = Custom.VecToDeg(-pos);
				}
			}

			public DevInterface.Button Mode;
			public SandFallHandle Handle;
			public float width;
			public SandFallRepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
			{
				this.width = 275f;
				//panel.size = new Vector2(285f, 85f);
				panel.size = new Vector2(285f, 25f);

				panel.subNodes.Add(Mode = new DevInterface.Button(this.owner, "Mode", this.panel, new Vector2(5, 25), width, "(UNUSED) Mode:"));
				subNodes.Add(Handle = new SandFallHandle(this.owner, "Handle", this, Custom.IntVector2ToVector2((pObj.data as SandFallData).width)));

				(panel.subNodes[0] as DevInterface.Slider).pos = new Vector2(5, 45f); //preDelay
				(panel.subNodes[1] as DevInterface.Slider).pos = new Vector2(5, 65f); //postDelay
				(panel.subNodes[2] as DevInterface.Slider).pos = new Vector2(5, 5f); //flow

				//((panel.subNodes[2] as DevInterface.Slider).subNodes[1] as DevUILabel).fSprites[0].width = 24f;
				//((panel.subNodes[2] as DevInterface.Slider).subNodes[1] as DevUILabel).pos = new Vector2(90, 5);
				//((panel.subNodes[2] as DevInterface.Slider).subNodes[0] as DevUILabel).pos = new Vector2(5, 0);
			}
			public override void Refresh()
			{
				base.Refresh();
				Mode.Text = "(UNUSED) Mode: " + ((pObj.data as SandFallData).Mode ? "Static" : "Dynamic");

				(panel.subNodes[panel.subNodes.IndexOf(Mode)] as DevInterface.Button).absPos = new Vector2(0, float.MinValue);
				(panel.subNodes[0] as DevInterface.Slider).absPos = new Vector2(0, float.MinValue);
				(panel.subNodes[1] as DevInterface.Slider).absPos = new Vector2(0, float.MinValue);
			}

			public override void Update()
			{
				base.Update();
				(pObj.data as SandFallData).width = new IntVector2((int)Handle.pos.x, (int)Handle.pos.y);
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				(pObj.data as SandFallData).Mode = !(pObj.data as SandFallData).Mode;
			}

			public override void Move(Vector2 newPos)
			{
				Vector2 pos = (owner.room.MiddleOfTile(newPos + new Vector2(-1f, 3f)) - owner.room.cameraPositions[owner.room.game.cameras[0].currentCameraPosition] - new Vector2(16f, 8f)) + owner.room.cameraPositions[owner.room.game.cameras[0].currentCameraPosition] + new Vector2(9f, 0f);
				base.Move(pos + new Vector2(0f, 10f));
			}
		}
	}
}
