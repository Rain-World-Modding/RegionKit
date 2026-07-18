using System.Runtime.CompilerServices;

namespace RegionKit.Modules.Insects;

/// <summary>
/// By LB/M4rbleL1ne
/// Colored camo beetle insect
/// New insect that can camo and uses one of the effect colors when stressed
/// </summary>
public class ColoredCamoBeetleInsect : Beetle
{
	private float _lerper;
	private bool _lerpUp = true;
	internal const float ADD = .075f;
	private readonly int _effectColor;
	private int _middleSleepCtr;

	private bool CanMiddleSleep
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _middleSleepCtr > 0 && stressed < .6f && room?.GetTile(pos).wallbehind is true;
	}

	/// <summary>
	/// Insect ctor
	/// </summary>
	/// <param name="room"></param>
	/// <param name="pos"></param>
    public ColoredCamoBeetleInsect(Room room, Vector2 pos) : base(room, pos)
    {
        type = _Enums.ColoredCamoBeetle;
        _effectColor = UnityEngine.Random.Range(0, 2);
    }

	/// <summary>
	/// Update
	/// </summary>
	/// <param name="eu"></param>
    public override void Update(bool eu)
    {
        if (_lerper <= -.25f)
            _lerpUp = true;
        else if (_lerper >= 1f)
            _lerpUp = false;
        if (_lerpUp)
            _lerper += ADD;
        else
            _lerper -= ADD;
        if (!sitPos.HasValue && CanMiddleSleep)
            vel.y += .8f;
        base.Update(eu);
    }

	/// <summary>
	/// Act
	/// </summary>
    public override void Act()
    {
        base.Act();
        if (UnityEngine.Random.value < .05f && _middleSleepCtr == 0)
            _middleSleepCtr = 200;
        if (CanMiddleSleep)
        {
            vel *= 0f;
            wingsOut = 0f;
            _middleSleepCtr--;
        }
    }

	/// <summary>
	/// Initiate sprites
	/// </summary>
	/// <param name="sLeaser"></param>
	/// <param name="rCam"></param>
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        base.InitiateSprites(sLeaser, rCam);
        AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("GrabShaders"));
    }

	/// <summary>
	/// Draw sprites
	/// </summary>
	/// <param name="sLeaser"></param>
	/// <param name="rCam"></param>
	/// <param name="timeStacker"></param>
	/// <param name="camPos"></param>
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		FSprite[] sprites = sLeaser.sprites;
		sprites[0].scaleX *= 1.4f;
        sprites[0].scaleY *= 1.45f;
        Color eCol = rCam.currentPalette.texture.GetPixel(30, 5 - _effectColor * 2), bCol = rCam.currentPalette.blackColor;
        for (var i = 0; i < sprites.Length; i++)
        {
            sprites[i].color = Color.Lerp(bCol, eCol, _lerper);
            sprites[i].alpha = (stressed - .4f) * 2f;
        }
    }
}
