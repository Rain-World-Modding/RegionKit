namespace RegionKit.Modules.Slideshow;

internal sealed record SetDelay(int newDelay) : PlaybackStep
{
	// public readonly int newDelay;

	// public SetDelay(int newDelay)
	// {
	// 	this.newDelay = newDelay;
	// }
}
