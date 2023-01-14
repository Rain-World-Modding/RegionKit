using UnityEngine;
using System.Text.RegularExpressions;
using DevInterface;
using RWCustom;
using System.Linq;
using System.IO;
using System.Reflection;

namespace RegionKit.Modules.Objects
{
    public class LittlePlanet : CosmeticSprite
    {
        public static void ApplyHooks()
        {
            On.RainWorld.LoadResources += (orig, self) =>
            {
                orig(self);
                EmbeddedResourceLoader.LoadEmbeddedResource("LittlePlanet");
                EmbeddedResourceLoader.LoadEmbeddedResource("LittlePlanetRing");
            };
            On.Room.Loaded += (orig, self) =>
            {
                orig(self);
                for (var i = 0; i < self.roomSettings.placedObjects.Count; i++)
                {
                    var pObj = self.roomSettings.placedObjects[i];
                    if (pObj.type == EnumExt_LittlePlanet.LittlePlanet)
                        self.AddObject(new LittlePlanet(self, pObj));
                }
            };
            On.PlacedObject.GenerateEmptyData += (orig, self) =>
            {
                orig(self);
                if (self.type == EnumExt_LittlePlanet.LittlePlanet)
                    self.data = new LittlePlanetData(self);
            };
            On.DevInterface.ObjectsPage.CreateObjRep += (orig, self, tp, pObj) =>
            {
                if (tp == EnumExt_LittlePlanet.LittlePlanet)
                {
                    if (pObj is null)
                    {
                        self.RoomSettings.placedObjects.Add(pObj = new(tp, null)
                        {
                            pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new(-683f, 384f), .25f) + Custom.DegToVec(RNG.value * 360f) * .2f
                        });
                    }
                    var pObjRep = new LittlePlanetRepresentation(self.owner, "LittlePlanet_Rep", self, pObj, tp.ToString());
                    self.tempNodes.Add(pObjRep);
                    self.subNodes.Add(pObjRep);
                }
                else
                    orig(self, tp, pObj);
            };
        }

        public class LittlePlanetData : PlacedObject.ResizableObjectData
        {
            public float red = 1f;
            public float green = 1f;
            public float blue = 1f;
            public float alpha = 1f;
            public Vector2 panelPos;
            public float speed = 1f;

            public Color Color
            {
                get => new(red, green, blue);
                set
                {
                    red = value.r;
                    green = value.g;
                    blue = value.b;
                }
            }

            public LittlePlanetData(PlacedObject owner) : base(owner) { }

            public override void FromString(string s)
            {
                base.FromString(s);
                var sAr = Regex.Split(s, "~");
                red = float.Parse(sAr[3]);
                green = float.Parse(sAr[4]);
                blue = float.Parse(sAr[5]);
                alpha = float.Parse(sAr[8]);
                panelPos.x = float.Parse(sAr[9]);
                panelPos.y = float.Parse(sAr[10]);
                speed = float.Parse(sAr[12]);
            }

            public override string ToString() => $"{base.ToString()}~Color(~{red}~{green}~{blue}~)~Alpha:~{alpha}~{panelPos.x}~{panelPos.y}~Speed:~{speed}";
        }

        bool underWaterMode;
        Color color;
        readonly float baseRad;
        float alpha;
        float speed;
        readonly float[] lastRot = new float[4];
        readonly float[] rot = new float[4];
        readonly float[] rotSpeed = new float[4] { -1f, .5f, -.25f, .125f };
        readonly float[] lastScaleX = new float[3];
        readonly float[] scaleX = new float[3];
        readonly bool[] increaseRad = new bool[3];

        PlacedObject PObj { get; init; }
        LittlePlanetData Data { get; init; }

        public LittlePlanet(Room room, PlacedObject placedObj)
        {
            this.room = room;
            PObj = placedObj;
            Data = (placedObj.data as LittlePlanetData)!;
            pos = placedObj.pos;
            lastPos = pos;
            underWaterMode = room.GetTilePosition(PObj.pos).y < room.defaultWaterLevel;
            color = Data.Color;
            baseRad = Data.Rad;
            alpha = Data.alpha;
            speed = Data.speed;
        }

