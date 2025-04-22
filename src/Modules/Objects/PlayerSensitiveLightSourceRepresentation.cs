using System;
using DevInterface;
using RWCustom;
using UnityEngine;

// Made by Alduris
namespace RegionKit.Modules.Objects
{
    public class PlayerSensitiveLightSourceRepresentation : PlacedObjectRepresentation
    {
        protected PlayerSensitiveLightSource light;

        protected Handle radHandle;
        protected FSprite radCircle;
        protected FSprite radLine;
        
        protected Handle detectRadHandle;
        protected FSprite detectRadCircle;
        protected FSprite detectRadLine;

        protected PSLSControlPanel panel;
        protected FSprite panelLine;
        protected PlayerSensitiveLightSourceData data;

        public PlayerSensitiveLightSourceRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
        {
            data = (pObj.data as PlayerSensitiveLightSourceData)!;

            // Initialize UI stuff
            radHandle = new Handle(owner, "Rad_Handle", this, data.radHandlePos);
            detectRadHandle = new Handle(owner, "DetRad_Handle", this, data.detectRadHandlePos);
            subNodes.Add(radHandle);
            subNodes.Add(detectRadHandle);

            radCircle = new FSprite("Futile_White", true)
            {
                shader = owner.room.game.rainWorld.Shaders["VectorCircle"]
            };
            radLine = new FSprite("pixel", true)
            {
                anchorY = 0f
            };
            detectRadCircle = new FSprite("Futile_White", true)
            {
                shader = owner.room.game.rainWorld.Shaders["VectorCircle"],
                color = new Color(1f, 0f, 1f)
            };
            detectRadLine = new FSprite("pixel", true)
            {
                anchorY = 0f
            };
            panelLine = new FSprite("pixel", true)
            {
                anchorY = 0f
            };
            fSprites.Add(radCircle);
            fSprites.Add(radLine);
            fSprites.Add(detectRadCircle);
            fSprites.Add(detectRadLine);
            fSprites.Add(panelLine);

            owner.placedObjectsContainer.AddChild(radCircle);
            owner.placedObjectsContainer.AddChild(radLine);
            owner.placedObjectsContainer.AddChild(detectRadCircle);
            owner.placedObjectsContainer.AddChild(detectRadLine);
            owner.placedObjectsContainer.AddChild(panelLine);

            panel = new PSLSControlPanel(owner, "PSLS_Panel", this, data.panelPos);
            subNodes.Add(panel);

            // Find our light source
            foreach (var obj in owner.room.updateList)
            {
                if (obj is PlayerSensitiveLightSource psls && psls.po == pObj)
                {
                    light = psls;
                    break;
                }
            }

            if (light == null)
            {
                light = new PlayerSensitiveLightSource(pObj, pos, radHandle.pos.magnitude, detectRadHandle.pos.magnitude, data.minStrength, data.maxStrength, data.fadeSpeed, data.colorType.index - 2);
                owner.room.AddObject(light);
            }
        }

        public override void Refresh()
        {
            base.Refresh();

            // Update sprites
            radCircle.SetPosition(absPos + new Vector2(0.01f, 0.01f));
            radCircle.scale = radHandle.pos.magnitude / 8f;
            radCircle.alpha = 2f / radHandle.pos.magnitude;

            detectRadCircle.SetPosition(absPos + new Vector2(0.01f, 0.01f));
            detectRadCircle.scale = detectRadHandle.pos.magnitude / 8f;
            detectRadCircle.alpha = 2f / detectRadHandle.pos.magnitude;

            radLine.SetPosition(absPos + new Vector2(0.01f, 0.01f));
            radLine.scaleY = radHandle.pos.magnitude;
            radLine.rotation = AimFromOneVectorToAnother(absPos, radHandle.absPos);

            detectRadLine.SetPosition(absPos + new Vector2(0.01f, 0.01f));
            detectRadLine.scaleY = detectRadHandle.pos.magnitude;
            detectRadLine.rotation = AimFromOneVectorToAnother(absPos, detectRadHandle.absPos);
            
            panelLine.SetPosition(absPos + new Vector2(0.01f, 0.01f));
            panelLine.scaleY = radHandle.pos.magnitude;
            panelLine.rotation = AimFromOneVectorToAnother(absPos, panel.absPos);

            // Update data
            data.radHandlePos = radHandle.pos;
            data.detectRadHandlePos = detectRadHandle.pos;
            data.panelPos = panel.pos;

            // Update light
            light.pos = pObj.pos;
            light.rad = data.Rad;
            light.detectRad = data.DetectRad;
            light.effectColor = data.colorType.index - 2;
            light.minStrength = data.minStrength;
            light.maxStrength = data.maxStrength;
            light.fadeSpeed = data.fadeSpeed;
            light.Flat = data.flat;
        }

