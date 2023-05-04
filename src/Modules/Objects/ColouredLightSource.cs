using System.Drawing.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace RegionKit.Modules.Objects;
/// <summary>
/// A light source that can be assigned any rgb value
/// </summary>

internal class ColoredLightSourceData : ManagedData
{
	private static ManagedField[] customFields = new ManagedField[]
	{
		new ColorField("lightCol", Color.white, displayName: "color"),
	};
	[BackedByField("lightCol")]
	public Color lightCol;
	[Vector2Field("radius", 0f, 0f, Vector2Field.VectorReprType.circle)]
	public Vector2 radius;
	[FloatField("alphaChannel", 0f, 1f, 1f, displayName: "Alpha")]
	public float alphaChannel;
	[BooleanField("flatLight", false, ManagedFieldWithPanel.ControlType.button, "Flat")]
	public bool flatLight;
	[FloatField("paletteDarkness", 0f, 1f, 0.5f, displayName: "Darkness Effect")]
	public float paletteDarkness;
	[FloatField("flickIntensity", 0f, 1f, 0f, displayName: "Flicker Intensity")]
	public float flickIntensity;
	[FloatField("threshold", 0f, 1f, 0.5f, displayName: "Flicker Threshold")]
	public float flickerThreshold;
	[EnumField<EnableConditions>("rainConditions", EnableConditions.Always)]
	public EnableConditions rainConditions;
	[FloatField("enableTreshold", 0f, 1f, 0f, 0.05f, displayName: "Enable time treshold")]
	public float enableThreshold;
	public ColoredLightSourceData(PlacedObject po) : base(po, customFields)
	{ }
	public ColoredLightSourceData(PlacedObject po, ColoredLightSourceLegacyData CLSLD) : base (po, customFields)
	{
		lightCol = CLSLD.lightCol;
		radius= CLSLD.radius;
		alphaChannel= CLSLD.alphaChannel;
		flatLight= CLSLD.flatLight;
		paletteDarkness= CLSLD.paletteDarkness;
		flickIntensity= CLSLD.flickIntensity;
		flickerThreshold= CLSLD.flickerThreshold;
		rainConditions = EnableConditions.Always;
		enableThreshold = 1f;
	}

	public override void FromString(string s)
	{
		string[] array = s.Split('~');
		panelPos = new Vector2(float.Parse(array[0]), float.Parse(array[1]));
		int datastart = 2;
		if (array.Length == 9) // this is legacy data parser
		{
			ManagedField[] fields= new ManagedField[]
			{
				new ColorField("lightCol", Color.white, ManagedFieldWithPanel.ControlType.slider, "Light Colour"),
				new Vector2Field("radius", Vector2.up, Vector2Field.VectorReprType.circle),
				new FloatField("alphaChannel", 0f, 1f, 1f, displayName: "Alpha"),
				new BooleanField("flatLight", false, ManagedFieldWithPanel.ControlType.button, "Flat"),
				new FloatField("paletteDarkness", 0f, 1f, 0.5f, displayName: "Darkness Effect"),
				new FloatField("flickIntensity", 0f, 1f, 0f, displayName: "Flicker Intensity"),
				new FloatField("threshold", 0f, 1f, 0.5f, displayName: "Flicker Threshold"),
			};
			for(int i = 0; i < fields.Length;i++)
			{
				try
				{
					object val = fields[i].FromString(array[datastart + i]);
					SetValue(fields[i].key, val);
				}
				catch (Exception)
				{
					__logger.LogError("Error parsing field "
						+ fields[i].key
						+ " from managed data type for "
						+ owner.type.ToString()
						+ "\nMaybe there's a version missmatch between the settings and the running version of the mod.");
				}
			}
			rainConditions = EnableConditions.Always;
			enableThreshold = 1f;
		}
		else
		{
			for (int i = 0; i < fields.Length; i++)
			{
				try
				{
					object val = fields[i].FromString(array[datastart + i]);
					SetValue(fields[i].key, val);
				}
				catch (Exception)
				{
					__logger.LogError("Error parsing field "
						+ fields[i].key
						+ " from managed data type for "
						+ owner.type.ToString()
						+ "\nMaybe there's a version missmatch between the settings and the running version of the mod.");
				}
			}
		}
		
	}
}
internal class ColoredLightSourceLegacyData : ManagedData
{
	private static ManagedField[] customFields = new ManagedField[]
	{
		new ColorField("lightCol", Color.white, displayName: "color"),
	};
	[BackedByField("lightCol")]
	public Color lightCol;
	[Vector2Field("radius", 0f, 0f, Vector2Field.VectorReprType.circle)]
	public Vector2 radius;
	[FloatField("alphaChannel", 0f, 1f, 1f, displayName: "Alpha")]
	public float alphaChannel;
	[BooleanField("flatLight", false, ManagedFieldWithPanel.ControlType.button, "Flat")]
	public bool flatLight;
	[FloatField("paletteDarkness", 0f, 1f, 0.5f, displayName: "Darkness Effect")]
	public float paletteDarkness;
	[FloatField("flickIntensity", 0f, 1f, 0f, displayName: "Flicker Intensity")]
	public float flickIntensity;
	[FloatField("threshold", 0f, 1f, 0.5f, displayName: "Flicker Threshold")]
	public float flickerThreshold;
	public ColoredLightSourceLegacyData(PlacedObject po) : base(po, customFields) { Debug.LogError("Legacy data constructor summonned"); }
}
internal enum EnableConditions
{
	Always = 0,
	Before,
	After
}

