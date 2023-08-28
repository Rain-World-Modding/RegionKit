namespace RegionKit.Modules.Slideshow;

internal sealed record SetInterpolation(Interpolator interpolator, InterpolationKind value, Channel[] channels) : PlaybackStep
{
	// public readonly Interpolator interpolator;
	// public readonly InterpolationKind value;
	// public readonly Channel[] channels;

	public SetInterpolation(
		InterpolationKind value,
		Channel[] channels) : this(
			CreateInterpolator(value),
			value,
			channels)
	{
		// this.value = value;
		// this.channels = channels;
		// Interpolator noInterpol = (from, to, amount) => from;
		// interpolator = value switch
		// {
		// 	InterpolationKind.No => noInterpol,
		// 	InterpolationKind.Linear => Mathf.Lerp,
		// 	InterpolationKind.Quadratic => ApplyEaser(Mathf.Lerp, (x) => x * x),
		// 	_ => noInterpol
		// };
	}

	private static Interpolator CreateInterpolator(InterpolationKind value)
	{
		return value switch
		{
			InterpolationKind.No => FlatSwitch,
			InterpolationKind.Linear => Mathf.Lerp,
			InterpolationKind.Quadratic => ApplyEaser(Mathf.Lerp, (x) => x * x),
			_ => throw new ArgumentException($"Value contains invalid setting! {value}({(int)value})")
		};
	}

	public static Interpolator ApplyEaser(Interpolator interpolator, Easer easer)
	{
		if (easer is null) throw new ArgumentNullException("easer");
		if (interpolator is null) throw new ArgumentNullException("interpolator");
		interpolator = (from, to, x) =>
		{
			return interpolator(from, to, easer(x));
		};
		return interpolator;
	}
	public static float FlatSwitch(float from, float to, float x) => from;
}
