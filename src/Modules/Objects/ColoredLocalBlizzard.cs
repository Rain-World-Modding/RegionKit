using System.Globalization;
using System.Text.RegularExpressions;
using DevInterface;
using RegionKit.Extras.FutileExtras;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.Objects
{
	public class ColoredLocalBlizzard : UpdatableAndDeletable, IDrawable
	{
		public Vector2 pos;
		public float rad;
		public float intensity;
		public float scale;
		public float angle;
		public Color color;

		public ColoredLocalBlizzard(Vector2 initPos, float initRad, float initIntensity, float initScale, Color initColor)
		{
			pos = initPos;
			rad = initRad;
			intensity = initIntensity;
			scale = initScale;
			angle = 0f;
			color = initColor;
		}

		public ColoredLocalBlizzard(PlacedObject pObj, Data data) : this(pObj.pos, data.Rad, data.intensity, data.scale, data.color) { }

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[1];
			sLeaser.sprites[0] = new FSpriteUVs("Futile_White")
			{
				shader = room.game.rainWorld.Shaders["RKColoredLocalBlizzard"]
			};
			AddToContainer(sLeaser, rCam, null!);
		}

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[0]);
			sLeaser.sprites[0].MoveToBack();
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			Vector2 showPos = new Vector2(pos.x - camPos.x + 0.5f, pos.y - camPos.y + 0.5f);
			float spriteScale = rad / 8f;
			FSpriteUVs sprite = (sLeaser.sprites[0] as FSpriteUVs)!;
			sprite.x = showPos.x;
			sprite.y = showPos.y;
			sprite.scale = spriteScale;
			sprite.color = new Color(intensity, scale, 0f, 0f);
			sprite.rotation = angle * 360f;
			sprite.SetUVs(new Vector2(color.r, color.g), 1);
			sprite.SetUVs(new Vector2(color.b, 1f), 2);
			if (slatedForDeletetion || room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
			}
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
		}

		public class Data : PlacedObject.Data
		{
			public Vector2 handlePos;
			public Vector2 panelPos;
			public float intensity;
			public float scale;
			public float angle;
			public Color color;

			public float Rad
			{
				get
				{
					return handlePos.magnitude;
				}
			}

			public Data(PlacedObject owner) : base(owner)
			{
				handlePos = new Vector2(0f, 100f);
				panelPos = Custom.DegToVec(30f) * 100f;
				intensity = 1f;
				scale = 0.5f;
				angle = 0f;
				color = Color.white;
			}

			protected string BaseSaveString()
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}~{1}~{2}~{3}~{4}~{5}~{6}~{7}~{8}~{9}",
				[
				handlePos.x, handlePos.y,
				panelPos.x, panelPos.y,
				intensity,
				scale,
				angle,
				color.r, color.g, color.b
				]);
			}

			public override string ToString()
			{
				string text = BaseSaveString();
				text = SaveState.SetCustomData(this, text);
				return SaveUtils.AppendUnrecognizedStringAttrs(text, "~", unrecognizedAttributes);
			}

			public override void FromString(string s)
			{
				string[] array = Regex.Split(s, "~");
				handlePos.x = float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
				handlePos.y = float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
				panelPos.x = float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
				panelPos.y = float.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture);
				intensity = float.Parse(array[4], NumberStyles.Any, CultureInfo.InvariantCulture);
				scale = float.Parse(array[5], NumberStyles.Any, CultureInfo.InvariantCulture);
				angle = float.Parse(array[6], NumberStyles.Any, CultureInfo.InvariantCulture);
				color.r = float.Parse(array[7], NumberStyles.Any, CultureInfo.InvariantCulture);
				color.g = float.Parse(array[8], NumberStyles.Any, CultureInfo.InvariantCulture);
				color.b = float.Parse(array[9], NumberStyles.Any, CultureInfo.InvariantCulture);
				unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, 10);
			}
		}

		public class Representation : PlacedObjectRepresentation
		{
			private Handle radHandle;
			private Panel panel;

			private ColoredLocalBlizzard blizzs;

			private Data Data => (pObj.data as Data)!;

			public Representation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name)
				: base(owner, IDstring, parentNode, pObj, name)
			{
				subNodes.Add(radHandle = new Handle(owner, "Rad_Handle", this, new Vector2(0f, 100f)));
				radHandle.pos = Data.handlePos;
				fSprites.Add(new FSprite("Futile_White", true));
				owner.placedObjectsContainer.AddChild(fSprites[1]);
				fSprites[1].shader = owner.room.game.rainWorld.Shaders["VectorCircle"];
				fSprites.Add(new FSprite("pixel", true));
				owner.placedObjectsContainer.AddChild(fSprites[2]);
				fSprites[2].anchorY = 0f;
				fSprites.Add(new FSprite("pixel", true));
				owner.placedObjectsContainer.AddChild(fSprites[3]);
				fSprites[3].anchorY = 0f;
				subNodes.Add(panel = new Panel(owner, "ColoredLocalBlizzard_Control_Panel", this, new Vector2(0f, 100f)));
				panel.pos = Data.panelPos;
				for (int i = 0; i < owner.room.updateList.Count; i++)
				{
					if (owner.room.updateList[i] is ColoredLocalBlizzard blizz && blizz.pos == pObj.pos)
					{
						blizzs = blizz;
						break;
					}
				}
				if (blizzs == null)
				{
					blizzs = new ColoredLocalBlizzard(pos, 100f, 1f, 0.5f, Color.white);
					owner.room.AddObject(blizzs);
				}
			}

			public override void Refresh()
			{
				base.Refresh();
				base.MoveSprite(1, absPos);
				fSprites[1].scale = radHandle.pos.magnitude / 8f;
				fSprites[1].alpha = 2f / radHandle.pos.magnitude;
				base.MoveSprite(2, absPos);
				fSprites[2].scaleY = radHandle.pos.magnitude;
				fSprites[2].rotation = Custom.AimFromOneVectorToAnother(absPos, radHandle.absPos);
				Data.handlePos = radHandle.pos;
				base.MoveSprite(3, absPos);
				fSprites[3].scaleY = panel.pos.magnitude;
				fSprites[3].rotation = Custom.AimFromOneVectorToAnother(absPos, panel.absPos);
				Data.handlePos = radHandle.pos;
				Data.panelPos = panel.pos;
				blizzs.pos = pObj.pos;
				blizzs.rad = Data.Rad;
				blizzs.angle = Data.angle;
				blizzs.intensity = Data.intensity;
				blizzs.scale = Data.scale;
				blizzs.color = Data.color;
			}

			public class Panel : DevInterface.Panel
			{
				private Representation Rep => (parentNode as Representation)!;
				private Data Data => Rep.Data;

				private RGBSelectButton colorButton;
				private Color lastColor;

				public Panel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos)
					: base(owner, IDstring, parentNode, pos, new Vector2(250f, 85f), "Colored Local Blizzard FX")
				{
					subNodes.Add(new Slider(owner, "Angle_Slider", this, new Vector2(5f, 25f), "Angle: "));
					subNodes.Add(new Slider(owner, "Scale_Slider", this, new Vector2(5f, 45f), "Scale: "));
					subNodes.Add(new Slider(owner, "Intensity_Slider", this, new Vector2(5f, 65f), "Intensity: "));
					subNodes.Add(new DevUILabel(owner, "Color_Label", this, new Vector2(5f, 5f), 110f, "Color: "));
					subNodes.Add(colorButton = new RGBSelectButton(owner, "Color_Button", this, new Vector2(125f, 5f), 125f, "", Data.color, "Blizzard color"));
				}

				public override void Update()
				{
					base.Update();
					if (lastColor != colorButton.actualValue)
					{
						Data.color = colorButton.actualValue;

						lastColor = colorButton.actualValue;
						Rep.Refresh();
					}
				}

				public class Slider : DevInterface.Slider
				{
					private Data Data => (parentNode.parentNode as Representation)!.Data;

					public Slider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title)
						: base(owner, IDstring, parentNode, pos, title, false, 110f)
					{
					}

					public override void NubDragged(float nubPos)
					{
						string idstring = IDstring;
						if (idstring != null && idstring == "Intensity_Slider")
						{
							Data.intensity = nubPos;
						}
						if (idstring != null && idstring == "Scale_Slider")
						{
							Data.scale = nubPos;
						}
						if (idstring != null && idstring == "Angle_Slider")
						{
							Data.angle = nubPos;
						}
						if (idstring != null && idstring == "Alpha_Slider")
						{
							Data.color.a = nubPos;
						}
						parentNode.parentNode.Refresh();
						Refresh();
					}

					public override void Refresh()
					{
						base.Refresh();
						if (IDstring == "Intensity_Slider")
						{
							float intensity = Data.intensity;
							base.NumberText = ((int)(intensity * 100f)).ToString() + "% ";
							base.RefreshNubPos(intensity);
						}
						if (IDstring == "Scale_Slider")
						{
							float scale = Data.scale;
							base.NumberText = ((int)(scale * 100f)).ToString() + "% ";
							base.RefreshNubPos(scale);
						}
						if (IDstring == "Angle_Slider")
						{
							float angle = Data.angle;
							base.NumberText = string.Concat((int)(angle * 360f));
							base.RefreshNubPos(angle);
						}
						if (IDstring == "Alpha_Slider")
						{
							float angle = Data.color.a;
							base.NumberText = ((int)(angle * 100f)).ToString() + "% ";
							base.RefreshNubPos(angle);
						}
					}
				}
			}
		}
	}
}
