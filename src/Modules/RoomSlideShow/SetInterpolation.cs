namespace RegionKit.Modules.Slideshow;

internal sealed class SetInterpolation : PlaybackStep
{
	public readonly Interpolator interpolator;
	public readonly InterpolationKind value;
	public readonly Channel[] channels;

	public SetInterpolation(InterpolationKind value, Channel[] channels)
	{
		this.value = value;
		this.channels = channels;
		Interpolator noInterpol = (from, to, amount) => from;
		interpolator = value switch
		{
			InterpolationKind.No => noInterpol,
			InterpolationKind.Linear => Mathf.Lerp,
			InterpolationKind.Quadratic => ApplyEaser(Mathf.Lerp, (x) => x * x),
			_ => noInterpol
		};
	}
	public static Interpolator ApplyEaser(Interpolator interpolator, Easer easer) {
		if (easer is null) throw new ArgumentNullException("easer");
		if (interpolator is null) throw new ArgumentNullException("interpolator");
		interpolator = (from, to, x) => {
			return interpolator(from, to, easer(x));
		};
		return interpolator;
	}
}
