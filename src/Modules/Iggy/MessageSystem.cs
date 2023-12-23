namespace RegionKit.Modules.Iggy;

public class MessageSystem
{
	public readonly static string[] randomHints = new[] { "I haven't implemented actual help requests yet", "DevInterface Signals suck", "Clean yo shoes" };
	public readonly static TimeSpan displayPassiveHintAfter = TimeSpan.FromSeconds(15);
	public readonly static TimeSpan hintDuration = TimeSpan.FromSeconds(10);
	public Message? currentMessage;
	public DateTime lastMessageWasOver;
	public MessageSystem()
	{
		ClearMessage();
	}

	public void Update()
	{
		if (currentMessage is null)
		{
			if (DateTime.Now - lastMessageWasOver > displayPassiveHintAfter)
			{

				string hint = randomHints.RandomOrDefault()!;
				LogTrace($"selected hint <{hint}>, displaying for {hintDuration}");
				DisplayNow(hintDuration, hint);
			}
		}
		else if (currentMessage.Expired)
		{
			ClearMessage();
		}
	}

	private void ClearMessage()
	{
		LogTrace("Setting message to null and resetting timer");
		currentMessage = null;
		lastMessageWasOver = DateTime.Now;
	}

	public void DisplayNow(TimeSpan howLong, string text)
	{
		currentMessage = new(howLong, text);
	}
}