        public override void Update(bool eu)
        {
            pos = PObj.pos;
            underWaterMode = room.GetTilePosition(PObj.pos).y < room.defaultWaterLevel;
            color = Data.Color;
            alpha = Data.alpha;
            speed = Data.speed;
            base.Update(eu);
            for (var i = 0; i < 4; i++)
            {
                lastRot[i] = rot[i];
                rot[i] += rotSpeed[i] * speed;
            }
            for (var i = 0; i < 3; i++)
            {
                lastScaleX[i] = scaleX[i];
                scaleX[i] += (increaseRad[i] ? .02f : -.02f) * speed;
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[6]
            {
                new("Futile_White")
                {
                    shader = rCam.game.rainWorld.Shaders[(!underWaterMode) ? "FlatLight" : "UnderWaterLight"],
                    scale = baseRad / 6f
                },
                new("LittlePlanet") { shader = rCam.game.rainWorld.Shaders["Hologram"] },
                new("LittlePlanetRing") { shader = rCam.game.rainWorld.Shaders["Hologram"] },
                new("LittlePlanetRing") { shader = rCam.game.rainWorld.Shaders["Hologram"] },
                new("LittlePlanetRing") { shader = rCam.game.rainWorld.Shaders["Hologram"] },
                new("Futile_White")
                {
                    shader = rCam.game.rainWorld.Shaders[(!underWaterMode) ? "FlatLight" : "UnderWaterLight"],
                    scale = baseRad / 24f
                }
            };
            for (var i = 1; i < sLeaser.sprites.Length - 1; i++)
                sLeaser.sprites[i].scale = baseRad / 400f * i;
            sLeaser.sprites[1].scale -= sLeaser.sprites[1].scale / 4f;
            AddToContainer(sLeaser, rCam, null!);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            var sPos = Vector2.Lerp(lastPos, pos, timeStacker);
            var sRot = new[]
            {
                Mathf.Lerp(lastRot[0], rot[0], timeStacker),
                Mathf.Lerp(lastRot[1], rot[1], timeStacker),
                Mathf.Lerp(lastRot[2], rot[2], timeStacker),
                Mathf.Lerp(lastRot[3], rot[3], timeStacker)
            };
            var sScaleX = new[]
            {
                baseRad / 400f * 2f * Mathf.Sin(Mathf.Lerp(lastScaleX[0], scaleX[0], timeStacker) * Mathf.PI),
                baseRad / 400f * 3f * Mathf.Cos(Mathf.Lerp(lastScaleX[1], scaleX[1], timeStacker) * Mathf.PI),
                baseRad / 400f * 4f * Mathf.Sin(Mathf.Lerp(lastScaleX[0], scaleX[0], timeStacker) * Mathf.PI)
            };
            foreach (var s in sLeaser.sprites)
            {
                s.x = sPos.x - camPos.x;
                s.y = sPos.y - camPos.y;
                s.alpha = alpha;
            }
            for (var i = 1; i < sLeaser.sprites.Length - 1; i++)
                sLeaser.sprites[i].rotation = sRot[i - 1];
            for (var i = 2; i < sLeaser.sprites.Length - 1; i++)
            {
                sLeaser.sprites[i].scaleX = sScaleX[i - 2];
                if (sLeaser.sprites[i].scaleX >= baseRad / 400f * i)
                    increaseRad[i - 2] = false;
                else if (sLeaser.sprites[i].scaleX <= 0f)
                    increaseRad[i - 2] = true;
            }
            sLeaser.sprites[0].alpha /= 2f;
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders[(!underWaterMode) ? "FlatLight" : "UnderWaterLight"];
            sLeaser.sprites[5].shader = rCam.game.rainWorld.Shaders[(!underWaterMode) ? "FlatLight" : "UnderWaterLight"];
            for (var i = 0; i < sLeaser.sprites.Length; i++)
                sLeaser.sprites[i].color = color;
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContainer)
        {
            newContainer ??= rCam.ReturnFContainer("GrabShaders");
            sLeaser.sprites.RemoveFromContainer();
            newContainer.AddChild(sLeaser.sprites);
        }
    }

    public class LittlePlanetRepresentation : ResizeableObjectRepresentation
    {
        public class LittlePlanetControlPanel : Panel, IDevUISignals
        {
            public class ControlSlider : Slider
            {
                LittlePlanet.LittlePlanetData Data { get; init; }

                public ControlSlider(
					DevUI owner,
					string IDstring,
					DevUINode parentNode,
					Vector2 pos,
					string title) : base(
						owner,
						IDstring,
						parentNode,
						pos,
						title,
						false,
						110f) 
					=> Data 
						= ((parentNode.parentNode as LittlePlanetRepresentation)!.pObj.data as LittlePlanet.LittlePlanetData)!;

