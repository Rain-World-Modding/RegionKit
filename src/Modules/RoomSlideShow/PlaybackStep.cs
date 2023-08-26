namespace RegionKit.Modules.Slideshow;

internal abstract record PlaybackStep
{
	public virtual bool InstantlyProgress => true;
}

