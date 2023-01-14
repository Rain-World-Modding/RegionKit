using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace RegionKit.AridBarrens
{
    public class SandStorm : BackgroundScene, INotifyWhenRoomIsReady
    {
        public SandStorm(RoomSettings.RoomEffect effect, Room room) : base(room)
        {
            deathtimer = 0;
            this.effect = effect;
            this.sceneOrigo = new Vector2(2514f, 26000);
            this.generalFog = new Fog(this);
            this.AddElement(this.generalFog);
            this.rainReach = new int[room.TileWidth];
            for (int i = 0; i < room.TileWidth; i++)
            {
                bool flag = true;
                for (int j = room.TileHeight - 1; j >= 0; j--)
                {
                    if (flag && room.GetTile(i, j).Solid)
                    {
                        flag = false;
                        this.rainReach[i] = j;
                    }
                }
            }
            this.particles = new List<SandPart>();
            float num = Mathf.Lerp(0f, 0.6f, effect.amount);
            this.totParticles = Custom.IntClamp((int)((float)(room.TileWidth * room.TileHeight) * num), 1, 300);
        }

        private RainCycle cycle
        {
            get
            {
                return this.room.game.world.rainCycle;
            }
        }

        public float Intensity
        {
            get
            {
                return this.effect.amount * Mathf.Pow(Mathf.InverseLerp((float)(this.cycle.cycleLength - 400), (float)(this.cycle.cycleLength + 2400), (float)this.cycle.timer), 2.2f);
            }
        }

        public override void AddElement(BackgroundScene.BackgroundSceneElement element)
        {
            base.AddElement(element);
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.Intensity == 0f)
            {
                return;
            }
            for (int i = this.particles.Count - 1; i >= 0; i--)
            {
                if (this.particles[i].slatedForDeletetion)
                {
                    this.particles.RemoveAt(i);
                }
                else
                {
                    this.particles[i].vel += this.wind * 0.2f;
                }
            }
            if (this.particles.Count < this.totParticles * Mathf.Pow(this.Intensity, 0.2f))
            {
                this.AddSpark();
            }
            this.wind += Custom.RNV() * 0.1f;
            this.wind *= 0.98f;
            this.wind = Vector2.ClampMagnitude(this.wind, 1f);
            if (this.soundLoop == null)
            {
                this.soundLoop = new DisembodiedDynamicSoundLoop(this);
                this.soundLoop.sound = SoundID.Void_Sea_Worm_Swimby_Woosh_LOOP;
                this.soundLoop.Volume = 0f;
            }
            else
            {
                this.soundLoop.Update();
                this.soundLoop.Volume = Mathf.Pow(this.Intensity, 0.5f);
            }
            if (this.soundLoop2 == null)
            {
                this.soundLoop2 = new DisembodiedDynamicSoundLoop(this);
                this.soundLoop2.sound = SoundID.Gate_Electric_Steam_LOOP;
                this.soundLoop2.Volume = 0f;
            }
            else
            {
                this.soundLoop2.Update();
                this.soundLoop2.Volume = Mathf.Pow(this.Intensity, 0.1f) * Mathf.Lerp(0.5f + 0.5f * Mathf.Sin(this.sin * 3.14159274f * 2f), 0f, Mathf.Pow(this.Intensity, 8f));
            }

            this.sin += 0.002f;
            if (this.closeToWallTiles != null && this.room.BeingViewed && UnityEngine.Random.value < Mathf.InverseLerp(1000f, 9120f, (float)(this.room.TileWidth * this.room.TileHeight)) * 2f * Mathf.Pow(this.Intensity, 0.3f))
            {
                IntVector2 pos = this.closeToWallTiles[UnityEngine.Random.Range(0, this.closeToWallTiles.Count)];
                Vector2 pos2 = this.room.MiddleOfTile(pos) + new Vector2(Mathf.Lerp(-10f, 10f, UnityEngine.Random.value), Mathf.Lerp(-10f, 10f, UnityEngine.Random.value));
                float num = UnityEngine.Random.value * this.Intensity;
                if (this.room.ViewedByAnyCamera(pos2, 50f))
                {
                    this.room.AddObject(new SandPuff(pos2, num));
                }
            }
            if (this.Intensity > 0.1)
            {
                ThrowAroundObjects();
            }

            if (this.Intensity > 0.99f && killedCreatures == false)
            {
                deathtimer++;
                if (deathtimer > 500)
                {
                    deathtimer = 450;
                    for (int j = 0; j < this.room.physicalObjects.Length; j++)
                    {
                        for (int k = 0; k < this.room.physicalObjects[j].Count; k++)
                        {
                            if (this.room.physicalObjects[j][k] is Creature)
                            {
                                if (!(this.room.physicalObjects[j][k] as Creature).dead)
                                {
                                    (this.room.physicalObjects[j][k] as Creature).Violence(null, null, this.room.physicalObjects[j][k].bodyChunks[0], null, Creature.DamageType.Blunt, 1.8f, 40f);
                                    if ((this.room.physicalObjects[j][k] as Creature) is Player)
                                    {
                                        killedCreatures = true;
                                    }
                                }
                                else if ((this.room.physicalObjects[j][k] as Creature) is Player)
                                {
                                    killedCreatures = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddSpark()
        {
            IntVector2 pos = new IntVector2(0, 0);
            if (UnityEngine.Random.value < (float)this.room.TileHeight / (float)this.room.TileWidth)
            {
                pos = new IntVector2(0, UnityEngine.Random.Range(0, this.room.TileHeight));
            }
            else
            {
                pos = new IntVector2(UnityEngine.Random.Range(0, this.room.TileWidth), 0);
            }
            if (!this.room.GetTile(pos).Solid)
            {
                Vector2 vector = this.room.MiddleOfTile(pos);
                int num = 0;
                while (num < 10 && this.room.ViewedByAnyCamera(vector, 200f))
                {
                    vector += Custom.DirVec(this.room.RoomRect.Center, vector) * 100f;
                    num++;
                }
                SandPart particle = new SandPart(vector);
                this.room.AddObject(particle);
                this.particles.Add(particle);
            }
        }

        public float InsidePushAround
        {
            get
            {
                return this.effect.amount * Intensity;
            }
        }

        private void ThrowAroundObjects()
        {
            if (this.Intensity == 0f)
            {
                return;
            }
            for (int i = 0; i < this.room.physicalObjects.Length; i++)
            {
                for (int j = 0; j < this.room.physicalObjects[i].Count; j++)
                {
                    for (int k = 0; k < this.room.physicalObjects[i][j].bodyChunks.Length; k++)
                    {
                        BodyChunk bodyChunk = this.room.physicalObjects[i][j].bodyChunks[k];
                        IntVector2 tilePosition = this.room.GetTilePosition(bodyChunk.pos + new Vector2(Mathf.Lerp(-bodyChunk.rad, bodyChunk.rad, UnityEngine.Random.value), Mathf.Lerp(-bodyChunk.rad, bodyChunk.rad, UnityEngine.Random.value)));
                        float num = this.InsidePushAround;
                        bool flag = false;
                        if (this.rainReach[Custom.IntClamp(tilePosition.x, 0, this.room.TileWidth - 1)] < tilePosition.y)
                        {
                            flag = true;
                            num = Mathf.Max(Intensity, this.InsidePushAround);
                        }
                        if (this.room.water)
                        {
                            num *= Mathf.InverseLerp(this.room.FloatWaterLevel(bodyChunk.pos.x) - 100f, this.room.FloatWaterLevel(bodyChunk.pos.x), bodyChunk.pos.y);
                        }
                        if (num > 0f)
                        {
                            bodyChunk.vel += Custom.DegToVec(Mathf.Lerp(0f, 360f, UnityEngine.Random.value)) * UnityEngine.Random.value * 6.5f * this.InsidePushAround;
                        }
                    }
                }
            }
        }

        public void ShortcutsReady()
        {
            killedCreatures = false;
        }

        public void AIMapReady()
        {
            this.closeToWallTiles = new List<IntVector2>();
            for (int i = 0; i < this.room.TileWidth; i++)
            {
                for (int j = 0; j < this.room.TileHeight; j++)
                {
                    if (this.room.aimap.getAItile(i, j).terrainProximity == 1)
                    {
                        this.closeToWallTiles.Add(new IntVector2(i, j));
                    }
                }
            }
        }

        private RoomSettings.RoomEffect effect;

        public List<IntVector2> closeToWallTiles;

        public bool killedCreatures = true;

        private float sin;

        public int[] rainReach;

        private int deathtimer = 0;

        public List<SandPart> particles;

        private int totParticles;

        public Vector2 wind;

        public DisembodiedDynamicSoundLoop soundLoop;

        public DisembodiedDynamicSoundLoop soundLoop2;
        public class SandPuff : CosmeticSprite
        {
            public SandPuff(Vector2 pos, float size)
            {
                this.pos = pos;
                this.lastPos = pos;
                this.size = size;
                this.lastLife = 1f;
                this.life = 1f;
                this.lifeTime = Mathf.Lerp(40f, 120f, UnityEngine.Random.value) * Mathf.Lerp(0.5f, 1.5f, size);
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                this.pos.y = this.pos.y + 0.5f;
                this.pos.x = this.pos.x + 0.25f;
                this.lastLife = this.life;
                this.life -= 1f / this.lifeTime;
                if (this.lastLife < 0f)
                {
                    this.Destroy();
                }
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[1];
                sLeaser.sprites[0] = new FSprite("Futile_White", true);
                sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Spores"];
                this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Background"));
                base.InitiateSprites(sLeaser, rCam);
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                sLeaser.sprites[0].x = Mathf.Lerp(this.lastPos.x, this.pos.x, timeStacker) - camPos.x;
                sLeaser.sprites[0].y = Mathf.Lerp(this.lastPos.y, this.pos.y, timeStacker) - camPos.y;
                sLeaser.sprites[0].scale = 10f * Mathf.Pow(1f - Mathf.Lerp(this.lastLife, this.life, timeStacker), 0.35f) * Mathf.Lerp(0.5f, 2.5f, this.size);
                sLeaser.sprites[0].alpha = Mathf.Lerp(this.lastLife, this.life, timeStacker);
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }

            public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                sLeaser.sprites[0].color = palette.texture.GetPixel(9, 5);
                base.ApplyPalette(sLeaser, rCam, palette);
            }

            private float life;

            private float lastLife;

            private float lifeTime;

            private float size;
        }

        public class Fog : BackgroundScene.FullScreenSingleColor
        {
            public Fog(SandStorm sandStormScene) : base(sandStormScene, default(Color), 1f, true, float.MaxValue)
            {
                this.depth = 0f;
            }

            private float Intensity
            {
                get
                {
                    return (base.scene as SandStorm).Intensity;
                }
            }


            private SandStorm SandStormScene
            {
                get
                {
                    return this.scene as SandStorm;
                }
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[1];
                sLeaser.sprites[0] = new FSprite("pixel", true);
                sLeaser.sprites[0].scaleX = (rCam.game.rainWorld.screenSize.x + 20f) / 1f;
                sLeaser.sprites[0].scaleY = (rCam.game.rainWorld.screenSize.y + 20f) / 1f;
                sLeaser.sprites[0].x = rCam.game.rainWorld.screenSize.x / 2f;
                sLeaser.sprites[0].y = rCam.game.rainWorld.screenSize.y / 2f;
                //sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Background"];
                sLeaser.sprites[0].color = this.color;
                sLeaser.sprites[0].alpha = this.alpha;
                this.AddToContainer(sLeaser, rCam, null);
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                this.alpha = Intensity / 1.1f;
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }

            public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                this.color = palette.skyColor;
                base.ApplyPalette(sLeaser, rCam, palette);
            }
        }

        public SandStorm.Fog generalFog;
        public class SandPart : CosmeticSprite
        {
            public SandPart(Vector2 pos)
            {
                this.pos = pos;
                this.lastLastPos = pos;
                this.lastPos = pos;
                this.vel = new Vector2(0f, 0f);
                this.life = 1f;
                this.lifeTime = Mathf.Lerp(600f, 1200f, UnityEngine.Random.value);
                this.col = new Color(0.690f / 2f, 0.525f / 2f, 0.478f / 2f);
                if (UnityEngine.Random.value < 0.8f)
                {
                    this.depth = 0f;
                }
                else if (UnityEngine.Random.value < 0.3f)
                {
                    this.depth = -0.5f * UnityEngine.Random.value;
                }
                else
                {
                    this.depth = Mathf.Pow(UnityEngine.Random.value, 1.5f) * 3f;
                }
            }
            public bool InPlayLayer
            {
                get
                {
                    return this.depth == 0f;
                }
            }

            public override void Update(bool eu)
            {
                this.vel *= 0.99f;
                this.vel += new Vector2(0.11f, Custom.LerpMap(this.life, 0f, 0.5f, -0.1f, 0.05f));
                this.vel += this.dir * 0.8f;
                this.dir = (this.dir + Custom.RNV() * 0.6f).normalized;
                this.life -= 1f / this.lifeTime;
                this.lastLastPos = this.lastPos;
                this.lastPos = this.pos;
                this.pos += this.vel / (this.depth + 1f);
                if (this.InPlayLayer)
                {
                    if (this.room.GetTile(this.pos).Solid)
                    {
                        this.life -= 0.025f;
                        if (!this.room.GetTile(this.lastPos).Solid)
                        {
                            IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(this.room, this.room.GetTilePosition(this.lastPos), this.room.GetTilePosition(this.pos));
                            FloatRect floatRect = Custom.RectCollision(this.pos, this.lastPos, this.room.TileRect(intVector.Value).Grow(2f));
                            this.pos = floatRect.GetCorner(FloatRect.CornerLabel.D);
                            float num = 0.3f;
                            if (floatRect.GetCorner(FloatRect.CornerLabel.B).x < 0f)
                            {
                                this.vel.x = Mathf.Abs(this.vel.x) * num;
                            }
                            else if (floatRect.GetCorner(FloatRect.CornerLabel.B).x > 0f)
                            {
                                this.vel.x = -Mathf.Abs(this.vel.x) * num;
                            }
                            else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y < 0f)
                            {
                                this.vel.y = Mathf.Abs(this.vel.y) * num;
                            }
                            else if (floatRect.GetCorner(FloatRect.CornerLabel.B).y > 0f)
                            {
                                this.vel.y = -Mathf.Abs(this.vel.y) * num;
                            }
                        }
                        else
                        {
                            this.pos.y = this.room.MiddleOfTile(this.pos).y + 10f;
                        }
                    }
                    if (this.room.PointSubmerged(this.pos))
                    {
                        this.pos.y = this.room.FloatWaterLevel(this.pos.x);
                        this.life -= 0.025f;
                    }
                }
                if (this.life < 0f || (Custom.VectorRectDistance(this.pos, this.room.RoomRect) > 100f && !this.room.ViewedByAnyCamera(this.pos, 400f)))
                {
                    this.Destroy();
                }
                if (!this.room.BeingViewed)
                {
                    this.Destroy();
                }
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[1];
                sLeaser.sprites[0] = new FSprite("pixel", true);
                if (this.depth < 0f)
                {
                    sLeaser.sprites[0].scaleX = Custom.LerpMap(this.depth, 0f, -0.5f, 1.5f, 2f);
                }
                else if (this.depth > 0f)
                {
                    sLeaser.sprites[0].scaleX = Custom.LerpMap(this.depth, 0f, 5f, 1.5f, 0.1f);
                }
                else
                {
                    sLeaser.sprites[0].scaleX = 1.5f;
                }
                sLeaser.sprites[0].anchorY = 0f;
                if (this.depth > 0f)
                {
                    sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["CustomDepth"];
                    sLeaser.sprites[0].alpha = 0f;
                }
                this.AddToContainer(sLeaser, rCam, null!);
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                sLeaser.sprites[0].x = Mathf.Lerp(this.lastPos.x, this.pos.x, timeStacker) - camPos.x;
                sLeaser.sprites[0].y = Mathf.Lerp(this.lastPos.y, this.pos.y, timeStacker) - camPos.y;
                sLeaser.sprites[0].rotation = Custom.AimFromOneVectorToAnother(Vector2.Lerp(this.lastLastPos, this.lastPos, timeStacker), Vector2.Lerp(this.lastPos, this.pos, timeStacker));
                sLeaser.sprites[0].scaleY = Mathf.Max(2f, 2f + 1.1f * Vector2.Distance(Vector2.Lerp(this.lastLastPos, this.lastPos, timeStacker), Vector2.Lerp(this.lastPos, this.pos, timeStacker)));
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }

            public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                //this.col = palette.blackColor;
                if (this.depth <= 0f)
                {
                    sLeaser.sprites[0].color = this.col;
                }
                else
                {
                    sLeaser.sprites[0].color = Color.Lerp(palette.skyColor, this.col, Mathf.InverseLerp(0f, 5f, this.depth));
                }
            }

            public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                newContatiner = rCam.ReturnFContainer((!this.InPlayLayer) ? "Foreground" : "Items");
                sLeaser.sprites[0].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[0]);
            }

            private Vector2 dir;

            private Vector2 lastLastPos;

            public Color col;

            public float life;

            public float lifeTime;

            public float depth;
        }
    }

}
