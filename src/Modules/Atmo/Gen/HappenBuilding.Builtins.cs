using RegionKit.Modules.Atmo.Data;
using RegionKit.Modules.Atmo.Helpers;
using static RegionKit.Modules.Atmo.Atmod;
using LOG = BepInEx.Logging;
using TXT = System.Text.RegularExpressions;
using RND = UnityEngine.Random;
using UAD = UpdatableAndDeletable;
using IO = System.IO;

using RegionKit.Modules.Atmo.Body;
using static RegionKit.Modules.Atmo.API.V0;
using static RegionKit.Modules.Atmo.Body.HappenTrigger;
using static RegionKit.Modules.Atmo.Body.HappenAction;
using static RegionKit.Modules.Atmo.Data.VarRegistry;
using static UnityEngine.Mathf;

namespace RegionKit.Modules.Atmo.Gen;
public static partial class HappenBuilding
{
	internal static void __InitBuiltins()
	{
		foreach (Action initfun in new[] { __RegisterBuiltinActions, __RegisterBuiltinTriggers, __RegisterBuiltinMetafun })
		{
			try
			{
				initfun();
			}
			catch (Exception ex)
			{
				LogFatal($"HappenBuilding: Static init: " +
					$"failed to {initfun.Method}:" +
					$"\n{ex}");
			}
		}
	}
	#region triggers
#warning contributor notice: triggers
	//Place your trigger registration code here.
	//Do not remove this warning directive.
	internal static void __RegisterBuiltinTriggers()
	{
		AddNamedTrigger(new[] { "always" }, TMake_Always);
		AddNamedTrigger(new[] { "untilrain", "beforerain" }, TMake_UntilRain);
		AddNamedTrigger(new[] { "afterrain" }, TMake_AfterRain);
		AddNamedTrigger(new[] { "everyx", "every" }, TMake_EveryX);
		AddNamedTrigger(new[] { "maybe", "chance" }, TMake_Maybe);
		AddNamedTrigger(new[] { "flicker" }, TMake_Flicker);
		AddNamedTrigger(new[] { "karma", "onkarma" }, TMake_OnKarma);
		AddNamedTrigger(new[] { "visit", "playervisited", "playervisit" }, TMake_Visit);
		AddNamedTrigger(new[] { "fry", "fryafter" }, TMake_Fry);
		AddNamedTrigger(new[] { "after", "afterother" }, TMake_AfterOther);
		AddNamedTrigger(new[] { "delay", "ondelay" }, TMake_Delay);
		AddNamedTrigger(new[] { "playercount" }, TMake_PlayerCount);
		AddNamedTrigger(new[] { "difficulty", "ondifficulty", "campaign" }, TMake_Difficulty);
		AddNamedTrigger(new[] { "vareq", "varequal", "variableeq" }, TMake_VarEq);
		AddNamedTrigger(new[] { "varne", "varnot", "varnotequal" }, TMake_VarNe);
		AddNamedTrigger(new[] { "varmatch", "variableregex", "varregex" }, TMake_VarMatch);
		AddNamedTrigger(new[] { "ghost", "echo" }, TMake_EchoPresence);
		//todo: document all triggers below:
		//do not document:
		AddNamedTrigger(new[] { "thisbreaks" }, (args, rwg, ha) =>
		{
			NewArg when = args.AtOr(0, "eval");
			EventfulTrigger evt = new(ha, args);
			switch (((string)when))
			{
			case "upd": evt.On_Update += delegate { throw new Exception("Intentional exception on Update"); }; break;
			case "sru": evt.Is_Active += delegate { throw new Exception("Intentional exception on ShouldRun"); }; break;
			}
			return evt;
		});
	}
	private static HappenTrigger? TMake_ExpeditionEnabled(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		EventfulTrigger evt = new(ha, args)
		{
			Is_Active = (_) => ModManager.Expedition
		};
		return evt;
	}
	private static HappenTrigger? TMake_JollyEnabled(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		EventfulTrigger evt = new(ha, args)
		{
			Is_Active = (_) => ModManager.JollyCoop
		};
		return evt;
	}
	private static HappenTrigger? TMake_MMFEnabled(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		EventfulTrigger evt = new(ha, args)
		{
			Is_Active = (_) => ModManager.MMF
		};
		return evt;
	}
	private static HappenTrigger? TMake_MSCEnabled(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		EventfulTrigger evt = new(ha, args)
		{
			Is_Active = (_) => ModManager.MSC
		};
		return evt;
	}
	private static HappenTrigger? TMake_RemixModEnabled(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		if (args.Count < 1)
		{
			__NotifyArgsMissing(TMake_RemixModEnabled, "modid");
			return null;
		}
		EventfulTrigger evt = new(ha, args)
		{
			Is_Active = (EventfulTrigger trigger) => ModManager.ActiveMods.Any(x => x.name == trigger.args[0].GetValue<string>())
		};
		return evt;
	}
	private static HappenTrigger? TMake_EchoPresence(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		EventfulTrigger evt = new(ha, args)
		{
			Is_Active = (EventfulTrigger trigger) => trigger.owner?.Set.world.worldGhost is not null
		};
		return evt;
	}
	/// <summary>
	/// Creates a trigger that is active based on difficulty. 
	/// </summary>
	private static HappenTrigger? TMake_Difficulty(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		return new EventfulTrigger(ha, args)
		{
			Is_Active = (EventfulTrigger trigger) =>
			{
				foreach (NewArg arg in trigger.args)
				{
					arg.TryGetValue(out SlugcatStats.Name? name);
					if (name == trigger.owner!.Set.world.game.GetStorySession.characterStats.name) return true;
				}
				return false;
			},
		};
	}
	/// <summary>
	/// Creates a trigger that is active on given player counts.
	/// </summary>
	private static HappenTrigger? TMake_PlayerCount(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		return new EventfulTrigger(ha, args)
		{
			Is_Active = (EventfulTrigger trigger) => trigger.args.Select(x => (int)x).Contains(trigger.owner!.Set.world.game.Players.Count)
		};
	}
	/// <summary>
	/// Creates a new trigger that is active after set delay (in seconds). If one argument is given, the delay is static. If two+ arguments are given, delay is randomly selected between them. Returns null if zero args.
	/// </summary>
	private static HappenTrigger? TMake_Delay(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		if (args.Count < 1)
		{
			__NotifyArgsMissing(TMake_Delay, "delay/delaymin+delaymax");
			return null;
		}
		int delay = args.Count > 1 ? RND.Range(args[0].SecAsFrames, args[1].SecAsFrames) : args[0].SecAsFrames;
		LogInfo($"Set delay: {delay} ( {args.Select(x => x.SecAsFrames.ToString()).Stitch()} )");
		return new EventfulTrigger(ha, new() { delay })
		{
			Is_Active = (EventfulTrigger trigger) => trigger.owner!.Set.world.rainCycle.timer > trigger.args[0].SecAsFrames
		};
	}
	/// <summary>
	/// Creates a trigger that is active after another happen. First argument is target happen, second is delay in seconds.
	/// </summary>
	/// <returns>Null if no arguments.</returns>
	private static HappenTrigger? TMake_AfterOther(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		if (args.Count < 1)
		{
			__NotifyArgsMissing(TMake_AfterOther, "name");
			return null;
		}
		return new AfterOtherTrigger(ha, args);
	}

