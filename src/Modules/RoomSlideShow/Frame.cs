namespace RegionKit.Modules.Slideshow;

internal sealed record Frame(int index, string elementName, int? ticksDuration, KeyFrame[] keyFramesHere) : PlaybackStep
{
	// public readonly string elementName;
	// public readonly int index;
	// public readonly int? ticksDuration;
	// public readonly KeyFrame[] keyFramesHere;

	// public Frame(int index, string elementName, int? ticksDuration, KeyFrame[] keyFramesHere)
	// {
	// 	this.elementName = elementName;
	// 	this.index = index;
	// 	this.ticksDuration = ticksDuration;
	// 	this.keyFramesHere = keyFramesHere;
	// }
	public int GetTicksDuration(int @default) => ticksDuration ?? @default;
	public override bool InstantlyProgress => false;
}
