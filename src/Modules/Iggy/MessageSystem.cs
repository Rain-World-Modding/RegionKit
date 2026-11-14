namespace RegionKit.Modules.Iggy;

public class MessageSystem
{
	public readonly List<string> selectedHints = new();
	public readonly List<string> hintsAlwaysOn = new()
	{
		"Right click an element, and I will try to describe it.",
		"Clicking on overlapping UI elements clicks all of them. Space your panels out to avoid unwanted input.",
		"When you exit the room with devtools on, the screen does not automatically clear itself. Close and reopen it to clear.",
		"You can find all Rain World wikis on Miraheze wikifarm. If someone leads you to another address, don't trust them.",
		"You can turn me off in RegionKit's Remix menu.",
		"Object panels have a copy and paste feature. Hold shift and click the arrows next to the minimize and move handles.",
		"You can submit your own decals to be included in RegionKit's decal collection! Contact an active RegionKit developer for more information.",
		"The following effects are mutually exclusive, and each will take priority over effects listed later: SkyBloom, SkyAndLightBloom, Lightning, LightBurn, Fog, Bloom, VoidMelt",
		"RegionKit adds the ability to use videos as custom decals! Supported extension types are: " + string.Join(", ", AnimatedDecals.VideoManager._supportedExtensions),
	};
	public readonly List<Hint> hintsConditional = new()
	{
		new("The default color scheme of Dev Tools is hard on the eyes. There is a workshop mod called Beautiful Devtools that makes things nicer.", () => !ModManager.ActiveMods.Any(mod => mod.id == "better_dev_tools")),
		new("Joar.", () => UnityEngine.Random.Range(0, 65536) == 0),
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
