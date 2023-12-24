namespace RegionKit.Modules.Iggy;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), nameof(Update), tickPeriod: 1, moduleName: "Iggy")]
public static class _Module
{
	internal static System.Runtime.CompilerServices.ConditionalWeakTable<DevInterface.Page, DevInterface.Panel> __iggys = new() { };
	internal static System.Runtime.CompilerServices.ConditionalWeakTable<DevInterface.DevUI, DTRightClickTracker> __devUIRMB = new();
	internal static System.Runtime.CompilerServices.ConditionalWeakTable<DevInterface.DevUINode, Func<ToolTip>> __attachedToolTips = new();
	internal static MessageSystem __messageSystem = new();
	internal static List<ToolTip> __requestedToolTips = new();
	internal static bool __IggyCollapsed = false;
	internal static Vector2 __IggyPos = new(100f, 100f);

	#region L I F E
	public static void Setup() { }
	public static void Enable()
	{
		On.DevInterface.Page.ctor += __SpawnDevIggy;
		On.DevInterface.DevUI.ctor += __ShowIggyHelp;
		On.DevInterface.DevUI.Update += __TrackRightClicks;
		On.DevInterface.DevUINode.Update += __SignalRightClick;
	}
	public static void Disable()
	{
		On.DevInterface.Page.ctor -= __SpawnDevIggy;
		On.DevInterface.DevUI.ctor -= __ShowIggyHelp;
		On.DevInterface.DevUI.Update -= __TrackRightClicks;
		On.DevInterface.DevUINode.Update -= __SignalRightClick;
	}
	public static void Update()
	{
		if (__requestedToolTips.Count > 0)
		{
			__requestedToolTips.Sort((a, b) => b.priority - a.priority);
			var item = __requestedToolTips[0];
			__messageSystem.PlayMessageNow(new(TimeSpan.FromSeconds(5), item.text, false));
		}
		__requestedToolTips.Clear();
		__messageSystem.Update();
	}
	#endregion
	#region methods

