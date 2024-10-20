using Random = UnityEngine.Random;

namespace RegionKit.Modules.Objects;

public class FanLightObject : UpdatableAndDeletable
{
	public class FanLightLight : LightSource
	{
		public int speed;
		public int inverseSpeed;
		public UpdatableAndDeletable? obj;
		public string? spriteName;

		public override string? ElementName => spriteName;

		public FanLightLight(Vector2 pos, Color col, UpdatableAndDeletable? attached) : base(pos, false, col, attached) 
		{
			obj = attached;
			if (attached is FanLightObject fan)
			{
				speed = fan.data.speed;
				inverseSpeed = fan.data.inverseSpeed;
				spriteName = fan.data.imageName;
				submersible = fan.data.submersible;
			}
		}

        public override void Update(bool eu)
        {
            base.Update(eu);
			if (obj is FanLightObject fan)
			{
				if (speed != fan.data.speed) 
					speed = fan.data.speed;
				if (inverseSpeed != fan.data.inverseSpeed) 
					inverseSpeed = fan.data.inverseSpeed;
				submersible = fan.data.submersible;
			}
		}
    }

	public PlacedObject pObj;
	public FanLightData data;
	public FanLightLight? lightSource;
	public LightSource flatLightSource;
	public int flickerWait;
	public int flicker;
	public float sin;
	public float switchOn;
	public bool gravityDependent;
	public bool powered;
	public readonly float[] alphas = new float[2];

    public virtual float NoElectricity => room is null ? 0f : 1f - room.ElectricPower;

	public FanLightObject(Room placedInRoom, PlacedObject placedObject, FanLightData lightData)
	{
		pObj = placedObject;
		data = lightData;
		sin = Random.value;
		flickerWait = Random.Range(0, 700);
		placedInRoom.AddObject(lightSource = new(placedObject.pos, lightData.Color, this));
		placedInRoom.AddObject(flatLightSource = new(placedObject.pos, false, lightData.Color2, this) { flat = true });
		gravityDependent = placedInRoom.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.BrokenZeroG) > 0f && lightData.randomSeed > 0f;
		powered = NoElectricity > .5f || !gravityDependent;
		switchOn = lightData.randomSeed / 100f;
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
        if (lightSource is null)
		{
			lightSource = new(pObj.pos, data.Color, this);
			room?.AddObject(lightSource);
		}
        if (lightSource.color != data.Color)
			lightSource.color = data.Color;
		if (flatLightSource.color != data.Color2)
			flatLightSource.color = data.Color2;
		if (gravityDependent)
		{
			if (!powered)
			{
				lightSource.setAlpha = 0f;
				flatLightSource.setAlpha = 0f;
                if (lightSource is LightSource sr && FanLightHooks.data.TryGetValue(sr, out var da))
                    da[0] = 0f;
                if (flatLightSource is LightSource sr2 && FanLightHooks.data.TryGetValue(sr2, out var da2))
                    da2[0] = 0f;
                if (!(NoElectricity > Mathf.Lerp(.65f, .95f, switchOn)) || !(Random.value < 1f / Mathf.Lerp(20f, 80f, switchOn)))
					return;
				powered = true;
				flicker = Random.Range(1, 15);
				room?.PlaySound(SoundID.Red_Light_On, pObj.pos, 1f, 1f);
			}
			else if (NoElectricity < .6f && Random.value < .05f)
				powered = false;
		}
		var num = !gravityDependent ? 1f : NoElectricity;
		flickerWait--;
		if (lightSource.setRad != data.Rad / 20f) 
			lightSource.setRad = data.Rad / 20f;
		if (flickerWait < 1)
		{
			flickerWait = Random.Range(0, 700);
			flicker = Random.Range(1, 15);
		}
		if (flicker > 0)
		{
			flicker--;
			if (Random.value < 1f / 3f)
			{
				float num2 = Mathf.Pow(Random.value, .5f), a = num2 * num, a2 = a * .25f;
                lightSource.setAlpha = a;
				flatLightSource.setAlpha = a2;
            }
		}
		else
		{
			float a = Mathf.Lerp(.9f, 1f, .5f + Mathf.Sin(sin * Mathf.PI * 2f) * .5f * Random.value) * num, a2 = .25f * num;
			lightSource.setAlpha = a;
			flatLightSource.setAlpha = a2;
        }
		lightSource.setPos = pObj.pos;
		flatLightSource.setPos = pObj.pos;
        if (lightSource is LightSource s && FanLightHooks.data.TryGetValue(s, out var d) && s.setAlpha is float f)
            d[0] = f;
        if (flatLightSource is LightSource s2 && FanLightHooks.data.TryGetValue(s2, out var d2) && s2.setAlpha is float f2)
            d2[0] = f2;
    }
}
