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
			InterpolationKind.Quadratic => (from, to, amount) => Mathf.Lerp(from, to, amount * amount),
			_ => noInterpol
		};
	}
}

public delegate float Interpolator(float from, float to, float amount01);
