namespace RegionKit.Modules.Iggy;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), nameof(Update), tickPeriod: 1, moduleName: "Iggy")]
public static class _Module
{
	internal static System.Runtime.CompilerServices.ConditionalWeakTable<DevInterface.Page, DevInterface.Panel> __iggys = new() { };
	internal static System.Runtime.CompilerServices.ConditionalWeakTable<DevInterface.DevUI, DTRightClickTracker> __devUIRMB = new();
	internal static MessageSystem __messageSystem = new();
	internal static List<DevInterface.DevUINode> __itemsRequestedTooltip = new();
	internal static bool __IggyCollapsed = false;
	internal static Vector2 __IggyPos = new(100f, 100f);

	#region L I F E
	public static void Setup() { }
	public static void Enable()
	{
		On.DevInterface.Page.ctor += __SpawnDevIggy;
		On.DevInterface.DevUI.Update += __TrackRightClicks;
		On.DevInterface.DevUINode.Update += __SignalRightClick;
	}
	public static void Disable()
	{
		On.DevInterface.Page.ctor -= __SpawnDevIggy;
		On.DevInterface.DevUI.Update -= __TrackRightClicks;
		On.DevInterface.DevUINode.Update -= __SignalRightClick;
	}
	public static void Update()
	{
		if (__itemsRequestedTooltip.Count > 0)
		{
			__itemsRequestedTooltip.Sort((a, b) => __GetTooltipPriority(b) - __GetTooltipPriority(a));
			DevInterface.DevUINode item = __itemsRequestedTooltip[0];
			__messageSystem.DisplayNow(TimeSpan.FromSeconds(5), __GetTooltip(item));
		}
		__itemsRequestedTooltip.Clear();
		__messageSystem.Update();
	}
	#endregion
	#region methods
	internal static void __DevUIElementReceivedRMB(DevInterface.DevUINode node)
	{
		if (!__itemsRequestedTooltip.Contains(node)) __itemsRequestedTooltip.Add(node);
	}
	internal static string __GetTooltip(DevInterface.DevUINode node) => node switch
	{
		//todo: cover more vanilla shit
		IGiveAToolTip ttp => ttp.ToolTip,
		DevInterface.AddObjectButton objButton => $"This button will add an {objButton.type} object to your room.",
		DevInterface.AddEffectButton effButton => $"This button will add an {effButton.type} effect to your room.",
		DevInterface.RoomPanel roomPanel => $"This panel controls basic settings for room {roomPanel.roomRep.room.name}.",
		DevInterface.EffectPanel effPanel => $"This panel controls amount (and maybe other settings) for effect {effPanel.effect.type} in this room, inherited: {effPanel.effect.inherited}. POM effect: {EffExt.Eff.TryGetEffectDefinition(effPanel.effect.type, out _)}",
		_ => $"This is a {node.GetType().FullName}. Its ID string is {node.IDstring}."
	};
	internal static int __GetTooltipPriority(DevInterface.DevUINode node) => node switch
	{
		IGiveAToolTip ttp => ttp.ToolTipPriority,
		DevInterface.Button => 10,
		DevInterface.Slider => 10,
		DevInterface.Panel => 5,
		_ => 0
	};
	internal static bool __IsThisBeingHovered(DevInterface.DevUINode node) => node switch
	{
		IGeneralMouseOver igmo => igmo.MouseOverMe,
		DevInterface.RectangularDevUINode rnode => rnode.MouseOver,
		DevInterface.Slider slider => new Rect(slider.fSprites[slider.fSprites.Count - 2].GetPosition(), new Vector2(slider.fSprites[slider.fSprites.Count - 2].width, slider.fSprites[slider.fSprites.Count - 2].height)).Contains(slider.owner.mousePos),
		_ => false,
	};
	#endregion

	#region hooks

	public static void __SignalRightClick(On.DevInterface.DevUINode.orig_Update orig, DevInterface.DevUINode self)
	{
		orig(self);
		DTRightClickTracker tracker = __devUIRMB.GetOrCreateValue(self.owner);
		if (self is DevInterface.RectangularDevUINode rnode && rnode.MouseOver && tracker.RightClick)
		{
			__DevUIElementReceivedRMB(rnode);
		}
	}
	public static void __TrackRightClicks(On.DevInterface.DevUI.orig_Update orig, DevInterface.DevUI self)
	{
		orig(self);
		bool rmbDown = Input.GetMouseButtonDown(1);
		DTRightClickTracker tracker = __devUIRMB.GetOrCreateValue(self);
		tracker.rmbWasDown = tracker.rmbDown;
		tracker.rmbDown = rmbDown;
	}
	public static void __SpawnDevIggy(
		On.DevInterface.Page.orig_ctor orig,
		DevInterface.Page self,
		DevInterface.DevUI owner,
		string IDstring,
		DevInterface.DevUINode parentNode,
		string name)
	{
		orig(self, owner, IDstring, parentNode, name);
		Iggy iggy = new Iggy(
			owner,
			IDstring + "_Iggy",
			self);
		self.subNodes.Add(iggy);
		__iggys.Add(self, iggy);
	}
	#endregion

	internal class DTRightClickTracker
	{
		public bool rmbDown;
		public bool rmbWasDown;
		public bool RightClick => rmbDown && !rmbWasDown;
	}
}
