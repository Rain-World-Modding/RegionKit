namespace RegionKit.Modules.RoomSlideShow;

internal record EndOfPlayback(bool loop, List<KeyFrame.Raw> finalKeyFrames) : PlaybackStep;