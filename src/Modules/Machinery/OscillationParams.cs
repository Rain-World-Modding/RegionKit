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
	public readonly float baseValue;
	///<inheritdoc/>
	public readonly float amplitude;
	///<inheritdoc/>
	public readonly float frequency;
	///<inheritdoc/>
	public readonly float phase;
	///<inheritdoc/>
	public Func<float, float> Oscillator => _oscillator ?? UnityEngine.Mathf.Sin;
	private Func<float, float>? _oscillator;
	/// <summary>
	/// Primary constructor
	/// </summary>
	public OscillationParams(float baseValue, float amp, float frq, float phase, Func<float, float>? oscillator)
	{
		this.baseValue = baseValue;
		this.amplitude = amp;
		this.frequency = frq;
		this.phase = phase;
		this._oscillator = oscillator;
	}

	public float ValueAt(float x) => baseValue + amplitude * Oscillator(frequency * (x + phase));
	/// <summary>
	/// Returns a new instance that deviates each field from current  one's by random amounts
	/// </summary>
	public OscillationParams Deviate(in OscillationParams fluke)
	{
		return new OscillationParams(
			ClampedFloatDeviation(this.baseValue, fluke.baseValue),
			ClampedFloatDeviation(this.amplitude, fluke.amplitude),
			ClampedFloatDeviation(this.frequency, fluke.frequency),
			ClampedFloatDeviation(phase, fluke.phase), (UnityEngine.Random.value < 0.5) ? this.Oscillator : fluke.Oscillator);
	}
}
