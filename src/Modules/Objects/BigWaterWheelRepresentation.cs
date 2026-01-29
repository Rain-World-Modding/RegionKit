using DevInterface;

namespace RegionKit.Modules.Objects
{
	internal class BigWaterWheelRepresentation : PlacedObjectRepresentation
	{
		public BigWaterWheel.Data Data => (pObj.data as BigWaterWheel.Data)!;

		public BigWaterWheelRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
		{
			subNodes.Add(new BigWaterWheelPanel(owner, "BigWaterWheel_Panel", this, new Vector2(0f, 100f))
			{
				pos = Data.panelPos
			});
			fSprites.Add(new FSprite("pixel", true)
			{
				anchorY = 0f,
			});
			owner.placedObjectsContainer.AddChild(fSprites[^1]);
		}

		public override void Refresh()
		{
			base.Refresh();
			MoveSprite(1, absPos);
			MoveSprite(2, absPos);
			fSprites[1].scaleY = (subNodes[0] as BigWaterWheelPanel)!.pos.magnitude;
			fSprites[1].rotation = Custom.AimFromOneVectorToAnother(absPos, (subNodes[0] as BigWaterWheelPanel)!.absPos);
		}

		public class BigWaterWheelPanel : Panel
		{
			public BigWaterWheelRepresentation Rep => (parentNode as BigWaterWheelRepresentation)!;
			public BigWaterWheel.Data Data => (Rep.pObj.data as BigWaterWheel.Data)!;

			public BigWaterWheelPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 65f), "Big Water Wheel")
			{
				subNodes.Add(new WheelSlider(owner, "Rotat_Slider", this, new Vector2(5f, 45f), "Rotation: "));
				subNodes.Add(new WheelSlider(owner, "Speed_Slider", this, new Vector2(5f, 25f), "Speed: "));
				subNodes.Add(new WheelSlider(owner, "Depth_Slider", this, new Vector2(5f, 5f), "Depth: "));
			}

			public class WheelSlider : Slider
			{
				public BigWaterWheelPanel Panel => (parentNode as BigWaterWheelPanel)!;
				public BigWaterWheel.Data Data => Panel.Data;

				public WheelSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title, false, 110f)
				{
				}

				public override void Refresh()
				{
					base.Refresh();
					float num = 0f;
					switch (IDstring)
					{
					case "Speed_Slider":
						num = Data.speed;
						NumberText = ((int)Mathf.Lerp(-100f, 100f, num)).ToString();
						break;
					case "Depth_Slider":
						num = Data.depth;
						NumberText = ((int)Mathf.Lerp(0f, 30f, num)).ToString();
						break;
					case "Rotat_Slider":
						num = Data.rotation;
						NumberText = ((int)Mathf.Lerp(0f, 360f, num)).ToString();
						break;
					}
					RefreshNubPos(num);
				}

				public override void NubDragged(float nubPos)
				{
					switch (IDstring)
					{
					case "Speed_Slider":
						Data.speed = nubPos;
						break;
					case "Depth_Slider":
						Data.depth = nubPos;
						break;
					case "Rotat_Slider":
						Data.rotation = nubPos;
						break;
					}
					parentNode.parentNode.Refresh();
					Refresh();
				}
			}
		}
	}
}
