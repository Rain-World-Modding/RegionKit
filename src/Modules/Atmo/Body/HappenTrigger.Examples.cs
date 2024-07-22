using RegionKit.Modules.Atmo.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RegionKit.Modules.Atmo.Data;

namespace RegionKit.Modules.Atmo.Body;

public partial class HappenTrigger
{

	#region builtins
#pragma warning disable CS1591
	/// <summary>
	/// Sample trigger, works after rain starts. Supports an optional delay (in seconds.)
	/// <para>
	/// Example use: <code></code>
	/// </para>
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class AfterRain : NeedsRWG
	{
		public AfterRain(RainWorldGame game, Happen ow, NewArg? delay = null) : base(game, ow)
		{
			this.delay = (int?)(delay?.GetValue<float>() * 40) ?? 0;
		}
		private readonly int delay;
		public override bool Active()
		{
			return game.world.rainCycle.TimeUntilRain + delay <= 0;
		}
	}
	/// <summary>
	/// Sample trigger, true until rain starts. Supports an optional delay.
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class BeforeRain : NeedsRWG
	{
		public BeforeRain(RainWorldGame game, Happen ow, NewArg? delay = null) : base(game, ow)
		{
			this.delay = (int?)(delay?.GetValue<float>() * 40) ?? 0;
		}
		private readonly int delay;
		public override bool Active()
		{
			return game.world.rainCycle.TimeUntilRain + delay >= 0;
		}
	}
	/// <summary>
	/// Sample trigger, fires every X frames.
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class EveryX : HappenTrigger
	{
		public EveryX(NewArg x, Happen ow) : base(ow)
		{
			period = (int?)(x?.GetValue<float>() * 40) ?? 30;
		}

		private readonly int period;
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
	/// Upon instantiation, rolls with given chance. If successful, stays on always.
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class Maybe : HappenTrigger
	{
		public Maybe(NewArg chance)
		{
			yes = UnityEngine.Random.value < chance.GetValue<float>();
		}
		private readonly bool yes;
		public override bool Active()
		{
			return yes;
		}
	}
	/// <summary>
	/// Turns on and off periodically.
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class Flicker : HappenTrigger
	{
		private readonly int minOn;
		private readonly int maxOn;
		private readonly int minOff;
		private readonly int maxOff;
		private bool on;
		private int counter;

		public Flicker(NewArg minOn, NewArg maxOn, NewArg minOff, NewArg maxOff, bool startOn = true)
		{
			this.minOn = (int?)(minOn?.GetValue<float>() * 40) ?? 200;
			this.maxOn = (int?)(maxOn?.GetValue<float>() * 40) ?? 200;
			this.minOff = (int?)(minOff?.GetValue<float>() * 40) ?? 400;
			this.maxOff = (int?)(maxOff?.GetValue<float>() * 40) ?? 400;
			ResetCounter(startOn);
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
	/// Requires specific karma levels
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class OnKarma : NeedsRWG
	{
		private readonly List<int> levels = new();
		//private readonly List<>;
		public OnKarma(RainWorldGame game, ArgSet options, Happen? ow = null) : base(game, ow)
		{
			foreach (NewArg op in options)
			{
				if (int.TryParse(op.GetValue<string>(), out int r)) levels.Add(r);
				string[]? spl = System.Text.RegularExpressions.Regex.Split(op.GetValue<string>(), "\\s*-\\s*");
				if (spl.Length == 2)
				{
					int.TryParse(spl[0], out int min);
					int.TryParse(spl[1], out int max);
					for (int i = min; i <= max; i++) if (!levels.Contains(i)) levels.Add(i);
				}
			}
		}
		public override bool Active()
		{
			return levels.Contains((game.Players[0].realizedCreature as Player)?.Karma ?? 0);
		}
	}
	/// <summary>
	/// Activates after any player visits a specific set of rooms.
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class AfterVisit : NeedsRWG
	{
		private readonly string[] rooms;
		private bool visit = false;
		public AfterVisit(RainWorldGame game, ArgSet roomnames) : base(game)
		{
			rooms = roomnames.Select(x => x.GetValue<string>()).ToArray();//roomnamesseWhere;
		}
		public override void Update()
		{
			if (visit) return;
			foreach (AbstractCreature? player in game.Players) if (rooms.Contains(player.Room.name))
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
	/// Fries and goes inactive for a duration if the happen stays on for too long.
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class Fry : HappenTrigger
	{
		private readonly int limit;
		private readonly int cd;
		private int counter;
		private bool active;
		public Fry(NewArg limit, NewArg cd, Happen owner) : base(owner)
		{
			this.limit = (int)(limit.GetValue<float>() * 40f);
			this.cd = (int)(cd.GetValue<float>() * 40f);
			counter = 0;
			active = true;
		}
		public override bool Active()
		{
			return active;
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
	}
	/// <summary>
	/// Activates after another event is tripped, with a customizeable spinup/spindown delay.
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class AfterOther : HappenTrigger
	{
		internal Happen? tar;
		internal string tarname;
		internal int delay;
		internal bool tarWasOn;
		internal bool amActive;
		internal List<int> switchOn = new();
		internal List<int> switchOff = new();
		public AfterOther(Happen owner, NewArg tarname, NewArg delay) : base(owner)
		{
			this.delay = (int?)(delay.GetValue<float>() * 40) ?? 40;
			this.tarname = (string)tarname;
			//tar = owner.set.AllHappens.FirstOrDefault(x => x.name == tarname.Str);
		}
		public override void Update()
		{
			tar ??= owner?.Set.AllHappens.FirstOrDefault(x => x.name == tarname);
			if (tar is null) return;
			if (tar.Active != tarWasOn)
			{
				if (tar.Active)
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
			tarWasOn = tar.Active;
		}
		public override bool Active()
		{
			return amActive;
		}
	}
	/// <summary>
	/// Activates after a set delay.
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class AfterDelay : NeedsRWG
	{
		private readonly int delay;
		/// <summary>
		/// Creates an instance with delay in set bounds.
		/// </summary>
		/// <param name="dmin">Lower bound</param>
		/// <param name="dmax">Upper bound</param>
		/// <param name="game">RWG instance to check the clock</param>
		public AfterDelay(NewArg dmin, NewArg dmax, RainWorldGame game) : base(game)
		{
			delay = UnityEngine.Random.Range((int?)(dmin?.GetValue<float>() * 40f) ?? 0, (int?)(dmax?.GetValue<float>() * 40f) ?? 2400);
		}
		/// <summary>
		/// Creates an instance with static delay.
		/// </summary>
		/// <param name="d">Delay</param>
		/// <param name="rwg">RWG instance to check the clock.</param>
		public AfterDelay(NewArg d, RainWorldGame rwg) : base(rwg)
		{
			delay = (int?)(d?.GetValue<float>() * 40f) ?? 2400;
		}
		public override bool Active()
		{
			return game.world.rainCycle.timer > delay;
		}
	}
	/// <summary>
	/// Activates if player count is within given value
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class OnPlayerCount : NeedsRWG
	{
		private readonly int[] accepted;
		public OnPlayerCount(ArgSet args, RainWorldGame game) : base(game, null)
		{
			BangBang(args, nameof(args));
			BangBang(game, nameof(game));
			accepted = args.Select(x => (int)x).ToArray();
		}
		public override bool Active()
		{
			return accepted.Contains(game.Players.Count);
		}
	}
	/// <summary>
	/// Only activates on a given difficulty.
	/// </summary>
	[Obsolete("Replaced with EventfulTrigger variant.")]
	public sealed class OnDifficulty : NeedsRWG
	{
		private readonly bool enabled = false;
		public OnDifficulty(ArgSet args, RainWorldGame game, Happen? ow = null) : base(game, ow)
		{
			BangBang(args, nameof(args));
			BangBang(game, nameof(game));
			foreach (NewArg arg in args)
			{
				arg.TryGetValue(out SlugcatStats.Name? name);
				if (name == game.GetStorySession.characterStats.name) enabled = true;
			}
		}
		public override bool Active()
		{
			return enabled;//difficulties.Contains(game.GetStorySession.characterStats.name);
		}
	}
#pragma warning restore CS1591
	#endregion

}
