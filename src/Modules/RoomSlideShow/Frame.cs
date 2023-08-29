namespace RegionKit.Modules.Slideshow;

internal sealed record Frame(int index, string elementName, int? ticksDuration, KeyFrame[] keyFramesHere) : PlaybackStep
{
	public Frame(
		int index,
		Raw raw)  : this (
			index,
			raw.elementName,
			raw.ticksDuration,
			raw.keyFramesHere.Select(kfRaw => new KeyFrame(index, kfRaw)).ToArray()) 
	{ }
	public int GetTicksDuration(int @default) => ticksDuration ?? @default;
	public override bool InstantlyProgress => false;
	internal sealed record Raw(string elementName, int? ticksDuration, KeyFrame.Raw[] keyFramesHere);
}
