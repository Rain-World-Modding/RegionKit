using System.Drawing.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace RegionKit.Modules.Objects;
/// <summary>
/// A light source that can be assigned any rgb value
/// </summary>
internal enum EnableConditions
{
	Always = 0,
	Before,
	After
}

public class ColouredLightSource : UpdatableAndDeletable
{
	private const int noiseUpdatePeriod = 2;
	private bool lightDisabled = false;
	private PlacedObject _localPlacedObject;
	private LightSource _lightSource;

	private ManagedData? _data;

	internal static readonly ManagedField[] __fields = {
		new ColorField("lightCol", Color.white, ManagedFieldWithPanel.ControlType.slider, "Light Colour"),
		new Vector2Field("radius", Vector2.up, Vector2Field.VectorReprType.circle),
		new FloatField("alphaChannel", 0f, 1f, 1f, displayName: "Alpha"),
		new BooleanField("flatLight", false, ManagedFieldWithPanel.ControlType.button, "Flat"),
		new FloatField("paletteDarkness", 0f, 1f, 0.5f, displayName: "Darkness Effect"),
		new FloatField("flickIntensity", 0f, 1f, 0f, displayName: "Flicker Intensity"),
		new FloatField("threshold", 0f, 1f, 0.5f, displayName: "Flicker Threshold"),
		new EnumField<EnableConditions>("rainConditions", EnableConditions.Always, displayName: "Enable Conditions"),
		new FloatField("enableThreshold", 0f, 1f, 0f, 0.05f, displayName: "Enable Time Threshold"),
		new IntegerField("fadeTime", 0, 9999, 100, ManagedFieldWithPanel.ControlType.text, displayName: "Fade Length")
	};
	float radius => _data!.GetValue<Vector2>("radius").magnitude;
	float alpha => _data!.GetValue<float>("alphaChannel");
	bool flatLight => _data!.GetValue<bool>("flatLight");
	Color lightCol => _data!.GetValue<Color>("lightCol");
	float paletteDarkness => _data!.GetValue<float>("paletteDarkness");
	float flickIntensity => _data!.GetValue<float>("flickIntensity");
	float flickerThreshold => _data!.GetValue<float>("threshold");
	EnableConditions rainConditions => _data!.GetValue<EnableConditions>("rainConditions");
	float enableThreshold => _data!.GetValue<float>("enableThreshold");
	int fadeTime => _data!.GetValue<int>("fadeTime");

	/// <summary>
	/// POM ctor
	/// </summary>
	public ColouredLightSource(PlacedObject placedObject, Room room)
	{
		this.room = room;
		_localPlacedObject = placedObject;
		_data = (placedObject.data as ManagedData)!;

		_lightSource = new LightSource(_localPlacedObject.pos, false, lightCol, this)
		{
			affectedByPaletteDarkness = paletteDarkness
		};
		room.AddObject(_lightSource);
	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);

		float instantAlpha = this.alpha;
		if (rainConditions != EnableConditions.Always)
		{
			instantAlpha *= EnableAlphaLerping();
		}
		_lightSource.color = lightCol;
		_lightSource.setRad = radius;
		_lightSource.setPos = _localPlacedObject.pos;
		_lightSource.flat = flatLight;
		_lightSource.affectedByPaletteDarkness = paletteDarkness;
		if (room.game.clock % noiseUpdatePeriod == 0)
		{
			float noiseIntensity = flickIntensity * RNG.value;
			lightDisabled = noiseIntensity > flickerThreshold;
		}
		_lightSource.setAlpha = lightDisabled ? 0f : instantAlpha;

	}

	private float EnableAlphaLerping()
	{
		float endTime = enableThreshold * room.game.world.rainCycle.cycleLength;
		float startTime = endTime - fadeTime;
		float timeLeft = room.game.world.rainCycle.timer;
		float fade = Mathf.InverseLerp(startTime, endTime, timeLeft);

		switch (rainConditions)
		{
		case EnableConditions.After:
			return fade;

		case EnableConditions.Before:
			return 1 - fade;

		default:
			return 1;
		}
	}
}
