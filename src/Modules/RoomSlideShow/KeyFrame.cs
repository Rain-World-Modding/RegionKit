namespace RegionKit.Modules.Slideshow;

internal struct KeyFrame
{
	public readonly int atFrame;
	public readonly Channel channel;
	public float value;

	public KeyFrame(int atFrame, Channel channel, float value)
	{
		this.atFrame = atFrame;
		this.channel = channel;
		this.value = value;
	}
}