	public sealed class AfterOtherTrigger : HappenTrigger
	{
		internal Happen? target;
		internal string targetName => args.AtOr(0, "none").GetValue<string>();
		internal int delay => (int?)(args.AtOr(1, 3f).GetValue<float>() * 40) ?? 40;
		internal bool tarWasOn;
		internal bool amActive;
		internal List<int> switchOn = new();
		internal List<int> switchOff = new();
		public AfterOtherTrigger(Happen owner, ArgSet args) : base(owner, args)
		{
			//tar = owner.set.AllHappens.FirstOrDefault(x => x.name == tarname.Str);
		}
		public override void Update()
		{
			target ??= owner?.Set.AllHappens.FirstOrDefault(x => x.name == targetName);
			if (target is null) return;
			if (target.Active != tarWasOn)
			{
				if (target.Active)
				{
					switchOn.Add(delay);
				}
				else
				{
					switchOff.Add(delay);
				}
			}
			for (int i = 0; i < switchOn.Count; i++)
			{
				switchOn[i]--;
			}
			if (switchOn.FirstOrDefault() < 0)
			{
				switchOn.RemoveAt(0);
				amActive = true;
			}
			for (int i = 0; i < switchOff.Count; i++)
			{
				switchOff[i]--;
			}
			if (switchOff.FirstOrDefault() < 0)
			{
				switchOff.RemoveAt(0);
				amActive = false;
			}
			tarWasOn = target.Active;
		}
		public override bool Active() => amActive;
	}
	/// <summary>
	/// Creates a trigger that is active, but turns off for a while if the happen stays on for too long. First argument is max tolerable time, second is time it takes to recover.
	/// </summary>
	/// <returns></returns>
	private static HappenTrigger? TMake_Fry(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		return new FryTrigger(ha, args);
	}
	public class FryTrigger : HappenTrigger
	{
		int counter = 0;
		bool active = true;
		int limit => args.AtOr(0, 10f).SecAsFrames;
		int cd => args.AtOr(1, 15f).SecAsFrames;

		public FryTrigger(Happen owner, ArgSet args) : base(owner)
		{
		}

		public override void Update()
		{
			if (active && owner != null)
			{
				if (owner.Active) counter++; else { counter = 0; }
				if (counter > limit) { active = false; counter = cd; }
			}
			else
			{
				counter--;
				if (counter == 0) { active = true; counter = 0; }
			}
		}

		public override bool Active() => active;
	}
	/// <summary>
	/// Creates a trigger that activates once player visits one of the provided rooms.
	/// </summary>
	private static HappenTrigger? TMake_Visit(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		return new AfterVisitTrigger(ha, args);
	}
	public sealed class AfterVisitTrigger : HappenTrigger
	{
		private string[] rooms => args.Select(x => x.GetValue<string>()).ToArray();
		private bool visit = false;
		public AfterVisitTrigger(Happen owner, ArgSet args) : base(owner, args)
		{
		}
		public override void Update()
		{
			if (visit) return;
			foreach (AbstractCreature? player in owner!.Set.world.game.Players) if (rooms.Contains(player.Room.name))
				{
					visit = true;
				}
		}
		public override bool Active()
		{
			return visit;
		}
	}

	/// <summary>
	/// Creates a trigger that flickers on and off. Arguments are: min time on, max time on, min time off, max time off, start active (yes/no)
	/// </summary>
	private static HappenTrigger? TMake_Flicker(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		return new Flicker(ha, args);
	}
	public sealed class Flicker : HappenTrigger
	{
		private int minOn => args.AtOr(0, 5.0f).SecAsFrames;
		private int maxOn => args.AtOr(1, 5.0f).SecAsFrames;
		private int minOff => args.AtOr(2, 5.0f).SecAsFrames;
		private int maxOff => args.AtOr(3, 5.0f).SecAsFrames;
		private bool on;
		private int counter;

		public Flicker(Happen owner, ArgSet args) : base(owner, args)
		{
			ResetCounter(args.AtOr(4, true).GetValue<bool>());
		}
		private void ResetCounter(bool next)
		{
			on = next;
			counter = on switch
			{
				true => UnityEngine.Random.Range(minOn, maxOn),
				false => UnityEngine.Random.Range(minOff, maxOff),
			};
		}
		public override bool Active()
		{
			return on;
		}

		public override void Update() { if (counter-- < 0) ResetCounter(!on); }
	}

	/// <summary>
	/// Creates a trigger that is always active.
	/// </summary>
	private static HappenTrigger? TMake_Always(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		return new EventfulTrigger(ha, args)
		{
			Is_Active = (_) => true
		};
	}

	/// <summary>
	/// Creates a trigger that is active each cycle with a given chance (default 50%)
	/// </summary>
	private static HappenTrigger? TMake_Maybe(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		NewArg ch = args.AtOr(0, 0.5f);
		//float rv = RND.value;
		bool yes = RND.value < ch.GetValue<float>();
		//plog.DbgVerbose($"bet {yes}: {ch} / {rv}");
		return new EventfulTrigger(ha, new() { yes })
		{
			Is_Active = (EventfulTrigger trigger) => trigger.args[0].GetValue<bool>(),
		};
	}
	/// <summary>
	/// Creates a trigger that is active at given karma levels.
	/// </summary>
	private static HappenTrigger? TMake_OnKarma(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		return new EventfulTrigger(ha, args)
		{
			Is_Active = (EventfulTrigger trigger) =>
			{
				List<int> levels = new();
				foreach (NewArg op in trigger.args)
				{
					if (op.TryGetValue<int>(out int r)) levels.Add(r);
					string[]? spl = TXT.Regex.Split(op.GetValue<string>(), "\\s*-\\s*");
					if (spl.Length == 2)
					{
						int.TryParse(spl[0], out int min);
						int.TryParse(spl[1], out int max);
						for (int i = min; i <= max; i++) if (!levels.Contains(i)) levels.Add(i);
					}
				}
				return levels.Contains((trigger.owner!.Set.world.game.Players[0].realizedCreature as Player)?.Karma ?? 0);
			}
		};
	}
	/// <summary>
	/// Creates a trigger that is active before rain, with an optional delay (in seconds).
	/// </summary>
	private static HappenTrigger? TMake_UntilRain(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		return new EventfulTrigger(ha, args)
		{
			Is_Active = (EventfulTrigger trigger) => trigger.owner!.Set.world.rainCycle.TimeUntilRain + args.AtOr(0, 0)?.SecAsFrames >= 0
		};
	}
	/// <summary>
	/// Creates a trigger that is active after rain, with an optional delay (in seconds).
	/// </summary>
	private static HappenTrigger? TMake_AfterRain(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		return new EventfulTrigger(ha, args)
		{
			Is_Active = (EventfulTrigger trigger) => trigger.owner!.Set.world.rainCycle.TimeUntilRain + args.AtOr(0, 0).SecAsFrames <= 0
		};
	}
	/// <summary>
	/// Creates a trigger that activates once every X seconds.
	/// </summary>
	private static HappenTrigger? TMake_EveryX(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		if (args.Count < 1)
		{
			__NotifyArgsMissing(TMake_EveryX, "period");
			return null;
		}
		return new EveryXTrigger(ha, args);
	}

