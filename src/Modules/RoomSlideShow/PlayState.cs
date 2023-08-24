namespace RegionKit.Modules.Slideshow;

internal sealed class PlayState
{
	private const int MAX_INSTANT_INSTRUCTIONS = 1024;
	//private readonly Dictionary<Channel, KeyFrame> startKeyFrames;
	//private readonly Dictionary<Channel, KeyFrame> endKeyFrames;
	private readonly Dictionary<Channel, KeyFrame> lastKeyFrames = new();
	private readonly Dictionary<Channel, KeyFrame> upcomingKeyFrames = new();
	private readonly Dictionary<Channel, SetInterpolation> interpolationSettings = new();
	public PlaybackStep CurrentStep => this.owner.playbackSteps[this.CurrentIndex];
	public Frame CurrentFrame => (Frame)this.CurrentStep;
	public int TicksInCurrentFrame => TicksSinceStart - CurrentFrame.startsAt;
	public int CurrentIndex { get; private set; } = 0;
	public int TicksSinceStart { get; private set; } = 0;
	public string Shader { get; private set; } = "Basic";
	public int DefaultFrameDelay { get; private set; } = 40;
	public readonly Playback owner;

	public PlayState(Playback owner)
	{
		// this.startKeyFrames = new() {
		// 	{ Channel.X, new(0, Channel.X, 0f)},
		// 	{ Channel.Y, new(0, Channel.Y, 0f)},
		// 	{ Channel.R, new(0, Channel.R, 1f)},
		// 	{ Channel.G, new(0, Channel.G, 1f)},
		// 	{ Channel.B, new(0, Channel.B, 1f)},
		// 	{ Channel.A, new(0, Channel.A, 1f)},
		// };
		this.owner = owner;
	}

	public void Update()
	{
		bool overstayed = true;
		for (int i = 0; i < MAX_INSTANT_INSTRUCTIONS; i++)
		{
			bool keepCycling = CurrentStep.InstantlyProgress;
			switch (CurrentStep)
			{
			case SetDelay setDelay:
				DefaultFrameDelay = setDelay.newDelay;
				break;

			case SetInterpolation setSmooth:
				foreach (var channel in setSmooth.channels)
				{
					interpolationSettings[channel] = setSmooth;
				}
				break;
			case SetShader setShader:
				Shader = setShader.shader;
				break;
			case Frame frame:
				if (TicksSinceStart > frame.startsAt + frame.framesDuration)
				{
					keepCycling = true;
				}
				break;
			}
			if (!keepCycling)
			{
				overstayed = false;
				break;
			}
			CurrentIndex++;
		}
		if (overstayed)
		{
			throw new NoActualFramesException();
		}
		TicksSinceStart++;
	}
	public SetInterpolation GetInterpolationSetting(Channel channel) => interpolationSettings[channel];
	public KeyFrame GetLastKeyFrame(Channel channel) => lastKeyFrames[channel];
	public KeyFrame GetUpcomingKeyFrame(Channel channel) => upcomingKeyFrames[channel];
	public int CurrentTransitionFrames(Channel channel) => GetLastKeyFrame(channel).atFrame - GetUpcomingKeyFrame(channel).atFrame;
	public int CurrentTransitionTicks(Channel channel) => CountTickLengths(GetLastKeyFrame(channel).atFrame, GetUpcomingKeyFrame(channel).atFrame);
	private int CountTickLengths(int from, int to)
	{
		int result = 0;
		for (int i = Mathf.Min(from, to); i < Mathf.Max(from, to); i++)
		{
			if (owner.playbackSteps[i] is Frame frame)
			{
				result += frame.framesDuration;
			}
		}
		return result;
	}

	//todo: check if lerping is right
	public float GetChannelValue(Channel channel)
	{
		var interpolationSetting = interpolationSettings[channel];

		return interpolationSetting.interpolator(GetLastKeyFrame(channel).value, GetUpcomingKeyFrame(channel).value, (float)(CountTickLengths(GetLastKeyFrame(channel).atFrame, CurrentIndex) + this.TicksInCurrentFrame) / (float)CurrentTransitionTicks(channel));

	}

	public SlideShowInstant ThisInstant()
	{
		string elementName = CurrentFrame.elementName;
		string shader = Shader;
		Vector2 position = new(GetChannelValue(Channel.X), GetChannelValue(Channel.Y));
		Color color = new(GetChannelValue(Channel.R), GetChannelValue(Channel.G), GetChannelValue(Channel.B), GetChannelValue(Channel.A));
		return new SlideShowInstant(elementName, shader, position, color);
	}
	sealed class NoActualFramesException : Exception
	{
		public override string Message => $"Advanced {MAX_INSTANT_INSTRUCTIONS} times without finding a frame; presuming that there are no actual frames.";
	}
}
