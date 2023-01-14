//by:thalber
//attempted on Devi's request
//suspended until bep transition complete

//by: thalber, ??? months later:
//uhhhgghg?

using static UnityEngine.Mathf;
using static RWCustom.Custom;
using static RegionKit.Modules.Objects._Module;

using GHalo = TempleGuardGraphics.Halo;
using URAnd = UnityEngine.Random;
using UDe = UnityEngine.Debug;

namespace RegionKit.Modules.Objects
{
    public class PlacedHalo : UpdatableAndDeletable, IDrawable
    {
        public PlacedHalo(PlacedObject owner, Room rm)
        {
            _ow = owner;
            //chal = this;
            if (cachedGuards.TryGet(rm, out var g))
            {
                halo = new GHalo(g, 0);
                reghalos.Set(halo, this);
            }
            else UDe.LogWarning("Cached guards not found!");
            
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
        }
        private readonly PlacedObject _ow;
        private PlacedHaloData phd => _ow.data as PlacedHaloData;
        private readonly GHalo halo;

        //do or omit? maybe creature proximity
        //zero for now
        private float tk;
        private float ltk;

        public float speed => Lerp(0.2f, 1.8f, halo.activity);
        //vanilla copypaste
        public float RadAtCircle(float circle, float timeStacker, float disruption)
        {
            return ((circle + 1f) * 20f + Lerp(halo.rad[0, 1], halo.rad[0, 0], timeStacker) * (1f - Lerp(ltk, tk, timeStacker))) * Lerp(Lerp(halo.rad[1, 1], halo.rad[1, 0], timeStacker), 0.7f, Lerp(ltk, tk, timeStacker)) * Lerp(1f, URAnd.value * disruption, Pow(disruption, 2f));
        }

        #region idrawable
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (halo == null) UDe.LogWarning("HALO IS NULL!");
            sLeaser.sprites = new FSprite[halo.totalSprites];
            halo.InitiateSprites(sLeaser, rCam);
            AddToContainer(sLeaser, rCam, null);
        }
        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            halo.DrawSprites(sLeaser, rCam, timeStacker, camPos, phd.headpos, phd.headdir / 10);
        }
        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {

        }
        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = newContatiner ?? rCam.ReturnFContainer(ContainerCodes.Foreground);
            foreach (var sp in sLeaser.sprites)
            {
                sp.RemoveFromContainer();
                newContatiner.AddChild(sp);
            }
        }
        #endregion idrawable
    }
}