public class ColoredLightSource : UpdatableAndDeletable
{
	private const float percentageLerpTreshold = 0.05f;
	private const int noiseUpdatePeriod = 2;
	private bool lightDisabled = false;
	private PlacedObject _localPlacedObject;
	private LightSource _lightSource;
	private ColoredLightSourceData data;

	/// <summary>
	/// POM ctor
	/// </summary>
	public ColoredLightSource(PlacedObject placedObject, Room room)
	{
		if(placedObject.data is ColoredLightSourceData CLSD)
		{
			this.data = CLSD;
		}
		else if (placedObject.data is ColoredLightSourceLegacyData CLSLD) //One of these might be useless
		{
			this.data = new ColoredLightSourceData(placedObject, CLSLD);
		}
		else throw new ArgumentException("Invalid data type was provided");
		this.room = room;
		_localPlacedObject = placedObject;
		_lightSource = new LightSource(_localPlacedObject.pos, false, data.lightCol, this)
		{
			affectedByPaletteDarkness = data.paletteDarkness
		};
		room.AddObject(_lightSource);
	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);	
		
			float instantAlpha = data.alphaChannel;
			if (data.rainConditions != EnableConditions.Always)
			{
				float timeLeftPercentage = 1f-room.game.world.rainCycle.CycleProgression;
				if(data.rainConditions == EnableConditions.Before && timeLeftPercentage < data.enableThreshold) instantAlpha = 0f;
				if (data.rainConditions == EnableConditions.After && timeLeftPercentage >= data.enableThreshold) instantAlpha = 0f;
			}
			_lightSource.color = data.lightCol;
			_lightSource.setRad = data.radius.magnitude;
			_lightSource.setPos = _localPlacedObject.pos;
			_lightSource.flat = data.flatLight;
			_lightSource.affectedByPaletteDarkness = data.paletteDarkness;
			if (room.game.clock % noiseUpdatePeriod == 0)
			{
				float noiseIntensity = data.flickIntensity * RNG.value;
				lightDisabled = noiseIntensity > data.flickerThreshold;
			}
			_lightSource.setAlpha = lightDisabled ? 0f : instantAlpha;
		
	}

	private float EnableAlphaLerping()
	{
		float instantAlpha;
		//This block of code is responsible for smooth enabling of lights as the moment of enabling approaches
		//Is a lerp in a timeframe between treshold and in the zone of percentageLerpTreshold
		//This likely can be done a lot cleaner, probably with inverse lerp
		float cycleCompletionPercentage = room.game.world.rainCycle.CycleProgression;
		float zeroLightPercentageMoment;
		if (data.rainConditions == EnableConditions.Before)
		{
			zeroLightPercentageMoment = data.enableThreshold - percentageLerpTreshold;
			instantAlpha = Mathf.Lerp(0, data.alphaChannel,
				Mathf.Clamp(cycleCompletionPercentage, zeroLightPercentageMoment, data.enableThreshold)
				- zeroLightPercentageMoment);
		}
		else
		{
			zeroLightPercentageMoment = data.enableThreshold + percentageLerpTreshold;
			instantAlpha = Mathf.Lerp(data.alphaChannel, 0,
				Mathf.Clamp(cycleCompletionPercentage, data.enableThreshold, zeroLightPercentageMoment)
				- data.enableThreshold);
		}
		return instantAlpha;
	}
}
