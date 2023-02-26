using static UnityEngine.Mathf;

namespace RegionKit.Modules.Particles.V2;

public class ParticleSystemData : ManagedData
{
	[EnumField<SpawnMode>("spawnMode", SpawnMode.Inside_Selected, null, ManagedFieldWithPanel.ControlType.arrows, "Spawn where")]
	public SpawnMode spawnMode;

	[StringField("groups", "0", "Selected groups")]
	public string groupTags = "0";

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
	[IntegerField("ctMin", 1, 99, 1, ManagedFieldWithPanel.ControlType.arrows, "Min spawn count")]
	public int minAmount;
	[IntegerField("ctMax", 1, 99, 1, ManagedFieldWithPanel.ControlType.arrows, "Max spawn count")]
	public int maxAmount;
	public ParticleSystemData(PlacedObject owner) : base(owner, null)
	{
	}
	public ParticleState StateForNew()
	{
		var res = new ParticleState
		{
			dir = LerpAngle(VecToDeg(sdBase) - startDirFluke, VecToDeg(sdBase) + startDirFluke, UnityEngine.Random.value),
			speed = Clamp(Lerp(startSpeed - startSpeedFluke, startSpeed + startSpeedFluke, UnityEngine.Random.value), 0f, float.MaxValue),
			fadeIn = ClampedIntDeviation(fadeIn, fadeInFluke, minRes: 0),
			fadeOut = ClampedIntDeviation(fadeOut, fadeOutFluke, minRes: 0),
			lifetime = ClampedIntDeviation(lifeTime, lifeTimeFluke, minRes: 0),
			stateChangeSlated = 1,
			age = 0,

		};

		return res;
	}

	public enum SpawnMode{
		Inside_Selected,
		Outside_Selected,
		Everywhere,
	}
}
