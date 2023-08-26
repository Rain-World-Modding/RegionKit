namespace RegionKit.Modules.Slideshow;

internal record KeyFrame(int atFrame, Channel channel, float value)
{
	public KeyFrame(int atFrame, Raw raw) : this(atFrame, raw.channel, raw.value) {
		
	}
	// public int atFrame;
	// public Channel channel;
	// public float value;

	// public KeyFrame(int atFrame, Channel channel, float value)
	// {
	// 	this.atFrame = atFrame;
	// 	this.channel = channel;
	// 	this.value = value;
	// }
	// public override string ToString()
	// {
	// 	return $"KeyFrame{{ atFrame:{atFrame}, channel:{channel}({(int)channel}), value: {value} }}";
	// }
	internal record Raw(Channel channel, float value) {

	}
}
