using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Triggers;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Triggers")]
internal static class _Module
{
	internal static void Enable()
	{
		// Init static constructor of enums so they load on time
		_ = _Enums.ExplodeEvent;

		// Loading hooks
		IL.RoomSettings.LoadTriggers += RoomSettings_LoadTriggers;
		IL.EventTrigger.FromString += EventTrigger_FromString;

		// Trigger check hooks
		On.ActiveTriggerChecker.FireEvent += ActiveTriggerChecker_FireEvent;
		On.ActiveTriggerChecker.Update += ActiveTriggerChecker_Update;

		// Dev UI hooks
		On.DevInterface.TriggersPage.ctor += TriggersPage_ctor;
		On.DevInterface.SelectEventPanel.ctor += SelectEventPanel_ctor;
		On.DevInterface.TriggersPage.CreateTriggerRep += TriggersPage_CreateTriggerRep;
		IL.DevInterface.TriggerPanel.ctor += TriggerPanel_ctor;
		On.DevInterface.TriggerPanel.AddEvent += TriggerPanel_AddEvent;
		On.DevInterface.TriggerPanel.AddEventPanel += TriggerPanel_AddEventPanel;
	}

	internal static void Disable()
	{
	}

	#region Functionality

	private static EventTrigger? CreateEventTrigger(EventTrigger.TriggerType type)
	{
		if (type == EventTrigger.TriggerType.SeeCreature)
		{
			return new SeeCreatureTrigger(CreatureTemplate.Type.StandardGroundCreature);
		}
		if (type == _Enums.QuadTrigger)
		{
			return new QuadTrigger();
		}
		if (type == _Enums.RectTrigger)
		{
			return new RectTrigger();
		}

		return null;
	}

	private static TriggeredEvent? CreateTriggeredEvent(TriggeredEvent.EventType type)
	{
		if (type == _Enums.ExplodeEvent)
		{
			return new ExplodeEvent();
		}

		return null;
	}

	#endregion

	#region Hooks

	private static void RoomSettings_LoadTriggers(ILContext il)
	{
		// Goal: load custom event triggers

		var c = new ILCursor(il);

		// Setup
		ILLabel cursorGoto = null!;
		ILLabel brTo = null!;
		c.GotoNext(x => x.MatchLdstr("Spot"));
		c.GotoNext(MoveType.After, x => x.MatchBrfalse(out cursorGoto));
		c.GotoNext(x => x.MatchBr(out brTo));
		c.GotoLabel(cursorGoto);
		c.MoveAfterLabels();

		// Emit our code
		c.Emit(OpCodes.Ldarg_0);
		c.Emit(OpCodes.Ldloc_2);
		c.Emit(OpCodes.Ldloc_1);
		c.EmitDelegate(MaybeLoadCustomEventTrigger);
		c.Emit(OpCodes.Brtrue, brTo);

		static bool MaybeLoadCustomEventTrigger(RoomSettings self, string type, string[] data)
		{
			EventTrigger? newTrigger = CreateEventTrigger(new EventTrigger.TriggerType(type, false));

			if (newTrigger != null)
			{
				self.triggers.Add(newTrigger);
				newTrigger.FromString(data);
				return true;
			}
			return false;
		}
	}

	private static void EventTrigger_FromString(ILContext il)
	{
		// Goal: load custom triggered events

		var c = new ILCursor(il);

		// Setup
		ILLabel cursorGoto = null!;
		c.GotoNext(x => x.MatchLdstr("ShowProjectedImageEvent"));
		c.GotoNext(MoveType.After, x => x.MatchBrtrue(out cursorGoto));
		ILLabel cursorGoBackTo = c.DefineLabel();
		c.MarkLabel(cursorGoBackTo);

		ILLabel brTo = null!;
		c.GotoLabel(cursorGoto);
		c.GotoNext(x => x.MatchBr(out brTo));

		c.GotoLabel(cursorGoBackTo, MoveType.AfterLabel);

		// Emit our code
		c.Emit(OpCodes.Ldarg_0);
		c.Emit(OpCodes.Ldloc, 5);
		c.Emit(OpCodes.Ldloc, 4);
		c.EmitDelegate(MaybeLoadCustomTriggeredEvent);
		c.Emit(OpCodes.Brtrue, brTo);

		static bool MaybeLoadCustomTriggeredEvent(EventTrigger self, string type, string[] data)
		{
			TriggeredEvent? newEvent = CreateTriggeredEvent(new TriggeredEvent.EventType(type, false));

			if (newEvent != null)
			{
				self.tEvent = newEvent;
				newEvent.FromString(data);
				return true;
			}
			return false;
		}
	}

	private static void ActiveTriggerChecker_FireEvent(On.ActiveTriggerChecker.orig_FireEvent orig, ActiveTriggerChecker self)
	{
		if (self.eventTrigger.tEvent != null)
		{
			bool triggered = false;
			if (self.eventTrigger.tEvent is ICustomEvent customEvent && (self.eventTrigger.fireChance == 1f || UnityEngine.Random.value < self.eventTrigger.fireChance))
			{
				triggered = true;
				customEvent.Fire(self.eventTrigger, self.room);
			}

			if (triggered && !self.eventTrigger.multiUse)
			{
				self.Destroy();
				return;
			}
		}
		orig(self);
	}