	public sealed class EveryXTrigger : HappenTrigger
	{
		public EveryXTrigger(Happen ow, ArgSet args) : base(ow, args)
		{
		}

		private int period => args[0].SecAsFrames;
		private int counter;
		public override bool Active()
		{
			return counter is 0;
		}
		public override void Update()
		{
			if (--counter < 0) counter = period;
		}
	}

	/// <summary>
	/// Creates a trigger that checks variable's equality against a given value. First argument is variable name, second is value to check against.
	/// </summary>
	private static HappenTrigger? TMake_VarEq(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		if (args.Count < 2)
		{
			__NotifyArgsMissing(TMake_VarEq, "varname/value");
			return null;
		}
		return new EventfulTrigger(ha, args)
		{
			Is_Active = (EventfulTrigger trigger) =>
			{
				NewArg tar = VarRegistry.GetVar(trigger.args[0].GetValue<string>(), trigger.owner!.Set.world);
				return tar.GetValue<string>() == trigger.args[1].GetValue<string>();
			},
		};
	}
	/// <summary>
	/// Creates a trigger that checks a variable's inequality against a given value. First argument is variable name, second is value to check against.
	/// </summary>
	public static HappenTrigger? TMake_VarNe(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		if (args.Count < 2)
		{
			__NotifyArgsMissing(TMake_VarNe, "varname/value");
			return null;
		}
		return new EventfulTrigger(ha, args)
		{
			Is_Active = (EventfulTrigger trigger) =>
			{
				NewArg tar = VarRegistry.GetVar(trigger.args[0].GetValue<string>(), trigger.owner!.Set.world);
				return tar.GetValue<string>() != trigger.args[1].GetValue<string>();
			},
		};
	}
	/// <summary>
	/// Creates a trigger that checks variable's match against a given regex. Responds to changing values.
	/// </summary>
	private static HappenTrigger? TMake_VarMatch(ArgSet args, RainWorldGame rwg, Happen ha)
	{
		if (args.Count < 2)
		{
			__NotifyArgsMissing(TMake_VarMatch, "varname/pattern");
			return null;
		}
		return new EventfulTrigger(ha, args)
		{
			Is_Active = (EventfulTrigger trigger) =>
			{
				NewArg tar = GetVar(trigger.args[0].GetValue<string>(), trigger.owner!.Set.world);
				return new TXT.Regex(trigger.args[1].GetValue<string>())?.IsMatch(tar.GetValue<string>()) ?? false;
			}
		};
	}
	#endregion
	#region actions
#warning contributor notice: actions
	//Add your action registration code here.
	//Do not remove this warning directive.
	private static void __RegisterBuiltinActions()
	{
		AddNamedAction(new[] { "playergrav", "playergravity" }, Make_Playergrav);
		AddNamedAction(new[] { "sound", "playsound" }, Make_Sound);
		AddNamedAction(new[] { "soundloop" }, Make_SoundLoop);
		AddNamedAction(new[] { "rumble", "screenshake" }, Make_Rumble);
		AddNamedAction(new[] { "karma", "setkarma" }, Make_SetKarma);
		AddNamedAction(new[] { "karmacap", "maxkarma", "setmaxkarma" }, Make_SetMaxKarma);
		AddNamedAction(new[] { "log", "message" }, Make_LogCall);
		AddNamedAction(new[] { "mark", "themark", "setmark" }, Make_Mark);
		AddNamedAction(new[] { "glow", "theglow", "setglow" }, Make_Glow);
		AddNamedAction(new[] { "raintimer", "cycleclock" }, Make_SetRainTimer);
		AddNamedAction(new[] { "palette", "changepalette" }, Make_ChangePalette);
		AddNamedAction(new[] { "setvar", "setvariable" }, Make_SetVar);
		AddNamedAction(new[] { "fling", "force" }, Make_Fling);
		AddNamedAction(new[] { "light", "tempglow" }, Make_Tempglow);
		AddNamedAction(new[] { "stun" }, Make_Stun);
		AddNamedAction(new[] { "lightning" }, Make_Lightning);
		AddNamedAction(new[] { "flash" }, Make_Flash);
		//todo: document all actions below:
		AddNamedAction(new[] { "soundlooppers" }, Make_SoundLoopPersistent);
		//do not document:
	}
	private static HappenAction Make_Flash(Happen ha, ArgSet args)
	{
		List<Guid> flashers = new();
		//fields are:
		//currentpow, oldpow, alive
		return new FlashAction(ha, args);
	}

	public class FlashAction : HappenAction
	{
		Dictionary<Room, RoomFlasher> flashers = new();
		NewArg color => args.AtOr(0, (Vector4)Color.white);
		NewArg maxopacity => args.AtOr(1, 0.9f);
		NewArg lerp => args["lerp"] ?? 0.04f;
		NewArg step => args["step"] ?? 0.01f;
		public FlashAction(Happen owner, ArgSet args) : base(owner, args)
		{
		}

		public override void RealizedUpdate(Room room)
		{
			if (!flashers.TryGetValue(room, out var myFlasher))
			{
				myFlasher = new(this);
				VerboseLog("Creating new room flasher " + myFlasher);
				flashers[room] = myFlasher;

				room.AddObject(myFlasher);
			}
			myFlasher.alpha = 1f;
		}

		public class RoomFlasher : UpdatableAndDeletable, IDrawable
		{
			public float alpha = 1f;
			float lastAlpha = 1f;
			bool init = true;
			public FlashAction action;

			public RoomFlasher(FlashAction action)
			{
				this.action = action;
			}

