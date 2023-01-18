using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using RWCustom;
using UnityEngine;
using DevInterface;


namespace RegionKit.Modules.Objects
{
    //Made By LeeMoriya
    public class Enums_RainbowNoFade
    {
        public static PlacedObject.Type RainbowNoFade = new(nameof(RainbowNoFade), true);
    }

    public class RainbowNoFade : CosmeticSprite
    {
        public RainbowNoFade(Room room, PlacedObject placedObject)
        {
            this.room = room;
            this.placedObject = placedObject;
            Futile.atlasManager.LoadAtlasFromTexture("rainbow", Resources.Load("Atlases/rainbow") as Texture2D, false);
            this.Refresh();
            this.alwaysShow = true;
        }

        private RainbowNoFade.RainbowNoFadeData RBData
        {
            get
            {
                return this.placedObject.data as RainbowNoFade.RainbowNoFadeData;
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.alwaysShow)
            {
                this._fade = 1f;
            }
            else
            {
                this._fade = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(Mathf.Pow(this.room.world.rainCycle.CycleStartUp, 0.75f) * 3.14159274f)), 0.6f);
                if (this.room.world.rainCycle.CycleStartUp >= 1f)
                {
                    this.Destroy();
                }
            }
        }

        public void Refresh()
        {
            this.pos = this.placedObject.pos - this.RBData.handlePos;
            this._rad = this.RBData.handlePos.magnitude * 2f;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new CustomFSprite("rainbow");
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Rainbow"];
            this.AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            float num = this._fade * Mathf.InverseLerp(0.2f, 0f, rCam.ghostMode);
            for (int i = 0; i < 4; i++)
            {
                (sLeaser.sprites[0] as CustomFSprite).MoveVertice(i, this.pos + Custom.eightDirections[1 + i * 2].ToVector2() * this._rad - camPos);
                (sLeaser.sprites[0] as CustomFSprite).verticeColors[i] = new Color(this.RBData.fades[4], 0f, 0f, num * this.RBData.fades[i]);
            }
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("GrabShaders");
            }
            base.AddToContainer(sLeaser, rCam, newContatiner);
        }

        public PlacedObject placedObject;
        private float _rad;
        private float _fade;
        public bool alwaysShow;

        public class RainbowNoFadeData : PlacedObject.ResizableObjectData
        {
            public RainbowNoFadeData(PlacedObject owner) : base(owner)
            {
                this.fades = new float[6];
                for (int i = 0; i < 4; i++)
                {
                    this.fades[i] = 1f;
                }
                this.fades[4] = 0.5f;
                this.fades[5] = 0.15f;
            }
            public float Chance
            {
                get
                {
                    return this.fades[5];
                }
            }
            public override void FromString(string s)
            {
                string[] array = Regex.Split(s, "~");
                this.handlePos.x = float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                this.handlePos.y = float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                this.panelPos.x = float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                this.panelPos.y = float.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                string[] array2 = array[4].Split(new char[]
                {
                ','
                });
                int num = 0;
                while (num < this.fades.Length && num < array2.Length)
                {
                    this.fades[num] = float.Parse(array2[num], NumberStyles.Any, CultureInfo.InvariantCulture);
                    num++;
                }
            }
            public override string ToString()
            {
                string text = string.Empty;
                for (int i = 0; i < this.fades.Length; i++)
                {
                    text += string.Format(CultureInfo.InvariantCulture, "{0}{1}", new object[]
                    {
                    this.fades[i],
                    (i >= this.fades.Length - 1) ? string.Empty : ","
                    });
                }
                return string.Format(CultureInfo.InvariantCulture, "{0}~{1}~{2}~{3}~{4}", new object[]
                {
                this.handlePos.x,
                this.handlePos.y,
                this.panelPos.x,
                this.panelPos.y,
                text
                });
            }
            public Vector2 panelPos;
            public float[] fades;
        }
    }

    public class RainbowNoFadeRepresentation : ResizeableObjectRepresentation
    {
        public RainbowNoFadeRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, "RainbowNoFade", false)
        {
            this.subNodes.Add(new RainbowNoFadeRepresentation.RainbowNoFadeControlPanel(owner, "RainbowNoFade_Panel", this, new Vector2(0f, 100f)));
            (this.subNodes[this.subNodes.Count - 1] as RainbowNoFadeRepresentation.RainbowNoFadeControlPanel).pos = (pObj.data as RainbowNoFade.RainbowNoFadeData).panelPos;
            this.fSprites.Add(new FSprite("pixel", true));
            this._lineSprite = this.fSprites.Count - 1;
            owner.placedObjectsContainer.AddChild(this.fSprites[this._lineSprite]);
            this.fSprites[this._lineSprite].anchorY = 0f;
            for (int i = 0; i < owner.room.updateList.Count; i++)
            {
                if (owner.room.updateList[i] is RainbowNoFade && (owner.room.updateList[i] as RainbowNoFade).placedObject == pObj)
                {
                    this._RainbowNoFade = (owner.room.updateList[i] as RainbowNoFade);
                    break;
                }
            }
            if (this._RainbowNoFade == null)
            {
                this._RainbowNoFade = new RainbowNoFade(owner.room, pObj);
                owner.room.AddObject(this._RainbowNoFade);
            }
            this._RainbowNoFade.alwaysShow = true;
        }

        public override void Refresh()
        {
            base.Refresh();
            base.MoveSprite(this._lineSprite, this.absPos);
            this.fSprites[this._lineSprite].scaleY = (this.subNodes[1] as RainbowNoFadeRepresentation.RainbowNoFadeControlPanel).pos.magnitude;
            this.fSprites[this._lineSprite].rotation = Custom.AimFromOneVectorToAnother(this.absPos, (this.subNodes[1] as RainbowNoFadeRepresentation.RainbowNoFadeControlPanel).absPos);
            this._RainbowNoFade.Refresh();
            (this.pObj.data as RainbowNoFade.RainbowNoFadeData).panelPos = (this.subNodes[1] as Panel).pos;
        }

        private int _lineSprite;
        private RainbowNoFade _RainbowNoFade;

        public class RainbowNoFadeControlPanel : Panel
        {
            public RainbowNoFadeControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 125f), "RainbowNoFade")
            {
                for (int i = 0; i < 4; i++)
                {
                    this.subNodes.Add(new RainbowNoFadeRepresentation.RainbowNoFadeControlPanel.FadeSlider(owner, "Fade_Slider", this, new Vector2(5.01f, 5f + 20f * (float)i), "Fade " + i + ":", i));
                }
                this.subNodes.Add(new RainbowNoFadeRepresentation.RainbowNoFadeControlPanel.FadeSlider(owner, "Thick_Slider", this, new Vector2(5.01f, 85f), "Thickness:", 4));
                this.subNodes.Add(new RainbowNoFadeRepresentation.RainbowNoFadeControlPanel.FadeSlider(owner, "Chance_Slider", this, new Vector2(5.01f, 105f), "Per cycle chance:", 5));
            }
            public RainbowNoFade.RainbowNoFadeData RainbowNoFadeData
            {
                get
                {
                    return (this.parentNode as RainbowNoFadeRepresentation).pObj.data as RainbowNoFade.RainbowNoFadeData;
                }
            }
            public class FadeSlider : Slider
            {
                public FadeSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, int index) : base(owner, IDstring, parentNode, pos, title, false, 110f)
                {
                    this._index = index;
                }
                public RainbowNoFade.RainbowNoFadeData RainbowNoFadeData
                {
                    get
                    {
                        return (this.parentNode.parentNode as RainbowNoFadeRepresentation).pObj.data as RainbowNoFade.RainbowNoFadeData;
                    }
                }
                public override void Refresh()
                {
                    base.Refresh();
                    base.NumberText = Mathf.RoundToInt(this.RainbowNoFadeData.fades[this._index] * 100f).ToString() + "%";
                    base.RefreshNubPos(this.RainbowNoFadeData.fades[this._index]);
                }
                public override void NubDragged(float nubPos)
                {
                    this.RainbowNoFadeData.fades[this._index] = nubPos;
                    this.parentNode.parentNode.Refresh();
                    this.Refresh();
                }
                private int _index;
            }
        }
    }
}



