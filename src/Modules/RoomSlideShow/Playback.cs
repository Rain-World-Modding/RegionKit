using System.Text.RegularExpressions;

namespace RegionKit.Modules.Slideshow;

internal class Playback
{
	public readonly List<PlaybackStep> playbackSteps;
	public readonly bool loop;

	public Playback(List<PlaybackStep> playbackSteps, bool loop)
	{
		this.playbackSteps = playbackSteps;
		this.loop = loop;
	}


	private Playback PushNewFrame(string elementName, int? ticksDuration, KeyFrame.Raw[] keyFramesRaw)
	{
		int newIndex = this.playbackSteps.Count;
		KeyFrame[] keyFrames = keyFramesRaw.Select(kfr => new KeyFrame(newIndex, kfr)).ToArray();

		playbackSteps.Add(new Frame(newIndex, elementName, ticksDuration, keyFrames));
		return this;
	}
	private Playback PushNewStep(PlaybackStep newStep)
	{
		playbackSteps.Add(newStep);
		return this;
	}
	public static Playback MakeTestPlayback()
	{
		Playback result = new Playback(new List<PlaybackStep>() {
			new SetDelay(3),
			new SetShader("PLOO"),
			new SetInterpolation(InterpolationKind.Linear, new[] {Channel.R, Channel.B})
		},
		false);
		result.PushNewFrame("f1", 1, new KeyFrame.Raw[] { new(Channel.R, 0f), new(Channel.B, 0f) });
		result.PushNewFrame("ff", null, new KeyFrame.Raw[0]);
		result.PushNewFrame("ff", null, new KeyFrame.Raw[0]);
		result.PushNewFrame("f2", 1, new KeyFrame.Raw[] { new(Channel.R, 1f) });
		result.PushNewFrame("ff", null, new KeyFrame.Raw[0]);
		result.PushNewFrame("ff", null, new KeyFrame.Raw[0]);
		result.PushNewFrame("ff", null, new KeyFrame.Raw[0]);
		result.PushNewFrame("f3", 1, new KeyFrame.Raw[] { new(Channel.B, 1f) });
		return result;
	}

	public static Playback ReadFromText(string[] text)
	{
		Regex commasplit = new("\\s*,\\s*");
		Regex spacesplit = new Regex("\\s+");
		foreach (string line in text)
		{

		}
		throw new NotImplementedException();
	}
}
