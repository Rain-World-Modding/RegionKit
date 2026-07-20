using System.Runtime.CompilerServices;
using DevInterface;
using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace RegionKit.Modules.Triggers
{
	public class RegionBumpEvent : TriggeredEvent, ICustomEvent, IBeforeTriggerUpdate
	{
		public RegionBumpEvent() : base(_Enums.RegionBumpEvent)
		{
			Implementation.ApplyHooks();
		}

		private bool hasRegistered = false;
		public void PreTriggerUpdate(Room room)
		{
			if (!hasRegistered)
			{
				hasRegistered = true;
				Implementation.WaitRegionBump(room);
			}
		}

		public bool DefaultMultiUse => false;

		public void Fire(EventTrigger trigger, Room room)
		{
			Implementation.FireRegionBump(room);
		}

		public StandardEventPanel? InitDevUIPanel(TriggerPanel triggerPanel)
		{
			return null; // no special panel for this one
		}

		private static class Implementation
		{
			private static readonly ConditionalWeakTable<Room, object> _regionBumpTracker = new(); // basically just being used as a weak reference set
			private static bool _appliedHooks = false;
			internal static void ApplyHooks()
			{
				if (_appliedHooks) return;
				_appliedHooks = true;
				On.HUD.SubregionTracker.Update += SubregionTracker_Update;
				_ = new Hook(typeof(SubregionTracker).GetProperty(nameof(SubregionTracker.RegionBump)).GetGetMethod(), SubregionTracker_get_RegionBump);
			}

			private static void SubregionTracker_Update(On.HUD.SubregionTracker.orig_Update orig, SubregionTracker self)
			{
				Room? room = (self.textPrompt.hud.owner as Player)!.room;
				if (room == null || !_regionBumpTracker.TryGetValue(room, out _))
				{
					orig(self);
				}
				else
				{
					self.counter = 0;
				}
			}

			private static bool SubregionTracker_get_RegionBump(Func<SubregionTracker, bool> orig, SubregionTracker self)
			{
				Room? room = (self.textPrompt.hud.owner as Player)!.room;
				return orig(self) && (room == null || !_regionBumpTracker.TryGetValue(room, out _));
			}

			public static void WaitRegionBump(Room room)
			{
				_ = _regionBumpTracker.GetOrCreateValue(room);
			}

			public static void FireRegionBump(Room room)
			{
				_regionBumpTracker.Remove(room);
			}
		}
	}
}
