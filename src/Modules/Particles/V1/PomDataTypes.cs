using static RegionKit.Utils;
using static UnityEngine.Mathf;

namespace RegionKit.Modules.Particles.V1;

#region spawners
public abstract class ParticleSystemData : ManagedData
{
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
	[Vector2Field("sdBase", 30f, 30f, label: "Direction")]
	public Vector2 sdBase;
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
	//cached tileset
	protected List<IntVector2>? _c_suitableTiles;
	/// <summary>
	/// Returns true when settings have been changed and tile set needs re-generating again. See code: <see cref="ReturnSuitableTiles(Room)"/>
	/// </summary>
	protected virtual bool AreaNeedsRefresh => _c_ownerpos != owner.pos;
	protected Vector2 _c_ownerpos;

	public ParticleSystemData(PlacedObject owner, List<ManagedField>? additionalFields)
		: base(owner, null)
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
			dir = LerpAngle(VecToDeg(sdBase) - startDirFluke, VecToDeg(sdBase) + startDirFluke, UnityEngine.Random.value),
			speed = Clamp(Lerp(startSpeed - startSpeedFluke, startSpeed + startSpeedFluke, UnityEngine.Random.value), 0f, float.MaxValue),
			fadeIn = ClampedIntDeviation(fadeIn, fadeInFluke, minRes: 0),
			fadeOut = ClampedIntDeviation(fadeOut, fadeOutFluke, minRes: 0),
			lifetime = ClampedIntDeviation(lifeTime, lifeTimeFluke, minRes: 0)
		};

		return res;
	}
}
/// <summary>
/// SpawnerData for tossing in particles in a rectangular area
/// </summary>
public class RectParticleSpawnerData : ParticleSystemData
{
	Vector2 RectBounds => GetValue<Vector2>("effRect");

	public RectParticleSpawnerData(PlacedObject owner) : base(owner, new List<ManagedField>
	{
		new Vector2Field("effRect", new Vector2(40f, 40f), Vector2Field.VectorReprType.rect)
	})
	{

	}
	//cached second point for areaNeedsRefresh
	private Vector2 _c_rectBounds;
	protected override bool AreaNeedsRefresh => base.AreaNeedsRefresh && _c_rectBounds == RectBounds;
	protected override void UpdateTilesetCacheValidity()
	{
		base.UpdateTilesetCacheValidity();
		_c_rectBounds = RectBounds;
	}
	protected override List<IntVector2> GetSuitableTiles(Room rm)
	{
		//c_RB = RectBounds;
		var res = new List<IntVector2>();
		IntVector2 orpos = (owner.pos / 20f).ToIntVector2();
		IntVector2 bounds = ((owner.pos + RectBounds) / 20f).ToIntVector2();
		IntRect area = IntRect.MakeFromIntVector2(orpos);
		area.ExpandToInclude(bounds);
		for (int x = area.left; x < area.right; x++)
		{
			for (int y = area.bottom; y < area.top; y++)
			{
				res.Add(new IntVector2(x, y));
			}
		}
		//PetrifiedWood.WriteLine(Json.Serialize(res));
		return res;
	}
}
public class OffscreenSpawnerData : ParticleSystemData
{
	[IntegerField("margin", 0, 30, 1)]
	public int margin;
	[BooleanField("nosolid", true, displayName: "Skip solid tiles")]
	public bool AirOnly;
	public OffscreenSpawnerData(PlacedObject owner) : base(owner, new List<ManagedField>())
	{
	}

	private Vector2 _c_dir;
	protected override void UpdateTilesetCacheValidity()
	{
		base.UpdateTilesetCacheValidity();
		_c_dir = base.GetValue<Vector2>("sdBase");
	}
	protected override bool AreaNeedsRefresh => base.AreaNeedsRefresh && _c_dir == base.GetValue<Vector2>("sdBase");
	protected override List<IntVector2> GetSuitableTiles(Room rm)
	{
		var res = new List<IntVector2>();
		var rb = new IntRect(0 - margin, 0 - margin, rm.Width + margin, rm.Height + margin);
		var dropVector = GetValue<Vector2>("sdBase");
		//var row = new List<IntVector2>();
		//var column = new List<IntVector2>();
		int ys = (dropVector.y > 0) ? rb.bottom : rb.top;
		int xs = (dropVector.x > 0) ? rb.left : rb.right;
		for (int x = rb.left; x < rb.right; x++)
		{
			var r = new IntVector2(x, ys);
			if (!rm.GetTile(r).Solid || !AirOnly) res.Add(r);
		}
		for (int y = rb.bottom; y < rb.top; y++)
		{
			var r = new IntVector2(xs, y);
			if (!rm.GetTile(r).Solid || !AirOnly) res.Add(r);
		}
		return res;
	}
}
public class WholeScreenSpawnerData : ParticleSystemData
{
	[IntegerField("Margin", 0, 30, 1, displayName: "Margin")]
	public int Margin;
	[BooleanField("nosolid", true, displayName: "Skip solid tiles")]
	public bool AirOnly;

