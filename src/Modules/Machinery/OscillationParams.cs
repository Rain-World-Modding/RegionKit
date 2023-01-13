using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegionKit.Machinery;
public struct OscillationParams
{
	public readonly float amp;
	public readonly float frq;
	public readonly float phase;
	public Func<float, float> oscm => _oscm ?? UnityEngine.Mathf.Sin;
	private Func<float, float> _oscm;

	public OscillationParams(float amp, float frq, float phase, Func<float, float> oscm)
	{
		this.amp = amp;
		this.frq = frq;
		this.phase = phase;
		this._oscm = oscm;
	}

	public OscillationParams Deviate(in OscillationParams fluke)
	{
		return new OscillationParams(ClampedFloatDeviation(this.amp, fluke.amp),
			ClampedFloatDeviation(this.frq, fluke.frq),
			ClampedFloatDeviation(phase, fluke.phase), (UnityEngine.Random.value < 0.5) ? this.oscm : fluke.oscm);
	}
}
