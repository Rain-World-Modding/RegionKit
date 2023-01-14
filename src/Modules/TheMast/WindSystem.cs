using System;
using System.Collections.Generic;
using DevInterface;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;
using System.Text.RegularExpressions;

//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast
{
    public static class EnumExt_WindSystem
    {
        public static PlacedObject.Type PlacedWind = new(nameof(PlacedWind), true);
    }

    internal static class WindSystem
    {
        public static void Apply()
        {
            On.Room.Loaded += Room_Loaded;
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep;
            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData;
            On.DevInterface.ObjectsPage.RemoveObject += ObjectsPage_RemoveObject;
            On.DevInterface.QuadObjectRepresentation.ctor += QuadObjectRepresentation_ctor;
        }

        private static void QuadObjectRepresentation_ctor(On.DevInterface.QuadObjectRepresentation.orig_ctor orig, QuadObjectRepresentation self, DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name)
        {
            orig(self, owner, IDstring, parentNode, pObj, name);
            if (pObj.type == EnumExt_WindSystem.PlacedWind)
            {
                // Replace the default control panel with one specific to placed wind
                WindControlPanel controlPanel = new WindControlPanel(owner, "Wind_Control_Panel", self, new Vector2(0f, 100f));
                self.subNodes.Add(controlPanel);
                controlPanel.pos = Vector2.zero;
            }
        }

        private static void ObjectsPage_RemoveObject(On.DevInterface.ObjectsPage.orig_RemoveObject orig, ObjectsPage self, PlacedObjectRepresentation objRep)
        {
            List<UpdatableAndDeletable> objs = self.owner.room.updateList;
            for(int i = objs.Count - 1; i >= 0; i--)
            {
                if ((objs[i] is Wind wind) && (wind.placedObj == objRep.pObj))
                    wind.RemoveFromRoom();
            }
            orig(self, objRep);
        }

        private static void PlacedObject_GenerateEmptyData(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
        {
            orig(self);
            if(self.type == EnumExt_WindSystem.PlacedWind)
                self.data = new WindData(self);
        }

        private static void ObjectsPage_CreateObjRep(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
        {
            if (tp == EnumExt_WindSystem.PlacedWind)
            {
                // From ObjectsPage.CreateObjRep
                if (pObj == null)
                {
                    pObj = new PlacedObject(tp, null);
                    pObj.pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + Custom.DegToVec(Random.value * 360f) * 0.2f;
                    self.RoomSettings.placedObjects.Add(pObj);
                    self.owner.room.AddObject(new Wind(pObj));
                }
                QuadObjectRepresentation rep = new QuadObjectRepresentation(self.owner, tp.ToString() + "_Rep", self, pObj, tp.ToString());
                self.tempNodes.Add(rep);
                self.subNodes.Add(rep);
            }
            else
                orig(self, tp, pObj);
        }

        // Create Wind objects corresponding to placed objects
        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            for (int i = 0; i < self.roomSettings.placedObjects.Count; i++)
            {
                PlacedObject pObj = self.roomSettings.placedObjects[i];
                if (!pObj.active) continue;
                if (pObj.type != EnumExt_WindSystem.PlacedWind) continue;
                self.AddObject(new Wind(pObj));
            }
        }

        public class Wind : UpdatableAndDeletable
        {
            public PlacedObject placedObj;
            
            public Wind(PlacedObject pObj)
            {
                placedObj = pObj;
            }

            private WindData Data => (WindData)placedObj.data;

            public override void Update(bool eu)
            {
                base.Update(eu);

                // Find all chunks in this area and change their velocities
                float targetVel = Data.velocity * (80f + 10f * Mathf.Sin(room.game.clock / 40f * 2f));
                float force = 0.02f;

                List<UpdatableAndDeletable> objs = room.updateList;
                BodyChunk[] chunks;

                RoomCamera cam = room.game.cameras[0];
                Rect levelBounds = new Rect();
                levelBounds.xMin = -40f + Mathf.Min(0f, cam.pos.x);
                levelBounds.yMin = -40f + Mathf.Min(0f, cam.pos.y);
                levelBounds.xMax =  40f + Mathf.Max(room.PixelWidth, cam.pos.x + cam.sSize.x);
                levelBounds.yMax =  40f + Mathf.Max(room.PixelHeight, cam.pos.y + cam.sSize.y);

                for(int i = objs.Count - 1; i >= 0; i--)
                {
                    if (objs[i] is PhysicalObject po)
                    {
                        bool affectObject = false;
                        if (Data.affectGroup != WindData.AffectGroup.Visuals)
                        {
                            if (Data.affectGroup == WindData.AffectGroup.Creatures) affectObject = true;
                            else if (Data.affectGroup == WindData.AffectGroup.Objects && po.abstractPhysicalObject.type != AbstractPhysicalObject.AbstractObjectType.Creature) affectObject = true;
                        }

                        if (affectObject) {
                            bool highTraction = !(po.abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Creature);
                            chunks = po.bodyChunks;
                            for (int chunk = chunks.Length - 1; chunk >= 0; chunk--)
                            {
                                float chunkForce = 1f;
                                if (chunks[chunk].ContactPoint.y == -1)
                                {
                                    if (highTraction)
                                        continue;
                                    chunkForce = 0.5f;
                                }
                                if (chunks[chunk].ContactPoint.x == -Math.Sign(targetVel)) chunkForce *= 0.1f;
                                if (po is Player)
                                {
                                    Player.AnimationIndex anim = ((Player)po).animation;
                                    if (anim == Player.AnimationIndex.GetUpToBeamTip)
                                        continue;
                                    if (anim == Player.AnimationIndex.GetUpOnBeam)
                                        chunkForce *= 0.1f;
                                    if (((Player)po).bodyMode == Player.BodyModeIndex.CorridorClimb)
                                        continue;
                                }

                                // Push creatures back less if their legs are on the ground
                                if (po is Lizard liz)
                                    chunkForce *= Custom.LerpMap(liz.LegsGripping, 0f, 3f, 1f, 0.1f);

                                float overlap = EstimateOverlap(chunks[chunk]);
                                chunkForce *= force * overlap / (chunks[chunk].mass + 0.5f);
                                if (Data.vertGroup == WindData.VertGroup.Horizontal)
                                {
                                    chunks[chunk].vel.x = ApplyWind(chunks[chunk].vel.x, targetVel, chunkForce);
                                }
                                else
                                {
                                    chunks[chunk].vel.y = ApplyWind(chunks[chunk].vel.y, targetVel, chunkForce);
                                }
                            }
                        }
                        if (po is Player ply && ply.graphicsModule != null)
                        {
                            PlayerGraphics pg = (PlayerGraphics)ply.graphicsModule;
                            for (int j = 0; j < pg.tail.Length; j++)
                            {
                                if (Data.vertGroup == WindData.VertGroup.Horizontal)
                                {
                                    if (WindAffectsPoint(pg.tail[j].pos))
                                        pg.tail[j].vel.x = ApplyWind(pg.tail[j].vel.x, targetVel, force);
                                }
                                else if (Data.vertGroup == WindData.VertGroup.Vertical)
                                {
                                    if (WindAffectsPoint(pg.tail[j].pos))
                                        pg.tail[j].vel.y = ApplyWind(pg.tail[j].vel.y, targetVel, force);
                                }
                            }
                        }
                    }
                    else if (objs[i] is SkyDandelions.SkyDandelion sd)
                    {
                        // Make sky dandelions wrap around the screen

                        Vector2 sPos = sd.pos;
                        if (sPos.x < levelBounds.xMin) sPos.x += levelBounds.width;
                        if (sPos.x > levelBounds.xMax) sPos.x -= levelBounds.width;
                        sd.lastPos += sPos - sd.pos;
                        sd.pos = sPos;
                        if (Data.vertGroup == WindData.VertGroup.Horizontal)
                        {

                            if (WindAffectsPoint(sd.pos)) sd.vel.x = ApplyWind(sd.vel.x, targetVel, force);
                        }
                        else if (Data.vertGroup == WindData.VertGroup.Vertical)
                        {

                            if (WindAffectsPoint(sd.pos)) sd.vel.y = ApplyWind(sd.vel.y, targetVel, force);
                        }
                    }
                    else if (objs[i] is Smoke.SmokeSystem.SmokeSystemParticle sp)
                    {
                        if (Data.vertGroup == WindData.VertGroup.Horizontal)
                        {
                            if (WindAffectsPoint(sp.pos)) sp.vel.x = ApplyWind(sp.vel.x, targetVel, force);
                        }
                        else if (Data.vertGroup == WindData.VertGroup.Vertical)
                        {

                            if (WindAffectsPoint(sp.pos)) sp.vel.y = ApplyWind(sp.vel.y, targetVel, force);
                        }
                    }
                }
            }

            private float ApplyWind(float vel, float target, float force)
            {
                if (Mathf.Sign(vel) == Mathf.Sign(target) && (Mathf.Abs(vel) > Mathf.Abs(target))) return vel;
                return Mathf.Lerp(vel, target, force);
            }

            private const float _sqrt2 = 1.4142135623730951f;
            private Vector2[] _testPoints = new Vector2[]
            {
                new Vector2( 0f,  1f),
                new Vector2( 1f,  0f),
                new Vector2( 0f, -1f),
                new Vector2(-1f,  0f),
                new Vector2( _sqrt2,  _sqrt2) * 0.5f,
                new Vector2( _sqrt2, -_sqrt2) * 0.5f,
                new Vector2(-_sqrt2, -_sqrt2) * 0.5f,
                new Vector2(-_sqrt2,  _sqrt2) * 0.5f
            };
            private float EstimateOverlap(BodyChunk c)
            {
                WindData wd = (WindData)placedObj.data;
                float o = 0f;
                for(int i = _testPoints.Length - 1; i >= 0; i--)
                {
                    Vector2 p = c.pos + c.rad * _testPoints[i];
                    if (WindAffectsPoint(p))
                        o += 1f;
                }
                o /= _testPoints.Length;
                return o;
            }

            private bool WindAffectsPoint(Vector2 p)
            {
                WindData wd = (WindData)placedObj.data;
                p -= placedObj.pos;
                return TriContainsPoint(p, Vector2.zero, wd.handles[0], wd.handles[1]) || TriContainsPoint(p, wd.handles[1], wd.handles[2], Vector2.zero);
            }

            // From https://blackpawn.com/texts/pointinpoly/
            private static bool TriContainsPoint(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            {
                // Compute vectors        
                Vector2 v0 = c - a;
                Vector2 v1 = b - a;
                Vector2 v2 = p - a;

                // Compute dot products
                float dot00 = Vector2.Dot(v0, v0);
                float dot01 = Vector2.Dot(v0, v1);
                float dot02 = Vector2.Dot(v0, v2);
                float dot11 = Vector2.Dot(v1, v1);
                float dot12 = Vector2.Dot(v1, v2);

                // Compute barycentric coordinates
                float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
                float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
                float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

                // Check if point is in triangle
                return (u >= 0) && (v >= 0) && (u + v < 1);
            }
        }

        public class WindData : PlacedObject.QuadObjectData
        {
            public float velocity;
            public enum AffectGroup : byte
            {
                Visuals, Objects, Creatures
            }
            public AffectGroup affectGroup;
            public enum VertGroup : byte
            {
                Horizontal, Vertical
            }
            public VertGroup vertGroup;

            public WindData(PlacedObject owner) : base(owner)
            {
                affectGroup = AffectGroup.Visuals;
            }

            public override void FromString(string s)
            {
                base.FromString(s);
                string[] array = Regex.Split(s, "~");
                if (array.Length >= 7 && float.TryParse(array[6], out float velocity))
                    this.velocity = velocity;
                try
                {
                    if (array.Length >= 8)
                    {
                        AffectGroup ag = (AffectGroup)Enum.Parse(typeof(AffectGroup), array[7]);
                        affectGroup = ag;
                    }
                }
                catch (ArgumentException) { }
                try
                {
                    if (array.Length >= 9)
                    {
                        VertGroup ag = (VertGroup)Enum.Parse(typeof(VertGroup), array[8]);
                        vertGroup = ag;
                    }
                }
                catch (ArgumentException) { }
            }

            public override string ToString()
            {
                return string.Concat(base.ToString(), "~", velocity.ToString(), "~", affectGroup.ToString(), "~", vertGroup.ToString());
            }
        }

        public class WindControlPanel : Panel, IDevUISignals
        {
            public WindControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 50f), "Wind Area")
            {
                subNodes.Add(new WindStrengthSlider(owner, "Wind_Strength", this, new Vector2(5f, 5f), "Velocity"));
                subNodes.Add(new WindAffectGroupCycler(owner, "Wind_Affect_Group", this, new Vector2(55f, 25f), 60f));
                subNodes.Add(new WindVertGroupCycler(owner, "Wind_Vert_Group", this, new Vector2(175f, 25f), 70f));
            }

            public void Signal(DevUISignalType type, DevUINode sender, string message)
            {
            }

            private class WindAffectGroupCycler : Button
            {
                public const string prefix = "Affects:";

                public WindData.AffectGroup[] options;

                public WindAffectGroupCycler(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width) : base(owner, IDstring, parentNode, pos, width, string.Empty)
                {
                    subNodes.Add(new DevUILabel(owner, "Title", this, new Vector2(-50f, 0f), 45f, prefix));
                    options = (WindData.AffectGroup[])Enum.GetValues(typeof(WindData.AffectGroup));
                }
                
                private WindData Data
                {
                    get
                    {
                        if (!(parentNode.parentNode is QuadObjectRepresentation rep) || !(rep.pObj.data is WindData wd)) return null;
                        return wd;
                    }
                }

                public override void Refresh()
                {
                    base.Refresh();
                    SwitchOption(Data.affectGroup);
                }
                
                private void SwitchOption(WindData.AffectGroup ag)
                {
                    Data.affectGroup = ag;
                    Text = ag.ToString();
                }

                public override void Clicked()
                {
                    int op = Array.IndexOf(options, Data.affectGroup) + 1;
                    SwitchOption(options[op % options.Length]);
                }
            }
            private class WindVertGroupCycler : Button
            {
                public const string prefix = "Vertical:";

                public WindData.VertGroup[] options;

                public WindVertGroupCycler(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width) : base(owner, IDstring, parentNode, pos, width, string.Empty)
                {
                    subNodes.Add(new DevUILabel(owner, "Title", this, new Vector2(-50f, 0f), 45f, prefix));
                    options = (WindData.VertGroup[])Enum.GetValues(typeof(WindData.VertGroup));
                }

                private WindData Data
                {
                    get
                    {
                        if (!(parentNode.parentNode is QuadObjectRepresentation rep) || !(rep.pObj.data is WindData wd)) return null;
                        return wd;
                    }
                }

                public override void Refresh()
                {
                    base.Refresh();
                    SwitchOption(Data.vertGroup);
                }

                private void SwitchOption(WindData.VertGroup ag)
                {
                    Data.vertGroup = ag;
                    Text = ag.ToString();
                }

                public override void Clicked()
                {
                    int op = Array.IndexOf(options, Data.vertGroup) + 1;
                    SwitchOption(options[op % options.Length]);
                }
            }
            private class WindStrengthSlider : Slider
            {
                public WindStrengthSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title, false, 110f)
                {
                    // Shrink title
                    subNodes[0].fSprites[0].scaleX -= 30f;
                    // Expand number
                    subNodes[1].fSprites[0].scaleX += 30f;
                    ((DevUILabel)subNodes[1]).Move(((DevUILabel)subNodes[1]).pos + new Vector2(-30f, 0f));
                }

                public override void Refresh()
                {
                    base.Refresh();

                    if (!(parentNode.parentNode is QuadObjectRepresentation rep) || !(rep.pObj.data is WindData wd)) return;

                    if (IDstring == "Wind_Strength")
                    {
                        NumberText = wd.velocity.ToString("0.00");
                        RefreshNubPos(wd.velocity * 0.5f + 0.5f);
                    }
                }

                public override void NubDragged(float nubPos)
                {
                    base.NubDragged(nubPos);
                    if (!(parentNode.parentNode is QuadObjectRepresentation rep) || !(rep.pObj.data is WindData wd)) return;
                    wd.velocity = nubPos * 2f - 1f;
                    NumberText = wd.velocity.ToString("0.00");
                }
            }
        }
    }
}