	public WholeScreenSpawnerData(PlacedObject owner) : base(owner, null)
	{

	}
	protected override List<IntVector2> GetSuitableTiles(Room rm)
	{
		var r = ConstructIR(new IntVector2(-Margin, -Margin), new IntVector2(rm.TileWidth + Margin, rm.TileHeight + Margin))
			.ReturnTiles()
			.ToList();
		if (AirOnly)
		{
			for (int i = r.Count() - 1; i > -1; i--)
			{
				if (rm.GetTile(r[i]).Solid) r.RemoveAt(i);
			}
		}
		return r;
	}
}
#endregion

public class ParticleVisualCustomizer : ManagedData
{
	[ColorField("sColBase", 1f, 1f, 1f, 1f, DisplayName: "Sprite color base")]
	public Color spriteColor;
	[ColorField("sColFluke", 0f, 0f, 0f, 0f, DisplayName: "Sprite color fluke")]
	public Color spriteColorFluke;
	[ColorField("lColBase", 1f, 1f, 1f, 1f, DisplayName: "Light color base")]
	public Color lightColor;
	[ColorField("lColFluke", 0f, 0f, 0f, 0f, DisplayName: "Light color fluke")]
	public Color lightColorFluke;
	[BooleanField("flat", false, displayName: "Flat light")]
	public bool flatLight;
	[FloatField("lrminBase", 0f, 400f, 20f, displayName: "Light radius min")]
	public float lightRadMin = 20f;
	[FloatField("lrminFluke", 0f, 400f, 0f, displayName: "Lightradmin fluke")]
	public float lightRadMinFluke = 0f;
	[FloatField("lrmaxBase", 0f, 400f, 30f, displayName: "Light radius max")]
	public float lightRadMax = 30f;
	[FloatField("lrmaxFluke", 0f, 400f, 0f, displayName: "Lightradmax fluke")]
	public float lightRadMaxFluke = 0f;
	[FloatField("lIntBase", 0f, 1f, 1f, displayName: "Light intensity")]
	public float LightIntensity = 1f;
	[FloatField("lIntFluke", 0f, 1f, 0f, displayName: "Light intensity fluke")]
	public float LightIntensityFluke = 0f;
	[StringField("eName", "SkyDandelion", displayName: "Atlas element")]
	public string elmName = "SkyDandelion";
	[StringField("shader", "Basic", displayName: "Shader")]
	public string shader = "Basic";
	[Vector2Field("p2", 40f, 0f)]
	public Vector2 p2;
	[EnumField<ContainerCodes>("cc", ContainerCodes.Foreground)]
	public ContainerCodes containerCode;
	[FloatField("z_scalemin", 0.1f, 2f, 1f, 0.05f, ManagedFieldWithPanel.ControlType.slider, displayName: "scale min")]
	public float scalemin = 1f;
	[FloatField("z_scalemax", 0.1f, 2f, 1f, 0.05f, ManagedFieldWithPanel.ControlType.slider, displayName: "scale max")]
	public float scalemax = 1f;

	public ParticleVisualCustomizer(PlacedObject owner) : base(owner, null)
	{

	}

	public PVisualState DataForNew()
	{
		var res = new PVisualState(
			elmName,
			shader,
			containerCode,
			spriteColor.Deviation(spriteColorFluke),
			lightColor.Deviation(lightColorFluke),
			ClampedFloatDeviation(LightIntensity, LightIntensityFluke, minRes: 0f),
			ClampedFloatDeviation(lightRadMax, lightRadMaxFluke, minRes: 0f),
			ClampedFloatDeviation(lightRadMin, lightRadMinFluke, minRes: 0f),
			0f,
			flatLight,
			Lerp(scalemin, scalemax, UnityEngine.Random.value))
		{
			//sCol = spriteColor.Deviation(spriteColorFluke),
			//lCol = lightColor.Deviation(lightColorFluke),
			//lRadMin = ClampedFloatDeviation(lightRadMin, lightRadMinFluke, minRes: 0f),
			//lRadMax = ClampedFloatDeviation(lightRadMax, lightRadMaxFluke, minRes: 0f),
			//lInt = ClampedFloatDeviation(LightIntensity, LightIntensityFluke, minRes: 0f),
			//aElm = elmName,
			//shader = shader,
			//container = cc,
			//flat = flatLight,
			//scale = Lerp(scalemin, scalemax, UnityEngine.Random.value),
		};
		res.sCol.ClampToNormal();
		res.lCol.ClampToNormal();
		return res;
	}
}