	internal static void __DevUIElementReceivedRMB(DevInterface.DevUINode node)
	{
		if (!__requestedToolTips.Any(tt => tt.source == node)) __requestedToolTips.Add(__GetTooltip(node));
	}
	internal static ToolTip __GetTooltip(DevInterface.DevUINode node) => node switch
	{
		//todo: cover more vanilla shit
		IGiveAToolTip givesAToolTip => givesAToolTip.toolTip,
		_ when __attachedToolTips.TryGetValue(node, out Func<ToolTip> ttf) => ttf(),
		_ when __GetSpecialHardcodedTooltip(node) is ToolTip tt => tt,
		_ => new($"This is a {node.GetType().FullName}. Its ID string is {node.IDstring}.", 0, node)
	};
	internal static ToolTip? __GetSpecialHardcodedTooltip(DevInterface.DevUINode node)
	{
		//meant to ONLY handle tooltips for elements from vanilla/msc
		ToolTip? result = node switch
		{
			//----
			//BUTTONS
			//----
			DevInterface.SwitchPageButton button => new($"Switches your devtools to another tab.", 10, node), //tab handles
			DevInterface.DangerTypeCycler cycler => new("Switches how the room reacts to end of cycle - whether it rains, floods, both, or neither.", 10, node), //settings GO cycler
			DevInterface.AddEffectButton button => new($"Adds or removes effect '{button.type}' to the room. If effect is inherited, switches to local and back.", 10, node), //spawn buttons on effects menu
			DevInterface.AddObjectButton button => new($"Adds a placed object '{button.type}' to the room.", 10, node), //spawn buttons on objects menu
			DevInterface.AddSoundButton button => new($"Adds a new ambient sound '{button.sound}' to the room. Sound's type is selected at the top of sounds panel before adding.", 10, node), //spawn buttons on sounds menu
			DevInterface.AddTriggerButton button => new("Adds a new trigger of specific form. You can add an event that will be fired on this trigger from the trigger's own panel.", 10, node),
			DevInterface.AddSoundType selector => new($"This selects what type the sounds you add will be.", 10, node), //switch at the top of sounds menu
			DevInterface.MapPage.ModeCycler cycler => new("Switches between dev and canon map views. Dev view displays more info. Canon view is used for rendering the automap (buggy, never got fixed).", 10, node), //top button in map tab
			DevInterface.RoomAttractivenessPanel.CreatureButton button => new($"Selects a specific creature type to paint attractiveness map with.", 10, node),
			DevInterface.Button button => button.IDstring switch
			{
				"Prev_Button" => new("Cycles pages of a panel backwards.", 10, node), //general button
				"Next_Button" => new("Cycles pages of a panel forwards.", 10, node), //general button
				"Inherit_Button" => new("Toggles inheriting of a certain room property from room template. See top right of the screen for templates.", 10, node), //button on a bunch of properties in the room props tab
				"Room_Specific_Script" => new("Used to toggle room-specific code in some rooms. The effects are hard-coded by room name, hooks needed to extend.", 10, node),
				"Wet_Terrain" => new("Toggles a slight wave-like distortion effect that passes over the screen.", 10, node),
				"Update_Dev_Positions" => new("Resets dev map room positions", 10, node), //todo: verify
				"Room_Attractiveness_Button" => new("Toggles the room AI attraction map mode, used to make rooms alluring or uncomfortable to creatures. Mostly works for abstract AI. Never fully forbids.", 10, node),
				"Sub_Regions_Toggle" => new("Switches to subregion mode, where you can assign rooms to subregions.", 10, node), //map mode
				"Apply_To_All" => new("Paints the entire region with selected attractiveness for selected creature templates.", 10, node), //map mode attr tool
				"Multi_Use_Button" => new("Select whether the trigger can be activated repeatedly or only once per playthrough.", 10, node), //trigger option
				"Entrance_Button" => new("Require player to enter the room from a specific pipe.", 10, node), //trigger option
				"Slugcat_Button" => new("Allow or disallow a specific slugcat campaign to activate the trigger.", 10, node), //trigger option
				"Event_Button" => new("Select a result for this trigger.", 10, node),
				_ => null
			},
			//----
			//PANELS
			//----
			DevInterface.RoomPanel panel => new($"This is a room tile map for {panel.roomRep.room.name}. Its subregion is {panel.roomRep.room.subregionName}. Letters on sticks are creature pointers. Squares below are dens.", 5, node),
			DevInterface.RoomAttractivenessPanel panel => new("Select a creature template type from panel, cycle the switch at the bottom to select your attractiveness value, then click on rooms to paint. Green buttons are special modes.", 5, node),
			DevInterface.AmbientSoundPanel panel => new($"This is a settings panel for sound '{panel.sound.sample}'. The sound is {panel.sound.type}, inherited: {panel.sound.inherited}", 5, node),
			DevInterface.TriggerPanel panel => new($"Customize activation conditions for this trigger. Select the event at the bottom.", 5, node),
			DevInterface.SelectEventPanel => new("Select what event will the trigger cause", 5, node), //event type
			DevInterface.MusicEventPanel => new("This event will ask the music player to start a specific track. Requested tracks can have different priority.", 5, node), //event type
			DevInterface.StopMusicEventPanel => new("This event will stop music tracks that are currently playing.", 5, node), //event type
			DevInterface.ShowProjectedImageEventPanel => new("This event will tell me to show the player something.", 5, node), //event type
			DevInterface.StandardEventPanel => new("This event doesn't have any extra options", 5, node), //event type
			DevInterface.Panel panel => panel.IDstring switch
			{
				"Effects_Panel" => new("Select effects to add to your room. Effects in blue are inherited from a template, effects in green are local. No duplicates. Press again to remove effect.", 5, node), //effects menu
				"Objects_Panel" => new("Select special objects to decorate your room. Objects can't be inherited. Duplicates allowed. Delete by dragging to the trash bin (bottom left)", 5, node), //objects menu
				"Sounds_Panel" => new("Select ambient sounds to add to the room. Select the type on top before spawning sounds. All sounds except spot can be inherited.", 5, node), //sounds menu
				"Triggers_Panel" => new("Create a special event that happens to the player based on some behavior.", 5, node), //triggers menu
				_ => null,
			},
			DevInterface.MouseOverSwitchColorLabel label when label.IDstring.StartsWith("Inherit_From_Template_") => new("Sets the room to inherit from a specific region-wide template. Basic room settings, effect list and ambient sounds (excluding spot sounds) are inherited.", 10, node), //weird pseudo button for templates
			DevInterface.MouseOverSwitchColorLabel label when label.IDstring.StartsWith("Save_As_Template_") => new("Saves current room properties. Basic room settings, effect list and ambient sounds (excluding spot sounds) are inherited.", 10, node), //weird pseudo button for templates
			_ => null,
		};


		return result;
	}

	internal static bool __IsThisBeingHovered(DevInterface.DevUINode node) => node switch
	{
		IGeneralMouseOver igmo => igmo.MouseOverMe,
		DevInterface.RectangularDevUINode rnode => rnode.MouseOver,
		DevInterface.Slider slider => new Rect(slider.fSprites[slider.fSprites.Count - 2].GetPosition(), new Vector2(slider.fSprites[slider.fSprites.Count - 2].width, slider.fSprites[slider.fSprites.Count - 2].height)).Contains(slider.owner.mousePos),
		_ => false,
	};
	#endregion
	#region hooks
	public static void __ShowIggyHelp(On.DevInterface.DevUI.orig_ctor orig, DevInterface.DevUI self, RainWorldGame rwg)
	{
		orig(self, rwg);
		__messageSystem.PlayMessageNow(new(TimeSpan.FromSeconds(20), "Right click an element, and I will try to decribe it.", false));
	}
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
