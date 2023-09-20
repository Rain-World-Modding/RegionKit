namespace RegionKit.Modules.RoomSlideShow;

internal record StartOfPlayback(List<KeyFrame.Raw> keyFrames) : PlaybackStep
{
    
}