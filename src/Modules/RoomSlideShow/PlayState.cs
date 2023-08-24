using System.Linq;

namespace RegionKit.Modules.Slideshow;

internal sealed class PlayState
{
	//todo: test ALL OF THIS SHIT
	private const int MAX_INSTANT_INSTRUCTIONS = 1024;
	private readonly Dictionary<Channel, KeyFrame> startKeyFrames;
	private readonly Dictionary<Channel, KeyFrame> endKeyFrames;

	private readonly Dictionary<Channel, KeyFrame> lastKeyFrames = new();
	private readonly Dictionary<Channel, KeyFrame> nextKeyFrames = new();
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
		this.owner = owner;
		this.startKeyFrames = CreateDefaultKeyframes(0);
		this.endKeyFrames = CreateDefaultKeyframes(owner.playbackSteps.Length - 1);
		this.interpolationSettings = CreateDefaultInterpolations();
	}
	private static Dictionary<Channel, SetInterpolation> CreateDefaultInterpolations()
	{
		return new() {
			{ Channel.X, new SetInterpolation(InterpolationKind.No, new[] {Channel.X})},
			{ Channel.Y, new SetInterpolation(InterpolationKind.No, new[] {Channel.Y})},
			{ Channel.R, new SetInterpolation(InterpolationKind.No, new[] {Channel.R})},
			{ Channel.G, new SetInterpolation(InterpolationKind.No, new[] {Channel.G})},
			{ Channel.B, new SetInterpolation(InterpolationKind.No, new[] {Channel.B})},
			{ Channel.A, new SetInterpolation(InterpolationKind.No, new[] {Channel.A})},
		};
	}
	private static Dictionary<Channel, KeyFrame> CreateDefaultKeyframes(int at)
	{
		return new() {
				{ Channel.X, new(at, Channel.X, 0f)},
				{ Channel.Y, new(at, Channel.Y, 0f)},
				{ Channel.R, new(at, Channel.R, 1f)},
				{ Channel.G, new(at, Channel.G, 1f)},
				{ Channel.B, new(at, Channel.B, 1f)},
				{ Channel.A, new(at, Channel.A, 1f)},
			};
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

			case SetInterpolation setInterpol:
				foreach (var channel in setInterpol.channels)
				{
					interpolationSettings[channel] = setInterpol;
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
			UpdateKeyFrames();
		}
		if (overstayed)
		{
			throw new NoActualFramesException();
		}
		TicksSinceStart++;
	}
	public void UpdateKeyFrames()
	{
		//move keyframes that have been hit to last
		List<KeyFrame> fromNextToLast = new();
		foreach ((Channel channel, KeyFrame keyframe) in nextKeyFrames)
		{
			if (keyframe.atFrame == CurrentIndex)
			{
				fromNextToLast.Add(keyframe);
			}
		}
		foreach (KeyFrame keyframe in fromNextToLast)
		{
			nextKeyFrames.Remove(keyframe.channel);
			lastKeyFrames[keyframe.channel] = keyframe;
		}
		//search for upcoming keyframes
		var allChannelsToCheck = ((Channel[])Enum.GetValues(typeof(Channel))).Where(item => !nextKeyFrames.ContainsKey(item)).ToList();
		for (int i = CurrentIndex; i < owner.playbackSteps.Length; i++)
		{
			PlaybackStep step = owner.playbackSteps[i];
			if (step is not Frame frame) continue;
			foreach (KeyFrame keyFrame in frame.keyFramesHere)
			{
				if (allChannelsToCheck.Contains(keyFrame.channel))
				{
					nextKeyFrames[keyFrame.channel] = keyFrame;
				}
			}
		}
		//i hate this lmao
	}
	public SetInterpolation GetInterpolationSetting(Channel channel) => interpolationSettings[channel];
	public KeyFrame GetLastKeyFrame(Channel channel) => lastKeyFrames.TryGetValue(channel, out KeyFrame frame) ? frame : startKeyFrames[channel];
	public KeyFrame GetUpcomingKeyFrame(Channel channel) => nextKeyFrames.TryGetValue(channel, out KeyFrame frame) ? frame : endKeyFrames[channel];
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

		KeyFrame lastKeyFrame = GetLastKeyFrame(channel);
		KeyFrame upcomingKeyFrame = GetUpcomingKeyFrame(channel);
		int ticksInTransitionSoFar = CountTickLengths(lastKeyFrame.atFrame, CurrentIndex) + this.TicksInCurrentFrame;
		int ticksInTransitionTotal = CurrentTransitionTicks(channel);
		return interpolationSetting.interpolator(lastKeyFrame.value, upcomingKeyFrame.value, (float)ticksInTransitionSoFar / (float)ticksInTransitionTotal);

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
