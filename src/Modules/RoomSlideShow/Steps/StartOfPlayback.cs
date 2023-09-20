namespace RegionKit.Modules.Slideshow;

internal record StartOfPlayback(List<KeyFrame.Raw> keyFrames) : PlaybackStep
{
    
}