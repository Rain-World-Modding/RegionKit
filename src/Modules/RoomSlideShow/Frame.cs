namespace RegionKit.Modules.Slideshow;

internal sealed class Frame : PlaybackStep
{
	public readonly string elementName;
	public readonly int startsAt;
	public readonly int framesDuration;
	public readonly KeyFrame[] keyFramesHere;

	public Frame(string elementName, int startsAt, int framesDuration, KeyFrame[] keyFramesHere)
	{
		this.elementName = elementName;
		this.startsAt = startsAt;
		this.framesDuration = framesDuration;
		this.keyFramesHere = keyFramesHere;
	}

	public override bool InstantlyProgress => false;
}
