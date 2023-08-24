namespace RegionKit.Modules.Slideshow;

internal sealed class SetDelay : PlaybackStep
{
	public readonly int newDelay;

	public SetDelay(int newDelay)
	{
		this.newDelay = newDelay;
	}
}
