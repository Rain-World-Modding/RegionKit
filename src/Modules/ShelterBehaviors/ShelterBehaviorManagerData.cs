namespace RegionKit.Modules.ShelterBehaviors;
public class ShelterManagerData : ManagedData
{
	//[BooleanField("nvd", true, displayName: "Hide Vanilla Door")]
	[BackedByField("nvd")]
	public bool hideVanillaDoor;
	//[BooleanField("htt", false, displayName: "Hold To Trigger")]
	[BackedByField("htt")]
	public bool holdToTrigger;
	//[IntegerField("htts", 1, 10, 4, displayName: "HTT Trigger Speed")]
	[BackedByField("htts")]
	public int httSpeed;
	//[BooleanField("cs", false, displayName: "Consumable Shelter")]
	[BackedByField("cs")]
	public bool isConsumable;
	//[IntegerField("csmin", -1, 30, 3, displayName: "Consum. Cooldown Min")]
	[BackedByField("csmin")]
	public int consumableCdMin;
	//[IntegerField("csmax", 0, 30, 6, displayName: "Consum. Cooldown Max")]
	[BackedByField("csmax")]
	public int consumableCdMax;
	//[IntegerField("ftt", 0, 400, 20, ManagedFieldWithPanel.ControlType.slider, displayName: "Frames to Trigger")]
	[BackedByField("ftt")]
	public int framesToTrigger;
	//[IntegerField("fts", 0, 400, 40, ManagedFieldWithPanel.ControlType.slider, displayName: "Frames to Sleep")]
	[BackedByField("fts")]
	public int framesToSleep;
	//[IntegerField("ftsv", 0, 400, 60, ManagedFieldWithPanel.ControlType.slider, displayName: "Frames to Starvation")]
	[BackedByField("ftsv")]
	public int framesToStarve;
	//[IntegerField("ftw", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName: "Frames to Win")]
	[BackedByField("ftw")]
	public int framesToWin;
	//[IntegerField("ini", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName: "Initial wait")]
	[BackedByField("ini")]
	public int initWait;
	//[IntegerField("ouf", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName: "Open up anim")]
	[BackedByField("ouf")]
	public int openUpAnim;
	//[BooleanField("ani", false, displayName: "Animate Water")]
	[BackedByField("ani")]
	public bool animateWater;
	public ShelterManagerData(PlacedObject owner) : base(owner, new ManagedField[]
	{
		new BooleanField("nvd", true, displayName:"No Vanilla Door"),
		new BooleanField("htt", false, displayName:"Hold To Trigger"),
		new IntegerField("htts", 1, 10, 4, displayName:"HTT Trigger Speed"),
		new BooleanField("cs", false, displayName:"Consumable Shelter"),
		new IntegerField("csmin", -1, 30, 3, displayName:"Consum. Cooldown Min"),
		new IntegerField("csmax", 0, 30, 6, displayName:"Consum. Cooldown Max"),
		new IntegerField("ftt", 0, 400, 20, ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Trigger"),
		new IntegerField("fts", 0, 400, 40, ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Sleep"),
		new IntegerField("ftsv", 0, 400, 60, ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Starvation"),
		new IntegerField("ftw", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Win"),
		new IntegerField("ini", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName:"Initial wait"),
		new IntegerField("ouf", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider, displayName:"Open up anim"),
		new BooleanField("ani", false, displayName:"Animate Water"),
 	})
	{
	}
}
