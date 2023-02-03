using UnityEngine;

namespace RegionKit.Modules.Objects;
/// <summary>
/// A light source that can be assigned any rgb value
/// </summary>
public class ColouredLightSource : UpdatableAndDeletable
{
	private PlacedObject _localPlacedObject;
	private LightSource _lightSource;
	private ManagedData? _data;
	private bool _flickering;

	internal static readonly ManagedField[] __fields = {
		new ColorField("lightCol", Color.white, ManagedFieldWithPanel.ControlType.slider, "Light Colour"),
		new Vector2Field("radius", Vector2.up, Vector2Field.VectorReprType.circle),
		new FloatField("alphaChannel", 0f, 1f, 1f, displayName: "Alpha"),
		new BooleanField("flatLight", false, ManagedFieldWithPanel.ControlType.button, "Flat"),
		new FloatField("paletteDarkness", 0f, 1f, 0.5f, displayName: "Darkness Effect"),
		new FloatField("flickIntensity", 0f, 1f, 0f, displayName: "Flicker Intensity"),
		new FloatField("threshold", 0f, 1f, 0.5f, displayName: "Flicker Threshold")
	};
	/// <summary>
	/// POM ctor
	/// </summary>
	public ColouredLightSource(PlacedObject pObj, Room room)
	{
		//Data = new(pObj, null);
		this.room = room;
		_localPlacedObject = pObj;
		_data = (pObj.data as ManagedData)!;

		_lightSource = new LightSource(_localPlacedObject.pos, false, _data?.GetValue<Color>("lightCol") ?? Color.white, this);
		_lightSource.affectedByPaletteDarkness = _data?.GetValue<float>("paletteDarkness") ?? 0.5f;
		room.AddObject(_lightSource);
	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);

		float rad = _data!.GetValue<Vector2>("radius").magnitude;
		float alpha = _data.GetValue<float>("alphaChannel");
		bool flat = _data.GetValue<bool>("flatLight");
		Color col = _data.GetValue<Color>("lightCol");
		float darknessEffect = _data.GetValue<float>("paletteDarkness");

		if (!_flickering) _lightSource.setAlpha = alpha;
		_lightSource.color = col;
		_lightSource.setRad = rad;
		_lightSource.setPos = _localPlacedObject.pos;
		_lightSource.flat = flat;
		_lightSource.affectedByPaletteDarkness = darknessEffect;

		if (room.game.clock % 2 != 0) return;
		float noiseIntensity = _data.GetValue<float>("flickIntensity") * OneDimensionalPerlinNoise();
		if (noiseIntensity > _data.GetValue<float>("threshold"))
		{
			_lightSource.setAlpha = 0f;
			_flickering = true;
		}
		else
		{
			_lightSource.setAlpha = alpha;
			_flickering = false;
		}
	}

	//Based off of a wind direction algorithm online. If you know a better noise function, entertain me
	private float OneDimensionalPerlinNoise()
	{
		var aux = RNG.value * Mathf.PI * 2f;
		var vectorX = Mathf.Cos(aux);
		var vectorY = Mathf.Sin(aux);

		float noiseValue = Mathf.Clamp01(Mathf.PerlinNoise(vectorX * Time.time, vectorY * Time.time));
		return noiseValue;
	}
}
