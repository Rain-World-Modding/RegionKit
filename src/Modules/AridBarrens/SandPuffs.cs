using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace RegionKit.AridBarrens
{
    public class SandPuffs : BackgroundScene, INotifyWhenRoomIsReady
    {
        public SandPuffs(RoomSettings.RoomEffect effect, Room room) : base(room)
        {
            this.effect = effect;
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            if (UnityEngine.Random.value < Custom.LerpMap(this.effect.amount, 0f, 1f, 0f, 0.7f))
            {
                for (int j = 0; j < this.room.physicalObjects.Length; j++)
                {
                    for (int k = 0; k < this.room.physicalObjects[j].Count; k++)
                    {
                        for (int l = 0; l < this.room.physicalObjects[j][k].bodyChunks.Length; l++)
                        {
                            if ((this.room.physicalObjects[j][k].bodyChunks[l].ContactPoint.x != 0 || this.room.physicalObjects[j][k].bodyChunks[l].ContactPoint.y != 0) && Mathf.Abs(this.room.physicalObjects[j][k].bodyChunks[l].lastPos.y - this.room.physicalObjects[j][k].bodyChunks[l].pos.y) > 5f)
                            {
                                this.room.AddObject(new SandPuff(this.room.physicalObjects[j][k].bodyChunks[l].pos + new Vector2(0f, -this.room.physicalObjects[j][k].bodyChunks[l].rad), Custom.LerpMap(this.room.physicalObjects[j][k].bodyChunks[l].lastPos.y - this.room.physicalObjects[j][k].bodyChunks[l].pos.y, 5f, 10f, 0.5f, 1f)));
                            }
                            else if ((this.room.physicalObjects[j][k].bodyChunks[l].ContactPoint.x != 0 || this.room.physicalObjects[j][k].bodyChunks[l].ContactPoint.y != 0) && Mathf.Abs(this.room.physicalObjects[j][k].bodyChunks[l].lastPos.x - this.room.physicalObjects[j][k].bodyChunks[l].pos.x) > 5f)
                            {
                                this.room.AddObject(new SandPuff(this.room.physicalObjects[j][k].bodyChunks[l].pos + new Vector2(0f, -this.room.physicalObjects[j][k].bodyChunks[l].rad), Custom.LerpMap(this.room.physicalObjects[j][k].bodyChunks[l].lastPos.y - this.room.physicalObjects[j][k].bodyChunks[l].pos.y, 5f, 10f, 0.5f, 1f)));
                            }
                        }
                    }
                }
            }
        }

        public void ShortcutsReady()
        {
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

        private float lastSin;

        public DisembodiedDynamicSoundLoop soundLoop;

        public DisembodiedDynamicSoundLoop soundLoop2;

        public class SandPuff : CosmeticSprite
        {
            // Token: 0x060009AD RID: 2477 RVA: 0x0005BFB4 File Offset: 0x0005A1B4
            public SandPuff(Vector2 pos, float size)
            {
                this.pos = pos;
                this.lastPos = pos;
                this.size = size;
                this.lastLife = 1f;
                this.life = 1f;
                this.lifeTime = Mathf.Lerp(40f, 120f, UnityEngine.Random.value) * Mathf.Lerp(0.5f, 1.5f, size);
            }

            // Token: 0x060009AE RID: 2478 RVA: 0x0005C020 File Offset: 0x0005A220
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
                sLeaser.sprites[0].scale = 10f * Mathf.Pow(1f - Mathf.Lerp(this.lastLife, this.life, timeStacker), 0.35f) * Mathf.Lerp(0.5f, 1.5f, this.size);
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


    }
}