	private static void ActiveTriggerChecker_Update(On.ActiveTriggerChecker.orig_Update orig, ActiveTriggerChecker self, bool eu)
	{
		orig(self, eu);
		if (self.counter < 0)
		{
			if (self.eventTrigger is SeeCreatureTrigger seeCreatureTrigger && self.room.game.Players.Count > 0)
			{
				// This trigger type is not implemented in the base game, so we're doing it here
				IEnumerable<Creature> creatures = self.room.abstractRoom.creatures
					.Where(x => x.creatureTemplate.type == seeCreatureTrigger.creatureType && x.realizedCreature != null && !x.realizedCreature.dead && x.realizedCreature.room == self.room)
					.Select(x => x.realizedCreature);
				for (int i = 0; i < self.room.game.Players.Count; i++)
				{
					if (self.room.game.Players[i].Room == self.room.abstractRoom)
					{
						if (self.TriggerConditions(i) && self.room.game.Players[i].realizedCreature is Player player)
						{
							if (creatures.Any(x => self.room.VisualContact(player.mainBodyChunk.pos, x.DangerPos)))
							{
								self.Positive();
							}
						}
					}
				}
			}
			else if (self.eventTrigger is ICustomTrigger customTrigger)
			{
				for (int i = 0; i < self.room.game.Players.Count; i++)
				{
					if (self.room.game.Players[i].Room == self.room.abstractRoom)
					{
						if (self.wait > 0 && customTrigger.PerformWait)
						{
							self.wait--;
						}
						else if (self.TriggerConditions(i) && self.room.game.Players[i].realizedCreature is Player player && customTrigger.CheckCondition(player, self.room))
						{
							self.Positive();
						}
					}
				}
			}
		}
	}

	private static void TriggersPage_ctor(On.DevInterface.TriggersPage.orig_ctor orig, TriggersPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
	{
		orig(self, owner, IDstring, parentNode, name);
		self.triggersPanel.size.y = 20f * (EventTrigger.TriggerType.values.Count + 0.5f);
		foreach (DevUINode node in self.triggersPanel.subNodes)
		{
			if (node is PositionedDevUINode positionedNode)
			{
				positionedNode.pos.y = self.triggersPanel.size.y - (100f - positionedNode.pos.y);
			}
		}
	}

	private static void SelectEventPanel_ctor(On.DevInterface.SelectEventPanel.orig_ctor orig, SelectEventPanel self, DevUI owner, DevUINode parentNode, Vector2 pos)
	{
		orig(self, owner, parentNode, pos);
		self.size.y = 20f * (TriggeredEvent.EventType.values.Count + 0.5f);
		foreach (DevUINode node in self.subNodes)
		{
			if (node is PositionedDevUINode positionedNode)
			{
				positionedNode.pos.y = self.size.y - (160f - positionedNode.pos.y);
			}
		}
	}

	private static void TriggersPage_CreateTriggerRep(On.DevInterface.TriggersPage.orig_CreateTriggerRep orig, TriggersPage self, EventTrigger.TriggerType tp)
	{
		EventTrigger? newTrigger = CreateEventTrigger(tp);
		if (newTrigger != null)
		{
			Vector2 createPos = Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.2f + new Vector2(40f, 40f);
			Vector2 inRoomPos = createPos + self.owner.game.cameras[0].pos;
			if (newTrigger is ICustomTrigger customTrigger)
			{
				customTrigger.InitAtPosition(inRoomPos);
			}

			self.RoomSettings.triggers.Add(newTrigger);
			newTrigger.panelPosition = createPos;
			self.Refresh();
			return;
		}
		orig(self, tp);
	}

	private static void TriggerPanel_ctor(ILContext il)
	{
		// Goal: init dev ui for custom trigger
		var c = new ILCursor(il);
		c.GotoNext(MoveType.AfterLabel, x => x.MatchCallOrCallvirt<TriggerPanel>(nameof(TriggerPanel.AddEventPanel)));

		c.Emit(OpCodes.Ldarg_0);
		c.Emit(OpCodes.Ldarg, 4);
		c.EmitDelegate(CreateCustomRep);

		static void CreateCustomRep(TriggerPanel triggerPanel, EventTrigger eventTrigger)
		{
			if (eventTrigger is ICustomTrigger customTrigger)
			{
				customTrigger.InitDevUI(triggerPanel);
			}
			else if (eventTrigger is SeeCreatureTrigger seeCreatureTrigger)
			{
				SeeCreatureTriggerDevUI.Init(triggerPanel, seeCreatureTrigger);
			}
		}
	}

	private static void TriggerPanel_AddEvent(On.DevInterface.TriggerPanel.orig_AddEvent orig, TriggerPanel self, TriggeredEvent.EventType evnt)
	{
		TriggeredEvent? newEvent = CreateTriggeredEvent(evnt);
		if (newEvent != null)
		{
			self.trigger.tEvent = newEvent;
			if (newEvent is ICustomEvent customEvent)
			{
				self.trigger.multiUse = customEvent.DefaultMultiUse;
			}
			self.RemoveSelectEventPanel();
			self.AddEventPanel();
			self.Refresh();
			return;
		}
		orig(self, evnt);
	}

	private static void TriggerPanel_AddEventPanel(On.DevInterface.TriggerPanel.orig_AddEventPanel orig, TriggerPanel self)
	{
		if (self.trigger.tEvent is ICustomEvent customEvent)
		{
			StandardEventPanel? panel = customEvent.InitDevUIPanel(self);
			if (panel != null)
			{
				self.eventPanel = panel;
				self.subNodes.Add(panel);
				return;
			}
		}
		orig(self);
	}

	#endregion
}
