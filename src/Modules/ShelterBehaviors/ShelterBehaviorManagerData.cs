namespace RegionKit.Modules.ShelterBehaviors;
public class ShelterManagerData : ManagedData
{
	[BooleanField("nvd", true, displayName: "Hide Vanilla Door")]
	public bool hideVanillaDoor;
	[BooleanField("htt", false, displayName: "Hold To Trigger")]
	public bool holdToTrigger;
	[IntegerField("htts", 1, 10, 4, displayName: "HTT Trigger Speed")]
	public int httSpeed;
	[BooleanField("cs", false, displayName: "Consumable Shelter")]
	public bool isConsumable;
	[IntegerField("csmin", -1, 30, 3, displayName: "Consum. Cooldown Min")]
	public int consumableCdMin;
	[IntegerField("csmax", 0, 30, 6, displayName: "Consum. Cooldown Max")]
	public int consumableCdMax;
	[IntegerField("ftt", 0, 400, 20, ManagedFieldWithPanel.ControlType.slider, displayName: "Frames to Trigger")]
	public int framesToTrigger;
	[IntegerField("fts", 0, 400, 40, ManagedFieldWithPanel.ControlType.slider, displayName: "Frames to Sleep")]
	public int framesToSleep;
	[IntegerField("ftsv", 0, 400, 60, ManagedFieldWithPanel.ControlType.slider, displayName: "Frames to Starvation")]
	public int framesToStarve;
	[IntegerField("ftw", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName: "Frames to Win")]
	public int framesToWin;
	[IntegerField("ini", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName: "Initial wait")]
	public int initWait;
	[IntegerField("ouf", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName: "Open up anim")]
	public int openUpAnim;
	[BooleanField("ani", false, displayName: "Animate Water")]
	public bool animateWater;
	public ShelterManagerData(PlacedObject owner) : base(owner, null)
	{
	}
}
