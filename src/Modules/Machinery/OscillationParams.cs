using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegionKit.Modules.Machinery;
/// <summary>
/// Movement parameters for oscillating machinery
/// </summary>
public struct OscillationParams
{
	///<inheritdoc/>
	public readonly float amplitude;
	///<inheritdoc/>
	public readonly float frequency;
	///<inheritdoc/>
	public readonly float phase;
	///<inheritdoc/>
	public Func<float, float> oscillationMode => _oscm ?? UnityEngine.Mathf.Sin;
	private Func<float, float> _oscm;
	/// <summary>
	/// Primary constructor
	/// </summary>
	public OscillationParams(float amp, float frq, float phase, Func<float, float> oscm)
	{
		this.amplitude = amp;
		this.frequency = frq;
		this.phase = phase;
		this._oscm = oscm;
	}
	/// <summary>
	/// Returns a new instance that deviates each field from current  one's by random amounts
	/// </summary>
	public OscillationParams Deviate(in OscillationParams fluke)
	{
		return new OscillationParams(ClampedFloatDeviation(this.amplitude, fluke.amplitude),
			ClampedFloatDeviation(this.frequency, fluke.frequency),
			ClampedFloatDeviation(phase, fluke.phase), (UnityEngine.Random.value < 0.5) ? this.oscillationMode : fluke.oscillationMode);
	}
}
