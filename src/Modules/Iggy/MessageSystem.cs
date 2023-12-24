namespace RegionKit.Modules.Iggy;

public class MessageSystem
{
	public readonly static string[] randomHints = new[] { 
		"Right click an element, and I will try to describe it.",
		"Clicking on overlapping UI elements clicks all of them. Space your panels out to avoid unwanted input.",
		"When you exit the room with devtools on, the screen does not automatically clear itself. Close and reopen it to clear.",
		"Devtools Triggers tab is by far the most underused. Very few mods added anything to it.",
		"The default color scheme of Dev Tools is hard on the eyes. There is a workshop mod called Legible Devtools that makes things blue.",
		"Black goo effect is on by default in a new editor project for a reason. The slop hides unnecessary detail in the walls."
		};
	public readonly static TimeSpan maxIdleTime = TimeSpan.FromSeconds(10);
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
			if (DateTime.Now - lastMessageWasOver > maxIdleTime)
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

	public void DisplayNow(TimeSpan howLong, string? text)
	{
		currentMessage = new(howLong, text ?? "NULL");
	}
}