        public class PSLSControlPanel : Panel, IDevUISignals
        {
            public PSLSControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 90f), "Player-Sensitive Light Source")
            {
                subNodes.Add(new MinStrengthSlider(owner, this, new Vector2(5f, 65f)));
                subNodes.Add(new MaxStrengthSlider(owner, this, new Vector2(5f, 45f)));
                subNodes.Add(new FadeSpeedSlider(owner, this, new Vector2(5f, 25f)));
                subNodes.Add(new Button(owner, "Color_Button", this, new Vector2(5f, 5f), 100f, (parentNode as PlayerSensitiveLightSourceRepresentation)!.data.colorType.ToString()));
                subNodes.Add(new Button(owner, "Flat_Button", this, new Vector2(125f, 5f), 100f, (parentNode as PlayerSensitiveLightSourceRepresentation)!.data.flat ? "Flat: ON" : "Flat: OFF"));
            }

            public void Signal(DevUISignalType type, DevUINode sender, string message)
            {
                var data = (parentNode as PlayerSensitiveLightSourceRepresentation)!.data;
                switch (sender.IDstring)
                {
                    case "Color_Button":
                        {
                            if ((int)data.colorType >= 3)
                            {
                                data.colorType = new PlacedObject.LightSourceData.ColorType(ExtEnum<PlacedObject.LightSourceData.ColorType>.values.GetEntry(0), false);
                            }
                            else
                            {
                                data.colorType = new PlacedObject.LightSourceData.ColorType(ExtEnum<PlacedObject.LightSourceData.ColorType>.values.GetEntry(data.colorType.Index + 1), false);
                            }
                            (sender as Button)!.Text = data.colorType.ToString();
                            (parentNode as PlayerSensitiveLightSourceRepresentation)!.light.Color = Color.white;
                            (parentNode as PlayerSensitiveLightSourceRepresentation)!.light.colorDirty = true;
                            break;
                        }
                    case "Flat_Button":
                        {
                            data.flat = !data.flat;
                            (sender as Button)!.Text = data.flat ? "Flat: ON" : "FLAT: OFF";
                            break;
                        }
                }
            }

            public class MinStrengthSlider(DevUI owner, DevUINode parentNode, Vector2 pos) : Slider(owner, "Min_Strength_Slider", parentNode, pos, "Min Strength: ", false, 110f)
            {
                public override void Refresh()
                {
                    base.Refresh();
                    float num = (parentNode.parentNode as PlayerSensitiveLightSourceRepresentation)!.data.minStrength;
                    NumberText = ((int)(num * 100f)).ToString() + "%";
                    RefreshNubPos(num);
                }

                public override void NubDragged(float nubPos)
                {
                    var data = (parentNode.parentNode as PlayerSensitiveLightSourceRepresentation)!.data;
                    data.minStrength = Mathf.Min(nubPos, data.maxStrength);
                    parentNode.parentNode.Refresh();
                    Refresh();
                }
            }

            public class MaxStrengthSlider(DevUI owner, DevUINode parentNode, Vector2 pos) : Slider(owner, "Max_Strength_Slider", parentNode, pos, "Max Strength: ", false, 110f)
            {
                public override void Refresh()
                {
                    base.Refresh();
                    float num = (parentNode.parentNode as PlayerSensitiveLightSourceRepresentation)!.data.maxStrength;
                    NumberText = ((int)(num * 100f)).ToString() + "%";
                    RefreshNubPos(num);
                }

                public override void NubDragged(float nubPos)
                {
                    var data = (parentNode.parentNode as PlayerSensitiveLightSourceRepresentation)!.data;
                    data.maxStrength = Mathf.Max(nubPos, data.minStrength);
                    parentNode.parentNode.Refresh();
                    Refresh();
                }
            }

            public class FadeSpeedSlider(DevUI owner, DevUINode parentNode, Vector2 pos) : Slider(owner, "Fade_Speed_Slider", parentNode, pos, "Fade Speed: ", false, 110f)
            {
                public override void Refresh()
                {
                    base.Refresh();
                    float num = (parentNode.parentNode as PlayerSensitiveLightSourceRepresentation)!.data.fadeSpeed;
                    NumberText = ((int)(num * 100f)).ToString() + "%";
                    RefreshNubPos(num);
                }

                public override void NubDragged(float nubPos)
                {
                    var data = (parentNode.parentNode as PlayerSensitiveLightSourceRepresentation)!.data;
                    data.fadeSpeed = nubPos;
                    parentNode.parentNode.Refresh();
                    Refresh();
                }
            }
        }
    }
}
