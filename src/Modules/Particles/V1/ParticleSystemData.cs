using static UnityEngine.Mathf;

namespace RegionKit.Modules.Particles.V1;
/// <summary>
/// base POM data for particle systems
/// </summary>
public abstract class ParticleSystemData : ManagedData
{
#pragma warning disable 1591
	[BooleanField("warmup", false, displayName: "Warmup on room load")]
	public bool doWarmup;
	[IntegerField("fadeIn", 0, 400, 80, ManagedFieldWithPanel.ControlType.text, displayName: "Fade-in frames")]
	public int fadeIn;
	[IntegerField("fadeInFluke", 0, 400, 0, ManagedFieldWithPanel.ControlType.text, displayName: "Fade-in fluke")]
	public int fadeInFluke;
	[IntegerField("fadeOut", 0, 400, 80, ManagedFieldWithPanel.ControlType.text, displayName: "Fade-out frames")]
	public int fadeOut;
	[IntegerField("fadeOutFluke", 0, 400, 0, ManagedFieldWithPanel.ControlType.text, displayName: "Fade-out fluke")]
	public int fadeOutFluke;
	[IntegerField("lt", 0, 15000, 80, ManagedFieldWithPanel.ControlType.text, displayName: "Lifetime")]
	public int lifeTime;
	[IntegerField("ltFluke", 0, 15000, 0, ManagedFieldWithPanel.ControlType.text, displayName: "Lifetime fluke")]
	public int lifeTimeFluke;
	//[Vector2Field("sdBase", 30f, 30f, label: "Direction")]
	[BackedByField("sdBase")]
	public Vector2 startDirBase;
	[FloatField("sdFluke", 0f, 180f, 0f, displayName: "Direction fluke (deg)")]
	public float startDirFluke;
	[FloatField("speed", 0f, 100f, 5f, control: ManagedFieldWithPanel.ControlType.text, displayName: "Speed")]
	public float startSpeed;
	[FloatField("speedFluke", 0f, 100f, 0f, control: ManagedFieldWithPanel.ControlType.text, displayName: "Speed fluke")]
	public float startSpeedFluke;

	[IntegerField("cdMin", 1, int.MaxValue, 40, ManagedFieldWithPanel.ControlType.text, displayName: "Min cooldown")]
	public int minCooldown;
	[IntegerField("cdMax", 1, int.MaxValue, 50, ManagedFieldWithPanel.ControlType.text, displayName: "Max cooldown")]
	public int maxCooldown;
#pragma warning restore 1591
	/// <summary>
	/// Recaches tile set if needed and returns it
	/// </summary>
	public List<IntVector2> ReturnSuitableTiles(Room rm)
	{
		//this right here is to update your changes live as you edit in devtools without recalculating tile set every request.
		if (AreaNeedsRefresh || _c_suitableTiles == null) { _c_suitableTiles = GetSuitableTiles(rm); }
		UpdateTilesetCacheValidity();
		return _c_suitableTiles;
	}
	/// <summary>
	/// Gets a list of tiles particles should be able to spawn on.
	/// </summary>
	/// <param name="rm"></param>
	/// <returns></returns>
	protected virtual List<IntVector2> GetSuitableTiles(Room rm)
	{
		return new List<IntVector2> { new((int)(owner.pos.x / 20), (int)(owner.pos.y / 20)) };
	}
	/// <summary>
	/// Override and use to add your checks for <see cref="AreaNeedsRefresh"/>.
	/// </summary>
	protected virtual void UpdateTilesetCacheValidity()
	{
		_c_ownerpos = owner.pos;
	}
	/// <summary>
	/// Cached suitable tiles
	/// </summary>
	protected List<IntVector2>? _c_suitableTiles;
	/// <summary>
	/// Returns true when settings have been changed and tile set needs re-generating again. See code: <see cref="ReturnSuitableTiles(Room)"/>
	/// </summary>
	protected virtual bool AreaNeedsRefresh => _c_ownerpos != owner.pos;
	/// <summary>
	/// cached position of placedobject
	/// </summary>
	protected Vector2 _c_ownerpos;
	///<inheritdoc/>
	public ParticleSystemData(PlacedObject owner, List<ManagedField>? additionalFields)
		: base(
			owner,
			additionalFields.AddRangeReturnSelf(new ManagedField[] { new Vector2Field("sdBase", new Vector2(30f, 30f), label: "Direction"), }).ToArray()
			)
	{
		//c_ST = GetSuitableTiles();
	}

	/// <summary>
	/// Returns fluke'd move params
	/// </summary>
	/// <returns></returns>
	public PMoveState DataForNew()
	{
		var res = new PMoveState
		{
			dir = LerpAngle(VecToDeg(startDirBase) - startDirFluke, VecToDeg(startDirBase) + startDirFluke, UnityEngine.Random.value),
			speed = Clamp(Lerp(startSpeed - startSpeedFluke, startSpeed + startSpeedFluke, UnityEngine.Random.value), 0f, float.MaxValue),
			fadeIn = ClampedIntDeviation(fadeIn, fadeInFluke, minRes: 0),
			fadeOut = ClampedIntDeviation(fadeOut, fadeOutFluke, minRes: 0),
			lifetime = ClampedIntDeviation(lifeTime, lifeTimeFluke, minRes: 0)
		};

		return res;
	}
}
