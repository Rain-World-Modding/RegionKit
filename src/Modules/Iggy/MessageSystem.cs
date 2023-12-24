namespace RegionKit.Modules.Iggy;

public class MessageSystem
{
	public readonly List<string> selectedHints = new();
	public readonly List<string> hintsAlwaysOn = new()
	{
		"Right click an element, and I will try to describe it.",
		"Clicking on overlapping UI elements clicks all of them. Space your panels out to avoid unwanted input.",
		"When you exit the room with devtools on, the screen does not automatically clear itself. Close and reopen it to clear.",
		"Devtools Triggers tab is by far the most underused (except for music events). Very few mods added anything to it.",
		"You can find all Rain World wikis on Miraheze wikifarm. If someone leads you to another address, don't trust them."
	};
	public readonly List<Hint> hintsConditional = new()
	{
		new("The default color scheme of Dev Tools is hard on the eyes. There is a workshop mod called Legible Devtools that makes things blue.", () => !ModManager.ActiveMods.Any(mod => mod.id == "niko.legibledevtools")),
	};
	public readonly static TimeSpan reselectHintsInterval = TimeSpan.FromMinutes(1f);
	public readonly static TimeSpan maxIdleTime = TimeSpan.FromSeconds(10);
	public readonly static TimeSpan hintDuration = TimeSpan.FromSeconds(10);
	public readonly Queue<Message> upcomingMessages = new();
	public Message? currentMessage;
	public DateTime lastMessageWasOver;
	public DateTime lastSelectedHints;
	public MessageSystem()
	{
		ClearMessage();
		SelectHints();
	}
	public void Update()
	{
		if (currentMessage is null)
		{
			if (DateTime.Now - lastMessageWasOver > maxIdleTime)
			{
				string hint = selectedHints.RandomOrDefault() ?? "NULL";
				LogTrace($"selected hint <{hint}>, displaying for {hintDuration}");
				PlayMessageNow(new(hintDuration, hint, false));
			}
		}
		else if (currentMessage.Expired)
		{
			ClearMessage();
			if (upcomingMessages.Count > 0)
			{
				currentMessage = upcomingMessages.Dequeue();
			}
		}
		if (DateTime.Now - lastSelectedHints > reselectHintsInterval)
		{
			SelectHints();
		}
	}
	private void ClearMessage()
	{
		LogTrace("Setting message to null and resetting timer");
		currentMessage = null;
		lastMessageWasOver = DateTime.Now;
	}
	public void PlayMessageNow(Message message)
	{
		if (currentMessage?.uninterruptable ?? false) return;
		currentMessage = message;
	}
	public void SelectHints()
	{
		selectedHints.Clear();
		selectedHints.AddRange(hintsAlwaysOn);
		selectedHints.AddRange(
			hintsConditional
			.Where(hint => hint.activeNow())
   			.Select(hint => hint.message)
			);
		lastSelectedHints = DateTime.Now;
	}

	public record Hint(string message, Func<bool> activeNow);
}
