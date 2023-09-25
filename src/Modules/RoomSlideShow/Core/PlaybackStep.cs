namespace RegionKit.Modules.RoomSlideShow;

internal abstract record PlaybackStep
{
	public virtual bool InstantlyProgress => true;
}

