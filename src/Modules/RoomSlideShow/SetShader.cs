namespace RegionKit.Modules.Slideshow;

internal sealed class SetShader : PlaybackStep
{
	public readonly string shader;

	public SetShader(string shader)
	{
		this.shader = shader;
	}
}