                public override void Refresh()
                {
                    base.Refresh();
                    var num = 0f;
                    switch (IDstring)
                    {
                        case "ColorR_Slider":
                            num = Data.red;
                            NumberText = ((int)(255f * num)).ToString();
                            break;
                        case "ColorG_Slider":
                            num = Data.green;
                            NumberText = ((int)(255f * num)).ToString();
                            break;
                        case "ColorB_Slider":
                            num = Data.blue;
                            NumberText = ((int)(255f * num)).ToString();
                            break;
                        case "Alpha_Slider":
                            num = Data.alpha;
                            NumberText = ((int)(100f * num)).ToString() + "%";
                            break;
                        case "Speed_Slider":
                            num = Data.speed / 2f;
                            NumberText = ((int)(100f * num)).ToString() + "%";
                            break;
                    }
                    RefreshNubPos(num);
                }

                public override void NubDragged(float nubPos)
                {
                    switch (IDstring)
                    {
                        case "ColorR_Slider":
                            Data.red = nubPos;
                            break;
                        case "ColorG_Slider":
                            Data.green = nubPos;
                            break;
                        case "ColorB_Slider":
                            Data.blue = nubPos;
                            break;
                        case "Alpha_Slider":
                            Data.alpha = nubPos;
                            break;
                        case "Speed_Slider":
                            Data.speed = nubPos * 2f;
                            break;
                    }
                    parentNode.parentNode.Refresh();
                    Refresh();
                }
            }

            public LittlePlanetControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new(250f, 105f), "Little Planet")
            {
                subNodes.Add(new ControlSlider(owner, "ColorR_Slider", this, new(5f, 85f), "Red: "));
                subNodes.Add(new ControlSlider(owner, "ColorG_Slider", this, new(5f, 65f), "Green: "));
                subNodes.Add(new ControlSlider(owner, "ColorB_Slider", this, new(5f, 45f), "Blue: "));
                subNodes.Add(new ControlSlider(owner, "Alpha_Slider", this, new(5f, 25f), "Alpha: "));
                subNodes.Add(new ControlSlider(owner, "Speed_Slider", this, new(5f, 5f), "Speed: "));
            }

            public void Signal(DevUISignalType type, DevUINode sender, string message) { }
        }

        readonly int linePixelSpriteIndex;
        readonly LittlePlanetControlPanel panel;

        public LittlePlanetRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, pObj.type.ToString(), true)
        {
            panel = new(owner, "LittlePlanet_Panel", this, new(0f, 100f));
            subNodes.Add(panel);
            panel.pos = (pObj.data as LittlePlanet.LittlePlanetData)!.panelPos;
            fSprites.Add(new("pixel"));
            linePixelSpriteIndex = fSprites.Count - 1;
            owner.placedObjectsContainer.AddChild(fSprites[linePixelSpriteIndex]);
            fSprites[linePixelSpriteIndex].anchorY = 0f;
        }

        public override void Refresh()
        {
            base.Refresh();
            MoveSprite(linePixelSpriteIndex, absPos);
            fSprites[linePixelSpriteIndex].scaleY = panel.pos.magnitude;
            fSprites[linePixelSpriteIndex].rotation = Custom.AimFromOneVectorToAnother(absPos, panel.absPos);
            (pObj.data as LittlePlanet.LittlePlanetData)!.panelPos = panel.pos;
        }
    }

    public static class LittlePlanetExtensions
    {
        public static void AddChild(this FContainer self, params FNode[] nodes)
        {
            foreach (var node in nodes)
                self.AddChild(node);
        }

        public static void RemoveFromContainer(this FNode[] self)
        {
            foreach (var node in self)
                node.RemoveFromContainer();
        }
    }

    public static class EnumExt_LittlePlanet
    {
        public static PlacedObject.Type LittlePlanet = new(nameof(LittlePlanet), true);
    }

    public static class EmbeddedResourceLoader
    {
        public static void LoadEmbeddedResource(string name)
        {
			//todo: update ER
            var thisAssembly = Assembly.GetExecutingAssembly();
            var resourceName = thisAssembly.GetManifestResourceNames().First(r => r.Contains(name));
            var resource = thisAssembly.GetManifestResourceStream(resourceName);
            using MemoryStream memoryStream = new();
            var buffer = new byte[16384];
            int count;
            while ((count = resource!.Read(buffer, 0, buffer.Length)) > 0)
                memoryStream.Write(buffer, 0, count);
            Texture2D spriteTexture = new(0, 0, TextureFormat.ARGB32, false);
            
			//TODO: check if loads correctly
			spriteTexture.LoadImage(memoryStream.ToArray());
            spriteTexture.anisoLevel = 1;
            spriteTexture.filterMode = 0;
            FAtlas atlas = new(name, spriteTexture, FAtlasManager._nextAtlasIndex, false);
            Futile.atlasManager.AddAtlas(atlas);
            FAtlasManager._nextAtlasIndex++;
        }
    }
}
