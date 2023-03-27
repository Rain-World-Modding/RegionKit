namespace RegionKit.Modules.ShelterBehaviors;

public class HoldToTriggerTutorialData : ManagedData
{
	[IntegerField("htttcd", -1, 12, 6, displayName: "HTT Tut. Cooldown")]
	public int cooldown;
	public HoldToTriggerTutorialData(PlacedObject owner) : base(owner, null)
	{
	}
}
