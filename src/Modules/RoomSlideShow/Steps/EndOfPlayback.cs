namespace RegionKit.Modules.Slideshow;

internal record EndOfPlayback(bool loop, List<KeyFrame.Raw> finalKeyFrames) : PlaybackStep;