			public override void Update(bool eu)
			{
				base.Update(eu);
				lastAlpha = alpha;
				if (!init) alpha = Clamp(LerpAndTick(alpha, 0f, action.lerp.GetValue<float>(), action.step.GetValue<float>()), 0f, 1f);
				init = false;
				if (alpha == 0f) Destroy();
			}

			public override void Destroy()
			{
				action.flashers.Remove(this.room);
				base.Destroy();
			}
			public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				Vector4 vec = action.color.GetValue<Vector4>();
				Color color = new(vec.x, vec.y, vec.z);
				sLeaser.sprites = new FSprite[1];
				sLeaser.sprites[0] = new FSprite("pixel", true)
				{
					scaleX = 1366f,
					scaleY = 768f,
					anchorX = 0f,
					anchorY = 0f,
					color = color
				};
				AddToContainer(sLeaser, rCam, null);
			}
			public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
			{
				sLeaser.sprites[0].RemoveFromContainer();
				//todo: u sure it should be HUD or HUD2?
				newContatiner ??= rCam.ReturnFContainer("HUD");
				newContatiner.AddChild(sLeaser.sprites[0]);
				//leaser.sprites[0].addT
			}

			public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
			{
			}

			public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
			{
				sLeaser.sprites[0].alpha = Lerp(lastAlpha, alpha, timeStacker) * action.maxopacity.GetValue<float>();
			}
		}

	}


	private static HappenAction Make_Lightning(Happen ha, ArgSet args)
	{
		VerboseLog("Making lightning!");
		if (args.Count < 1)
		{
			__NotifyArgsMissing(source: Make_Lightning, "intensity");
		}

		return new EventfulAction<List<Lightning>>(ha, args, new())
		{
			On_RealizedUpdate = (action, room) =>
			{
				NewArg intensity = action.args[0], bkgonly = action.args.AtOr(1, false);

				if (room.lightning is null)
				{
					Lightning l = new(room: room, intensity.GetValue<float>(), bkgonly.GetValue<bool>());
					room.lightning = l;
					room.AddObject(l);
					action.persistent.Add(l);
				}
				room.lightning.intensity = intensity.GetValue<float>();
			},
			On_AbstractUpdate = (action) =>
			{
				List<Lightning> lightnings = action.persistent;
				for (int i = lightnings.Count - 1; i >= 0; i--)
				{
					if (lightnings[i].slatedForDeletetion)
					{
						lightnings.RemoveAt(i);
					}
					else if (!action.owner.Active)
					{
						Room room = lightnings[i].room;
						room.RemoveObject(lightnings[i]);
						room.lightning = null;
					}
				}
			}
		};
	}


	private static HappenAction Make_Stun(Happen ha, ArgSet args)
	{
		return new EventfulAction(ha, args)
		{
			On_RealizedUpdate = (action, room) =>
			{
				NewArg select = action.args["select", "filter", "who"] ?? ".*",
					duration = action.args["duration", "dur", "st"] ?? 10;
				for (int i = 0; i < room.updateList.Count; i++)
				{
					UpdatableAndDeletable? uad = room.updateList[i];
					if (uad is Creature c && TXT.Regex.IsMatch(c.Template.type.ToString(), select.GetValue<string>(), TXT.RegexOptions.IgnoreCase))
					{
						c.Stun(duration.GetValue<int>());
					}
				}
			}
		};
	}
	private static HappenAction Make_Tempglow(Happen ha, ArgSet args)
	{
		if (args.Count < 1)
		{
			__NotifyArgsMissing(Make_Tempglow, "color");
		}
		return new EventfulAction<Dictionary<Player, (Color, float?)>>(ha, args, new())
		{
			On_RealizedUpdate = (action, room) =>
			{
				var playersActive = action.persistent;
				NewArg argcol = action.args[0], radius = action.args.AtOr(1, 300f);

				foreach (UAD? uad in room.updateList)
				{
					if (uad is not Player p) continue;

					p.glowing = true;
					PlayerGraphics? pgraf = p.graphicsModule as PlayerGraphics;
					if (pgraf?.lightSource is null) continue;
					playersActive[p] = (pgraf.lightSource.color, pgraf.lightSource.setRad);
					pgraf.lightSource.color = argcol.GetValue<Vector4>().ToOpaqueCol();
					pgraf.lightSource.setRad = radius.GetValue<float>();
				}
			},
			On_AbstractUpdate = (action) =>
			{
				var playersActive = action.persistent;
				if (action.owner.Active) return;
				foreach (AbstractCreature? absp in action.owner.game.Players)
				{
					if (absp.realizedCreature is not Player p || p.slatedForDeletetion) continue;

					if (!playersActive.TryGetValue(p, out var value)) continue;

					p.glowing = action.owner.game.GetStorySession?.saveState.theGlow ?? false;
					if (p.graphicsModule is PlayerGraphics pgraf && pgraf.lightSource is not null)
					{
						pgraf.lightSource.setRad = value.Item2;
						pgraf.lightSource.color = value.Item1;
						if (!p.glowing)
						{
							pgraf.lightSource.stayAlive = false;
							pgraf.lightSource = null;
						}
					}

					playersActive.Remove(p);
				}
			}
		};
	}
	private static HappenAction Make_Fling(Happen ha, ArgSet args)
	{
		if (args.Count < 1)
		{
			__NotifyArgsMissing(Make_Fling, "force");
		}

		return new EventfulAction(ha, args)
		{
			On_RealizedUpdate = (action, rm) =>
			{
				NewArg force = action.args[0],
					filter = action.args["filter", "select"] ?? ".*",
					forceVar = action.args["variance", "var"] ?? 0f,
					spread = action.args["spread", "deviation", "dev"] ?? 0f;

				foreach (UpdatableAndDeletable? uad in rm.updateList)
				{
					if (uad is not PhysicalObject obj) continue;

					string objtype = obj.abstractPhysicalObject.type.ToString();
					string? crittype = (obj as Creature)?.Template.type.ToString();
					if (TXT.Regex.IsMatch(objtype, filter.GetValue<string>(), System.Text.RegularExpressions.RegexOptions.IgnoreCase)
					||
					(crittype is not null && TXT.Regex.IsMatch(crittype, filter.GetValue<string>(), System.Text.RegularExpressions.RegexOptions.IgnoreCase)))
					{
						foreach (BodyChunk ch in obj.bodyChunks)
						{
							ch.vel += (Vector2)force.GetValue<Vector4>();//RotateAroundOrigo((Vector2)(force.GetValue<Vector4>() * cvar.a), cvar.b);
						}
					}
				}
			}
		};
	}

	private static HappenAction Make_SoundLoopPersistent(Happen ha, ArgSet args)
	{
		if (args.Count == 0)
		{
			__NotifyArgsMissing(Make_SoundLoopPersistent, "soundid");
			//return;
		}
		if (args[0].GetValue<SoundID>().index == -1)
		{
			__NotifyArgsMissing(Make_SoundLoopPersistent, "soundid");
			//return;
		}
		return new PersistentSoundLoopAction(ha, args);
	}

	public class PersistentSoundLoopAction : HappenAction
	{
		SoundID? soundID => sid.GetValue<SoundID>();
		NewArg sid => args[0];
		NewArg vol => args["vol", "volume"] ?? 1f;
		NewArg pitch => args["pitch"] ?? 1f;
		NewArg pan => args["pan"] ?? 0f;
		NewArg limit => args["lim", "limit"] ?? float.PositiveInfinity;

		//so basically
		//you need to:
		//- keep 1 soundloop per camera
		//- make sure there is only one ever sound loop per camera
		System.Runtime.CompilerServices.ConditionalWeakTable<RoomCamera, PersistentSoundPlayer> soundPlayers = new();
		PersistentSoundPlayer getNewSoundPlayer(RoomCamera _cam)
		{
			DisembodiedLoopEmitter disembodiedEmitter = _cam.room.PlayDisembodiedLoop(soundID, vol.GetValue<float>(), pitch.GetValue<float>(), pan.GetValue<float>());
			disembodiedEmitter.requireActiveUpkeep = false;
			disembodiedEmitter.alive = true;
			VerboseLog($"Creating new persistent loop {_cam.room.abstractRoom.name} {soundID}");
			return new(disembodiedEmitter, true);
			//return disembodiedEmitter;
		}

		public PersistentSoundLoopAction(Happen owner, ArgSet args) : base(owner, args)
		{
		}

		public override void RealizedUpdate(Room room)
		{
			for (int i = 0; i < room.game.cameras.Length; i++)
			{
				RoomCamera cam = room.game.cameras[i];
				if (cam.room != room) continue;

				PersistentSoundPlayer soundPlayer = soundPlayers.GetValue(cam, getNewSoundPlayer);
				// soundPlayer._0.loop.soundStillPlaying = true;
				// soundPlayer._0.loop.alive = true;
				// soundPlayer.slatedForDeletetion = false;
				if (soundPlayer.room != room)
				{
					VerboseLog($"switching persistent soundloop for cam {i} from {soundPlayer.room?.abstractRoom.name} to {room.abstractRoom.name}");
					//soundPlayer?.RemoveFromRoom();
					room.AddObject(soundPlayer);
					soundPlayer.room = room;
				}
			}
		}

		public override void AbstractUpdate()
		{
			foreach (RoomCamera cam in owner.game.cameras)
			{
				if (!soundPlayers.TryGetValue(cam, out var existingplayer) || existingplayer is null) continue;
				if (!owner.AffectsRoom(existingplayer.room?.abstractRoom) || !owner.Active)
				{
					existingplayer.Destroy();
					soundPlayers.Remove(cam);
				}
				else
				{
					existingplayer.alive = true;
					existingplayer.loop.volume = owner.Active ? vol.GetValue<float>() : 0f;
					existingplayer.loop.pitch = pitch.GetValue<float>();
					existingplayer.loop.pan = pan.GetValue<float>();
				}
			}
		}

		public class PersistentSoundPlayer : UpdatableAndDeletable
		{
			public DisembodiedLoopEmitter loop;
			public bool alive;

			public PersistentSoundPlayer(DisembodiedLoopEmitter loop, bool alive)
			{
				this.loop = loop;
				this.alive = alive;
			}

			public override void Destroy()
			{
				loop.Destroy();
				base.Destroy();
			}

			public override void Update(bool eu)
			{
				base.Update(eu);
				if (!alive) Destroy();
				alive = false;
			}
		}
	}

	private static HappenAction Make_SoundLoop(Happen ha, ArgSet args)
	{
		//BUG: doesn't turn off when exiting into a room where the happen isnt active. need to kill them manually?
		//does not work in HI (???). does not automatically get discontinued when leaving an affected room.
		//Breaks with warp.
		if (args.Count == 0)
		{
			__NotifyArgsMissing(Make_SoundLoop, "soundid");
			//return;
		}
		int timeAlive = 0;
		List<Guid> soundPlayers = new();
		// VerboseLog($"Creating action soundloop {activePlayers.GetHashCode()}");
		// //bool wasActive = false;
		// //int timer = 0;
		// VerboseLog(args.Select(x => x.ToString()).Stitch());
		// //Dictionary<string, DisembodiedDynamicSoundLoop> soundloops = new();//hashes = new();
		// ha.On_RealUpdate += (rm) => {
		// 	if (timeAlive > limit.SecAsFrames) return;
		// 	foreach (UAD uad in rm.updateList) {
		// 		if (uad is RoomSoundPlayer pl && activePlayers.Contains(pl._2)) return;
		// 	}
		// 	RoomSoundPlayer player = null!;
		// 	player = new(null!, false, Guid.NewGuid()) {
		// 		room = rm,
		// 		onUpdate = (eu) => {
		// 			if (!ha.Active || timeAlive > limit.SecAsFrames) {
		// 				player.Destroy();
		// 				player._0.Stop();
		// 				return;
		// 			}
		// 			bool shouldMakeSound = player.room.BeingViewed;
		// 			Action? neededChange = (shouldMakeSound, player._1) switch {
		// 				(true, false) => player._0.Start,
		// 				(false, true) => player._0.Stop,
		// 				_ => null
		// 			};
		// 			neededChange?.Invoke();
		// 			if (neededChange is not null) LogDebug($"{player._2} {neededChange.Method.Name}");
		// 			player._0.Update();
		// 			player._1 = shouldMakeSound;
		// 		}
		// 	};
		// 	player._0 = new(player) {
		// 		destroyClipWhenDone = false,
		// 		Volume = vol.F32,
		// 		Pan = pan.F32,
		// 		Pitch = pitch.F32,
		// 	};
		// 	LogDebug($"{rm.abstractRoom.name} creating new soundloop");
		// 	rm.AddObject(player);
		// 	activePlayers.Add(player._2);
		// };
		// ha.On_CoreUpdate += (rwg) => {
		// 	if (ha.Active) timeAlive++;
		// 	//lazy enum parsing
		// 	if (sid.Str != lastSid) {
		// 		sid.GetExtEnum(out soundid);
		// 	}
		// 	lastSid = sid.Str;
		// 	//wasActive = ha.Active;
		// 	if (!ha.Active) activePlayers.Clear();
		// };
		return new SoundLoopAction(ha, args);
	}

	public class SoundLoopAction : HappenAction
	{
		int timeAlive = 0;
		Dictionary<Room, RoomSoundPlayerr> soundPlayers = new();

		SoundID? soundid => sid.GetValue<SoundID>();
		NewArg sid => args[0];
		NewArg vol => args["vol", "volume"] ?? 1f;
		NewArg pitch => args["pitch"] ?? 1f;
		NewArg pan => args["pan"] ?? 0f;
		NewArg limit => args["lim", "limit"] ?? float.PositiveInfinity;
		public SoundLoopAction(Happen owner, ArgSet args) : base(owner, args)
		{
		}

		public override void RealizedUpdate(Room room)
		{
			if(soundPlayers.TryGetValue(room, out _))
			{
				return;
			}
			RoomSoundPlayerr mine = new(null!, false, this);
			mine.loop = new(mine)
			{
				destroyClipWhenDone = false,
				Volume = vol.GetValue<float>(),
				Pan = pan.GetValue<float>(),
				Pitch = pitch.GetValue<float>(),
			};
			mine.loop.sound = soundid;
			mine.loop.InitSound();
			soundPlayers[room] = mine;
			room.AddObject(mine);
		}

		public override void AbstractUpdate()
		{
			if (!owner.Active) soundPlayers.Clear();
		}

		public class RoomSoundPlayerr : UpdatableAndDeletable
		{
			public DisembodiedDynamicSoundLoop loop;
			bool alive;
			private readonly SoundLoopAction action;

			public RoomSoundPlayerr(DisembodiedDynamicSoundLoop loop, bool alive, SoundLoopAction action)
			{
				this.loop = loop;
				this.alive = alive;
				this.action = action;
			}

			public override void Update(bool eu)
			{
				base.Update(eu);
				if (!action.owner.Active || action.timeAlive > action.limit.SecAsFrames)
				{
					Destroy();
					loop.Stop();
					return;
				}
				bool shouldMakeSound = room.BeingViewed;
				Action? neededChange = (shouldMakeSound, alive) switch
				{
					(true, false) => null,//mine._0.Start,
					(false, true) => null,//mine._0.Stop,
					_ => null
				};
				neededChange?.Invoke();
				loop.Update();
				alive = shouldMakeSound;
			}
		}
	}


	private static HappenAction Make_Sound(Happen ha, ArgSet args)
	{
		if (args.Count == 0)
		{
			__NotifyArgsMissing(Make_Sound, "soundid");
			//return;
		}

		if (args[0].GetValue<SoundID>().index == -1)
		{
			LogError($"Happen {ha.name}: sound action: " +
				$"Invalid SoundID ({args[0]})");
			//return;
		}
		return new EventfulAction<int>(ha, args, 1)
		{
			On_RealizedUpdate = (action, room) =>
			{
				SoundID? soundid = action.args[0].GetValue<SoundID>();
				NewArg sid = action.args[0];
				string lastSid = sid.GetValue<string>();
				int cooldown = action.args["cd", "cooldown"]?.SecAsFrames ?? 2,
					limit = action.args["lim", "limit"]?.GetValue<int>() ?? int.MaxValue;
				NewArg
					vol = action.args["vol", "volume"] ?? 0.5f,
					pitch = action.args["pitch"] ?? 1f
					//pan = args["pan"]?.F32 ?? 1f
					;
				if (action.persistent != 0) return;
				if (limit < 1) return;
				for (int i = 0; i < room.updateList.Count; i++)
				{
					if (room.updateList[i] is Player p)
					{
						ChunkSoundEmitter? em = room.PlaySound(soundid ?? SoundID.HUD_Karma_Reinforce_Bump, p.firstChunk, false, vol.GetValue<float>(), pitch.GetValue<float>());
						action.persistent = cooldown;
						limit--;
						return;
					}
				}
			},
			On_AbstractUpdate = (action) =>
			{
				SoundID? soundid = action.args[0].GetValue<SoundID>();
				NewArg sid = action.args[0];
				string lastSid = sid.GetValue<string>();
				if (action.persistent > 0) action.persistent--;
				if (sid.GetValue<string>() != lastSid)
				{
					sid.TryGetValue(out soundid);
				}
				lastSid = sid.GetValue<string>();
			}
		};
	}
	private static HappenAction Make_Glow(Happen ha, ArgSet args)
	{
		return new EventfulAction(ha, args)
		{
			On_Init = (action) => 
			{
				SaveState? ss = action.owner.game.GetStorySession?.saveState;//./deathPersistentSaveData;
				if (ss is null) return;
				ss.theGlow = action.args.AtOr(0, true).GetValue<bool>();
			}
		};
	}
	private static HappenAction Make_Mark(Happen ha, ArgSet args)
	{
		return new EventfulAction(ha, args)
		{
			On_Init = (action) =>
			{
				DeathPersistentSaveData? ss = action.owner.game.GetStorySession?.saveState.deathPersistentSaveData;//./deathPersistentSaveData;
				if (ss is null) return;
				ss.theMark = action.args.AtOr(0, true).GetValue<bool>();
			}
		};
	}
	private static HappenAction Make_LogCall(Happen ha, ArgSet args)
	{
		return new EventfulAction<string>(ha, args, "")
		{
			On_Init = (action) =>
			{
				NewArg sev = action.args["sev", "severity"] ?? new NewArg(null, LOG.LogLevel.Message.ToString());
				sev.TryGetValue(out LOG.LogLevel sevVal);
				NewArg? onInit = action.args["init", "oninit"];
				if (onInit is not null)
				{
					UnityEngine.Debug.Log($"{action.owner.name}:\"{onInit.GetValue<string>()}\"");
					Log(sevVal, $"{action.owner.name}:\"{onInit.GetValue<string>()}\"");
				}
			},
			On_AbstractUpdate = (action) =>
			{
				NewArg sev = action.args["sev", "severity"] ?? new NewArg(null, LOG.LogLevel.Message.ToString());
				sev.TryGetValue(out LOG.LogLevel sevVal);
				if (sev.GetValue<string>() != action.persistent)
				{
					sev.TryGetValue(out sevVal);
				}
				action.persistent = sev.GetValue<string>();
			}
		};
	}
	private static HappenAction Make_SetRainTimer(Happen ha, ArgSet args)
	{
		if (args.Count < 1)
		{
			__NotifyArgsMissing(Make_SetRainTimer, "value");
			//return;
		}

		return new EventfulAction(ha, args)
		{
			On_Init = (action) =>
			{
				NewArg target = action.args[0];
				action.owner.game.world.rainCycle.timer = target.SecAsFrames;
			}
		};
	}
	private static HappenAction Make_SetKarma(Happen ha, ArgSet args)
	{
		if (args.Count < 1)
		{
			__NotifyArgsMissing(Make_SetKarma, "level");
			//return;
		}

		return new EventfulAction(ha, args)
		{
			On_Init = (action) =>
			{
				DeathPersistentSaveData? dpsd = action.owner.game?.GetStorySession?.saveState?.deathPersistentSaveData;
				if (dpsd is null || action.owner.game is null) return;
				NewArg ts = action.args[0];

				int karma = dpsd.karma;
				if (ts.Name is "add" or "+") karma += ts.GetValue<int>();
				else if (ts.Name is "sub" or "substract" or "-") karma -= ts.GetValue<int>();
				else karma = ts.GetValue<int>() - 1;
				karma = Clamp(karma, 0, 9);
				dpsd.karma = karma;
				VerboseLog($"Setting karma to {ts} (result: {dpsd.karma})");
				foreach (RoomCamera cam in action.owner.game.cameras) { cam?.hud.karmaMeter?.UpdateGraphic(); }
			}
		};
	}
	private static HappenAction Make_SetMaxKarma(Happen ha, ArgSet args)
	{
		return new EventfulAction(ha, args)
		{
			On_Init = (action) =>
			{
				DeathPersistentSaveData? dpsd = action.owner.game?.GetStorySession?.saveState?.deathPersistentSaveData;
				if (dpsd is null || action.owner.game is null) return;
				NewArg ts = action.args.AtOr(0, 0);
				int cap = dpsd.karmaCap;
				if (ts.Name is "add" or "+") cap += ts.GetValue<int>();
				else if (ts.Name is "sub" or "-") cap -= ts.GetValue<int>();
				else cap = ts.GetValue<int>() - 1;
				cap = Clamp(cap, 4, 9);
				dpsd.karmaCap = cap;
				VerboseLog($"Setting max karma to {ts} (result: {dpsd.karmaCap})");
				foreach (RoomCamera? cam in action.owner.game.cameras) { cam?.hud.karmaMeter?.UpdateGraphic(); }

			}
		};
	}
	private static HappenAction Make_Playergrav(Happen ha, ArgSet args)
	{
		return new EventfulAction(ha, args)
		{
			On_RealizedUpdate = (action, room) =>
			{
				NewArg frac = action.args.AtOr(0, 0.5f);
				for (int i = 0; i < room.updateList.Count; i++)
				{
					UAD? uad = room.updateList[i];
					if (uad is not Player p) continue;
					
					foreach (BodyChunk? bc in p.bodyChunks)
					{
						bc.vel.y += frac.GetValue<float>();
					}
				}
			}
		};
	}
	private static HappenAction Make_Rumble(Happen ha, ArgSet args)
	{
		return new EventfulAction(ha, args)
		{
			On_RealizedUpdate = (action, room) =>
			{
				NewArg intensity = action.args["int", "intensity"] ?? 1f, shake = action.args["shake"] ?? 0.5f;
				room.ScreenMovement(null, RND.insideUnitCircle * intensity.GetValue<float>(), shake.GetValue<float>());

			}
		};
	}
	private static HappenAction Make_ChangePalette(Happen ha, ArgSet args)
	{
		if (args.Count < 1)
		{
			__NotifyArgsMissing(Make_ChangePalette, "pal");
			//return;
		}
		return new EventfulAction<string[]?>(ha, args, null)
		{
			On_RealizedUpdate = (action, room) =>
			{
				string[]? lastRoomPerCam = action.persistent;
				if (lastRoomPerCam is null) return;
				//todo: support for fade palettes? make sure they dont fuck with rain
				NewArg palA = action.args[0];//["palA", "A", "1"];
				for (int i = 0; i < lastRoomPerCam.Length; i++)
				{
					RoomCamera? cam = room.game.cameras[i];
					if (cam.room != room || !room.BeingViewed || cam.AboutToSwitchRoom) continue;
					if (cam.room.abstractRoom.name != lastRoomPerCam[i])
					{
						if (palA is not null)
						{
							cam.ChangeMainPalette(palA.GetValue<int>());
							VerboseLog($"changing palette in {room.abstractRoom.name} to {palA.GetValue<int>()}");
						}
					}
				}
			},
			On_AbstractUpdate = (action) =>
			{
				string[]? lastRoomPerCam = action.persistent;
				if (lastRoomPerCam is null) lastRoomPerCam = new string[action.owner.game.cameras.Length];
				else 
					for (int i = 0; i < action.owner.game.cameras.Length; i++)
					{
						lastRoomPerCam[i] = action.owner.game.cameras[i].room.abstractRoom.name;
					}
			}
		};
	}
	private static HappenAction Make_SetVar(Happen ha, ArgSet args)
	{
		if (args.Count < 2)
		{
			__NotifyArgsMissing(Make_SetVar, "varname", "value");
			//return;
		}

		return new EventfulAction(ha, args)
		{
			On_Init = (action) =>
			{
				NewArg target = VarRegistry.GetVar(action.args[0].GetValue<string>(), action.owner.Set.world);
				var section = action.args[1].GetValue<SaveVarRegistry.DataSection>();

				SaveVarRegistry.SetArg(action.owner.game.world, section, target.Name, target.Raw);
			},
			On_RealizedUpdate = (action, room) =>
			{
				if (!action.args.AtOr(2, false).GetValue<bool>()) return;
				action.Init();
			}
		};
	}
	#endregion
	#region metafunctions
	internal static readonly TXT.Regex __FMT_Is = new("\\$FMT\\((.*?)\\)");
	internal static readonly TXT.Regex __FMT_Split = new("{.+?}");
	internal static readonly TXT.Regex __FMT_Match = new("(?<={).+?(?=})");
	internal static void __RegisterBuiltinMetafun()
	{
		AddNamedMetafun(new[] { "FMT", "FORMAT" }, MMake_FMT);
		AddNamedMetafun(new[] { "FILEREAD", "FILE" }, MMAke_FileRead);
		AddNamedMetafun(new[] { "WWW", "WEBREQUEST" }, MMake_WWW);
		AddNamedMetafun(new[] { "CURRENTROOM", "VIEWEDROOM" }, MMake_CurrentRoom);
		AddNamedMetafun(new[] { "SCREENRES", "RESOLUTION" }, MMake_ScreenRes);
		AddNamedMetafun(new[] { "OWNSAPP", "OWNSGAME" }, MMake_AppFound);
		//todo: document metafuncs below:
		AddNamedMetafun(new[] { "SHARPRAND", "SHARPRANDOM" }, MMake_SharpRandom);
		AddNamedMetafun(new[] { "LISTDIRECTORY", "ASSETDIR" }, MMake_ListDirectory);
		AddNamedMetafun(new[] { "RESOLVEFILEPATH", "ASSETPATH" }, MMake_ResolveFilepath);
		//do not document:
		AddNamedMetafun(new[] { "FILEREADWRITE", "TEXTIO" }, MMake_FileReadWrite);
		AddNamedMetafun(new[] { "GETVAR" }, MMake_GetSave);
	}
	private static NewArg? MMake_ListDirectory(string text, World world)
	{
		try
		{
			return new() { new Callback<string>(getter: () => AssetManager.ListDirectory(text).Stitch((x, y) => $"{x}:{y}")) };
		}
		catch
		{
			LogError($"INVALID assetpath {text}");
			return new();
		}
	}
	private static NewArg? MMake_ResolveFilepath(string text, World world)
	{
		try
		{
			return new() { new Callback<string>(getter: () => AssetManager.ResolveFilePath(text))};
		}
		catch
		{
			LogError($"INVALID assetpath {text}");
			return new();
		}
	}
	private static NewArg? MMake_AppFound(string text, World world)
	{
		uint.TryParse(text, out var id);
		return new() { new Callback<bool>(getter: () => Steamworks.SteamApps.BIsSubscribedApp(new(id))) };
	}
	private static NewArg? MMake_ScreenRes(string text, World world)
	{
		return new()
		{
			new Callback<Vector4>(getter: () =>
			{
				Resolution res = UnityEngine.Screen.currentResolution;
				return new Vector4(res.width, res.height, res.refreshRate, 0f);
			}),
			new Callback<string>(getter: () =>
			{
				Resolution res = UnityEngine.Screen.currentResolution;
				return $"{res.width}x{res.height}@{res.refreshRate}";
			})

		};
	}
	private static NewArg? MMake_CurrentRoom(string text, World world)
	{
		if (!int.TryParse(text, out int camnum)) camnum = 0;
		AbstractRoom? findAbsRoom(int cam) => world.game.cameras.AtOr(cam - 1, null)?.room?.abstractRoom;

		Vector2 nosize = new(-1, -1);
		return new()
		{
			new Callback<string>(getter: () => findAbsRoom(camnum)?.name ?? string.Empty),
			new Callback<float>(getter: () => findAbsRoom(camnum)?.index ?? -1),
			new Callback<Vector4>(getter: () => findAbsRoom(camnum)?.size.ToVector2() * 20f ?? nosize),
			new Callback<bool>(getter: () => false),
		};
	}
	private static NewArg? MMake_GetSave(string text, World world)
	{
		ArgSet args = new(text.Split(' '));

		NewArg getSaveArg(World world)
		{
			NewArg target = VarRegistry.GetVar(args[0].GetValue<string>(), world);
			var section = args[1].GetValue<SaveVarRegistry.DataSection>();
			return SaveVarRegistry.GetArg(world, section, target.Raw);
		};

		return new()
		{
			new RWCallback<string>(world, getter: (world) => getSaveArg(world).GetValue<string>()),
			new RWCallback<float>(world, getter: (world) => getSaveArg(world).GetValue<float>()),
			new RWCallback<int>(world, getter: (world) => getSaveArg(world).GetValue<int>()),
			new RWCallback<bool>(world, getter: (world) => getSaveArg(world).GetValue<bool>()),
			new RWCallback<Vector4>(world, getter: (world) => getSaveArg(world).GetValue<Vector4>()),
		};
	}

	private static NewArg? MMake_WWW(string text, World world)
	{
		WWW? www = new WWW(text);
		string? failed = null;
		return new()
		{
			new Callback<string>(getter: () =>
			{
				if (failed is not null) return $"ERROR:{failed}";
				try
				{
					return www.isDone ? www.text : "[LOADING]";
				}
				catch (Exception ex)
				{
					failed = ex.Message;
					return failed;
				}
			})
		};
	}
	private static NewArg? MMAke_FileRead(string text, World world)
	{
		IO.FileInfo fi = new(text);
		DateTime? lw = null;
		string? contents = null;
		return new()
		{
		new Callback<string>(getter: () =>
			{
				fi.Refresh();
				if (fi.Exists)
				{
					if (lw != fi.LastWriteTimeUtc)
					{
						using System.IO.StreamReader? sr = fi.OpenText();
						contents = sr?.ReadToEnd();
					}
					lw = fi.LastAccessTimeUtc;
				}
				return contents ?? string.Empty;
			})
		};
	}
	private static NewArg? MMake_FileReadWrite(string text, World world)
	{
		LogWarning($"CAUTION: {nameof(MMake_FileReadWrite)} DOES NO SAFETY CHECKS! Atmo developers are not responsible for any accidental damage by write");
		IO.FileInfo file = new(text);
		DateTime? lwt = null;//file.LastWriteTimeUtc;
		string pl = string.Empty;

		string ReadFromFile()
		{
			file.Refresh();
			try
			{
				if (file.Exists && file.LastWriteTimeUtc != lwt)
				{
					lwt = file.LastWriteTimeUtc;
					using IO.StreamReader sr = file.OpenText();
					pl = sr.ReadToEnd();
				}
			}
			catch (IO.IOException ex) { LogError($"Could not sync with file {file.FullName}: {ex}"); }
			return pl;
		}
		void WriteToFile(string val)
		{
			pl = val;
			using IO.StreamWriter sw = file.CreateText();
			sw.Write(val);
			sw.Flush();
		}
		return new() { new Callback<string>(getter: ReadFromFile, setter: WriteToFile) };
	}
	private static NewArg? MMake_FMT(string text, World world)
	{
		string[] bits = __FMT_Split.Split(text);
		TXT.MatchCollection names = __FMT_Match.Matches(text);
		NewArg[] variables = new NewArg[names.Count];
		for (int i = 0; i < names.Count; i++)
		{
			variables[i] = GetVar(names[i].Value, world);
		}
		int ind = 0;
		string format = bits.Stitch((x, y) => $"{x}{{{ind++}}}{y}");
		object[] getStrs()
		{
			return variables.Select(x => x.GetValue<string>()).ToArray();
		}
		return new() { new Callback<string>(getter: () => string.Format(format, getStrs())) };

	}
	private static NewArg? MMake_SharpRandom(string text, World world)
	{
		ArgSet args = new(text.Split(' '));
		(NewArg min, NewArg max) bounds = args switch
		{
		[NewArg a, NewArg b] => (a, b),
		[NewArg a] => (0f, a),
			_ => (0f, 1f)
		};
		Callback<float>.Getter getValue = () => UnityEngine.Mathf.Lerp(bounds.min.GetValue<float>(), bounds.max.GetValue<float>(), UnityEngine.Random.value);
		return new() { 
			new Callback<float>(getter: getValue),
			new Callback<int>(getter: () => (int)getValue()),
			new Callback<string>(getter: () => getValue().ToString()),
			new Callback<Vector4>(getter: () => new(getValue(), 0, 0, 0)),
		};
	}

	#endregion
	private static void __NotifyArgsMissing(Delegate source, params string[] args)
	{
		UnityEngine.Debug.LogWarning($"{nameof(HappenBuilding)}.{source.Method.Name}: Missing argument(s): {args.Stitch()}");
	}
}
