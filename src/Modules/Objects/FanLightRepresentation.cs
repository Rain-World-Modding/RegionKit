using DevInterface;

namespace RegionKit.Modules.Objects;

public class FanLightRepresentation : PlacedObjectRepresentation
{
	public class FanLightControlPanel : Panel, IDevUISignals
	{
		public class ControlSlider : Slider
		{
			FanLightData Data => ((parentNode.parentNode as FanLightRepresentation)!.pObj.data as FanLightData)!;

			public ControlSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title, false, 110f) { }

			public override void Refresh()
			{
				base.Refresh();
				int num;
				float num2;
				if (Data is not FanLightData d)
					return;
				switch (IDstring)
				{
					case "Seed_Slider":
						num = d.randomSeed;
						NumberText = num.ToString();
						RefreshNubPos(num / 100f);
						break;
					case "ColorR_Slider":
						num2 = d.colorR;
						NumberText = ((int)(255f * num2)).ToString();
						RefreshNubPos(num2);
						break;
					case "ColorG_Slider":
						num2 = d.colorG;
						NumberText = ((int)(255f * num2)).ToString();
						RefreshNubPos(num2);
						break;
					case "ColorB_Slider":
						num2 = d.colorB;
						NumberText = ((int)(255f * num2)).ToString();
						RefreshNubPos(num2);
						break;
					case "Speed_Slider":
						num = d.speed;
						NumberText = num.ToString();
						RefreshNubPos(num / 100f);
						break;
					case "Inverse_Speed_Slider":
						num = d.inverseSpeed;
						NumberText = num.ToString();
						RefreshNubPos(num / 100f);
						break;
				}
			}

			public override void NubDragged(float nubPos)
			{
				if (Data is not FanLightData d)
					return;
				switch (IDstring)
				{
					case "Seed_Slider":
						d.randomSeed = (int)(nubPos * 100f);
						break;
					case "ColorR_Slider":
						d.colorR = nubPos;
						break;
					case "ColorG_Slider":
						d.colorG = nubPos;
						break;
					case "ColorB_Slider":
						d.colorB = nubPos;
						break;
					case "Speed_Slider":
						d.speed = (int)(nubPos * 100f);
						break;
					case "Inverse_Speed_Slider":
						d.inverseSpeed = (int)(nubPos * 100f);
						break;
				}
				parentNode.parentNode.Refresh();
				Refresh();
			}
		}

		public class SelectSpritePanel : Panel
		{
            public SelectSpritePanel(DevUI owner, DevUINode parentNode, Vector2 pos, string[] decalNames) : base(owner, "Select_Sprite_Panel", parentNode, pos, new(175f, 225f), "Select sprite")
			{
				var intVector = new IntVector2(0, 0);
				for (var i = 0; i < decalNames.Length; i++)
				{
					subNodes.Add(new Button(owner, decalNames[i], this, new(5f, size.y - 20f - 20f * intVector.y), 165f, decalNames[i]));
					intVector.y++;
					if (intVector.y > 9)
					{
						intVector.x++;
						intVector.y = 0;
					}
				}
				subNodes.Add(new Button(owner, "Button_Sprites_Previous0", this, new(5f, 5f), 80f, "Previous"));
				subNodes.Add(new Button(owner, "Button_Sprites_Next0", this, new(90f, 5f), 80f, "Next"));
				OrganizeSprites(0);
			}

			public void OrganizeSprites(int page)
			{
				var intVector = new IntVector2(0, 0);
				for (var i = 0; i < subNodes.Count; i++)
				{
					if (subNodes[i] is not Button button) 
						continue;
					if (!button.IDstring.StartsWith("Button_Sprites_Next") && !button.IDstring.StartsWith("Button_Sprites_Previous"))
					{
						button.pos = (intVector.x >= page && intVector.x <= page) ? new(5f, size.y - 20f - 20f * intVector.y) : new(10000f, 10000f);
						intVector.y++;
						if (intVector.y > 9)
						{
							intVector.x++;
							intVector.y = 0;
						}
					}
					else if (button.IDstring.StartsWith("Button_Sprites_Next"))
						button.IDstring = "Button_Sprites_Next" + page;
					else if (button.IDstring.StartsWith("Button_Sprites_Previous"))
						button.IDstring = "Button_Sprites_Previous" + page;
				}
			}
        }

		public SelectSpritePanel? spriteSelectPanel;

        FanLightRepresentation Rep => (parentNode as FanLightRepresentation)!;

        FanLightData Data => ((parentNode as FanLightRepresentation)!.pObj.data as FanLightData)!;

