using RWCustom;
using UnityEngine;

// Made by Alduris
namespace RegionKit.Modules.Objects
{
    public class PlayerSensitiveLightSource : UpdatableAndDeletable, IDrawable
    {
        public Vector2 pos;
        public float rad;
        public float detectRad;
        public float fadeSpeed = 0.5f;
        public float minStrength;
        public float maxStrength;

        internal PlacedObject po = null!;

        internal int effectColor;
        protected float dist = float.MaxValue;
        protected float lastDist = float.MaxValue;
        protected float alpha;
        protected float lastAlpha;
        protected float waterSurfaceLevel = -1f;

        private bool _flat;
        private bool _shaderDirty = false;
        internal bool colorDirty = true;
        public virtual bool Flat
        {
            get => _flat;
            set
            {
                _flat = value;
                _shaderDirty = true;
            }
        }

        private Color _color;
        protected float colorAlpha;

        public PlayerSensitiveLightSource(PlacedObject pObj, Vector2 initPos, float initRad, float initDetRad, float minStrength, float maxStrength, float fadeSpeed, int effectColor)
        {
			po = pObj;
            pos = initPos;
            rad = initRad;
            detectRad = initDetRad;
            this.effectColor = effectColor;
            this.minStrength = minStrength;
            this.maxStrength = maxStrength;
            this.fadeSpeed = fadeSpeed;
            alpha = minStrength;
            lastAlpha = alpha;
        }

        public virtual Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                _color = value;
                colorAlpha = 0f;
                if (_color.r > colorAlpha)
                {
                    colorAlpha = _color.r;
                }
                if (_color.g > colorAlpha)
                {
                    colorAlpha = _color.g;
                }
                if (_color.b > colorAlpha)
                {
                    colorAlpha = _color.b;
                }
                if (colorAlpha == 0f)
                {
                    _color = Color.white;
                    return;
                }
                _color /= colorAlpha;
            }
        }

        public virtual float Lightness
        {
            get
            {
                if (colorAlpha == 0f || alpha == 0f)
                {
                    return 0f;
                }
                return (Color.r + Color.g + Color.b) / 3f * colorAlpha * alpha;
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            lastDist = dist;
            lastAlpha = alpha;

            if (room != null)
            {
                dist = float.MaxValue;
                foreach (var list in room.physicalObjects)
                {
                    foreach (var obj in list)
                    {
                        if (obj is Player)
                        {
                            dist = Mathf.Min(dist, Vector2.Distance((obj as Player)!.mainBodyChunk.pos, pos));
                        }
                    }
                }

                waterSurfaceLevel = room.FloatWaterLevel(pos);

                if (effectColor < -1 && room.game.cameras[0].room == room)
                {
                    Color = room.game.cameras[0].PixelColorAtCoordinate(pos);
                }
            }

            if (dist == float.MaxValue)
            {
                alpha = 0f;
            }
            else
            {
                bool withinThreshold = dist < detectRad;
                alpha = fadeSpeed == 0f ? withinThreshold ? 1f : 0f : Mathf.Max(0f, (detectRad - dist) / fadeSpeed / detectRad);
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[rCam.room.water ? 2 : 1];
            sLeaser.sprites[0] = new FSprite("Futile_White", true)
            {
                shader = rCam.room.game.rainWorld.Shaders[Flat ? "FlatLight" : "LightSource"],
                color = Color
            };
            if (rCam.room.water)
            {
                sLeaser.sprites[1] = new FSprite("Futile_White", true)
                {
                    shader = rCam.room.game.rainWorld.Shaders["UnderWaterLight"],
                    color = Color
                };
            }
            AddToContainer(sLeaser, rCam, null!);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (effectColor >= 0)
            {
                Color = palette.texture.GetPixel(30, 5 - effectColor * 2);
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            bool submersible = rCam.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.WaterLights) > 0f;
            rCam.ReturnFContainer(submersible ? "Water" : "ForegroundLights").AddChild(sLeaser.sprites[0]);
            if (sLeaser.sprites.Length > 1)
            {
                rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[1]);
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            const bool AFFECTED_BY_DARKNESS = false;
            float alphaFac = Mathf.Lerp(minStrength, maxStrength, Mathf.Lerp(lastAlpha, lastAlpha, timeStacker));
            float darkness = AFFECTED_BY_DARKNESS ? rCam.room.Darkness(pos) : 1f;

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].x = Mathf.Floor(pos.x - camPos.x) + 0.5f;
                sLeaser.sprites[i].color = Color;
                if (i == 0)
                {
                    // Normal glow sprite
                    sLeaser.sprites[i].y = Mathf.Floor(pos.y - camPos.y) + 0.5f;
                    sLeaser.sprites[i].scale = rad / 8f;
                    sLeaser.sprites[i].alpha = darkness * colorAlpha * alphaFac;
                }
                else
                {
                    // Water glow sprite
                    float yPos = pos.y;
                    float waterFade = Mathf.InverseLerp(waterSurfaceLevel - rad * 0.25f, waterSurfaceLevel + rad * 0.25f, yPos);
                    float waterFadeScale = rad * 0.5f * Mathf.Pow(1f - waterFade, 0.5f);
                    sLeaser.sprites[i].y = Mathf.Floor(Mathf.Min(yPos, Mathf.Lerp(yPos, waterSurfaceLevel - waterFadeScale * 0.5f, 0.5f)) - camPos.y) + 0.5f;
                    if (ModManager.MSC && rCam.room.waterInverted)
                    {
                        waterFade = 1f - Mathf.InverseLerp(waterSurfaceLevel - rad * 0.25f, waterSurfaceLevel + rad * 0.25f, yPos);
                        waterFadeScale = rad * 0.5f * Mathf.Pow(1f - waterFade, 0.5f);
                        sLeaser.sprites[i].y = Mathf.Floor(Mathf.Min(yPos, Mathf.Lerp(yPos, waterFadeScale - waterSurfaceLevel * 0.5f, 0.5f)) - camPos.y) + 0.5f;
                    }
                    sLeaser.sprites[i].scale = waterFadeScale / 8f;
                    sLeaser.sprites[i].alpha = darkness * Mathf.Pow(1f - waterFade, 0.5f) * colorAlpha * alphaFac;
                }
            }
            if (_shaderDirty)
            {
                sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders[Flat ? "FlatLight" : "LightSource"];
                _shaderDirty = false;
            }

            if (colorDirty)
            {
                ApplyPalette(sLeaser, rCam, rCam.currentPalette);
                colorDirty = false;
            }

            if (slatedForDeletetion)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }
    }
}