		public FanLightControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new(250f, 165f), "Fan Light Fixture")
		{
			subNodes.Add(new Button(owner, "Subm_Button", this, new(5f, 145f), 240f, "Submersible: " + Data.submersible));
			subNodes.Add(new ControlSlider(owner, "Seed_Slider", this, new(5f, 125f), "Seed: "));
			subNodes.Add(new ControlSlider(owner, "ColorR_Slider", this, new(5f, 105f), "Red: "));
			subNodes.Add(new ControlSlider(owner, "ColorG_Slider", this, new(5f, 85f), "Green: "));
			subNodes.Add(new ControlSlider(owner, "ColorB_Slider", this, new(5f, 65f), "Blue: "));
			subNodes.Add(new ControlSlider(owner, "Speed_Slider", this, new(5f, 45f), "Speed: "));
			subNodes.Add(new ControlSlider(owner, "Inverse_Speed_Slider", this, new(5f, 25f), "Inverse Speed: "));
			subNodes.Add(new Button(owner, "Sprite_Button", this, new(5f, 5f), 240f, "Sprite: " + Data.imageName));
		}

		public virtual void Signal(DevUISignalType type, DevUINode sender, string message)
		{
            if (sender.IDstring == "Subm_Button" && Data is FanLightData d)
            {
                d.submersible = !d.submersible;
                (sender as Button)!.Text = "Submersible: " + d.submersible;
                return;
            }
            else if (Rep.files is string[] f)
			{
                if (spriteSelectPanel is SelectSpritePanel s)
                {
                    if (sender.IDstring.StartsWith("Button_Sprites_Next"))
                    {
                        var num = int.Parse(sender.IDstring[19..]);
                        var nP = f.Length / 10f - 1f;
                        if (num < nP)
                        {
                            num++;
                            s.OrganizeSprites(num);
                        }
                    }
                    else if (sender.IDstring.StartsWith("Button_Sprites_Previous"))
                    {
                        var num = int.Parse(sender.IDstring[23..]);
                        if (num > 0)
                        {
                            num--;
                            s.OrganizeSprites(num);
                        }
                    }
                    else
                    {
                        if (sender.IDstring != "Sprite_Button" && Data is FanLightData da)
                        {
                            da.imageName = sender.IDstring;
                            if (subNodes.FirstOrDefault(x => x.IDstring == "Sprite_Button") is Button b)
                                b.Text = "Sprite: " + da.imageName;
                        }
                        subNodes.Remove(s);
                        s.ClearSprites();
                        spriteSelectPanel = null;
                    }
                }
                else
                {
                    if (sender.IDstring == "Sprite_Button")
                    {
                        spriteSelectPanel = new(owner, this, new Vector2(190f, 225f) - absPos, f);
                        subNodes.Add(spriteSelectPanel);
                        return;
                    }
                }
            }
		}
	}

	public string[] files;

    public FanLightRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, pObj.type.ToString())
	{
        subNodes.Add(new FanLightControlPanel(owner, "Fan_Light_Panel", this, new(0f, 100f)) { pos = (pObj.data as FanLightData)!.panelPos });
		subNodes.Add(new Handle(owner, "Rad_Handle", this, new(0f, 100f)) { pos = (pObj.data as FanLightData)!.handlePos });
		fSprites.Add(new("Futile_White") { shader = owner.room.game.rainWorld.Shaders["VectorCircle"] });
		fSprites.Add(new("pixel") { anchorY = 0f });
		fSprites.Add(new("pixel") { anchorY = 0f });
		owner.placedObjectsContainer.AddChild(fSprites[1]);
		owner.placedObjectsContainer.AddChild(fSprites[2]);
		owner.placedObjectsContainer.AddChild(fSprites[3]);
		files = (from n in Futile.atlasManager._allElementsByName.Values where n.name.StartsWith("FanLightMask") select n.name).ToArray();
	}

	public override void Refresh()
	{
		base.Refresh();
        MoveSprite(1, absPos);
        (pObj.data as FanLightData)!.panelPos = (subNodes[0] as Panel)!.pos;
        fSprites[1].scale = (subNodes[1] as Handle)!.pos.magnitude / 8f;
        fSprites[1].alpha = 2f / (subNodes[1] as Handle)!.pos.magnitude;
        MoveSprite(2, absPos);
        fSprites[2].scaleY = (subNodes[1] as Handle)!.pos.magnitude;
        fSprites[2].rotation = AimFromOneVectorToAnother(absPos, (subNodes[1] as Handle)!.absPos);
        (pObj.data as FanLightData)!.handlePos = (subNodes[1] as Handle)!.pos;
        MoveSprite(3, absPos);
        fSprites[3].scaleY = (subNodes[0] as Panel)!.pos.magnitude;
        fSprites[3].rotation = AimFromOneVectorToAnother(absPos, (subNodes[0] as Panel)!.absPos);
    }
}
