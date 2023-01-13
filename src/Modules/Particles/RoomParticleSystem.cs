using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RWCustom;
using static UnityEngine.Mathf;
using static RWCustom.Custom;

namespace RegionKit.Particles
{
	/// <summary>
	/// particle spawner.
	/// </summary>
	public class RoomParticleSystem : UpdatableAndDeletable
	{
		/// <summary>
		/// Constructor used by MPO.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="room"></param>
		public RoomParticleSystem(PlacedObject owner, Room room) : this(owner, room, owner.pos, owner.data as ParticleSystemData, GenericParticle.MakeNew)
		{

		}
		/// <summary>
		/// Constructor that can be used for manual instantiation with a custom set of birth delegates
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="room"></param>
		/// <param name="births"></param>
		public RoomParticleSystem(PlacedObject owner, Room room, Vector2 oPos, ParticleSystemData npsd, params ParticleCreate[] births)
		{
			backupPSD = npsd;
			overridePos = oPos;
			Owner = owner;
			FetchVisualsAndBM(room);
			if (births != null) foreach (var d in births) BirthEvent += d;
			if (!PSD.doWarmup) return;
			int IdealTotalBootUpFrames = (int)(AverageLifetime() * 1.5f);
			DelayRequestedByMe = Min(Min(IdealTotalBootUpFrames, TotalForceFrameLimit));
			room.waitToEnterAfterFullyLoaded = Max(room.waitToEnterAfterFullyLoaded, DelayRequestedByMe);
			ForceFramesMultiplier = (int)(BaseComfortableFpF / AverageComputeCost() / ((ComfortableParticleDensity > AverageDensity()) ? (1f) : (AverageDensity() / ComfortableParticleDensity)));
			plog.LogMessage($"{IdealTotalBootUpFrames}, {AverageLifetime()}, {ForceFramesMultiplier}");
		}
		#region warmup setup;
		//this section has settings for "warmup" = faking constant activity and making it look like particle system works offscreen constantly
		//this is somewhat janky and causes noticeable load time delays
		//there are probably better ways to go about this, but i can't be bothered with them right now

		//max particle density before force rate starts decreasing
		public static int ComfortableParticleDensity = 35;
		//comfortable rate "force frames per frame" for self and particles created on warmup
		public static int BaseComfortableFpF = 200;
		//add load frames upper border
		public static int TotalForceFrameLimit = 25;
		#endregion
		//how many force updates should be ran per warmup frame
		readonly int ForceFramesMultiplier;
		int DelayRequestedByMe;
		public int AverageDensity()
		{
			return AverageLifetime() / ((PSD.minCooldown + PSD.maxCooldown) / 2);
		}
		public int AverageLifetime()
		{
			return PSD.fadeIn + PSD.lifeTime + PSD.fadeOut;
		}
		public float AverageComputeCost()
		{
			var TotalModCost = 1f;
			var dummy = new GenericParticle(default, default);
			foreach (var mod in Modifiers)
			{
				TotalModCost += Max(0f, mod.GetNewForParticle(dummy)?.ComputationalCost ?? 0f);
			}
			var pBirths = BirthEvent?.GetInvocationList() ?? new Delegate[0];
			float[] birthcosts = new float[pBirths.Length];
			float result;
			if (birthcosts.Length != 0)
			{
				float sum = 0f;
				for (int i = 0; i < birthcosts.Length; i++)
				{
					float ccost = ((GenericParticle)pBirths[i].DynamicInvoke(default(PMoveState), default(PVisualState))).ComputationalCost;
					sum += Max(1f, ccost);
				}
				sum /= birthcosts.Length;
				result = sum * TotalModCost;
			}
			else
			{
				return 1f;
			}
			return result;
		}

		readonly List<GenericParticle> forceupdatelist = new();
		public override void Update(bool eu)
		{
			if (room.waitToEnterAfterFullyLoaded > 0 && DelayRequestedByMe > 0)
			{
				//warmup
				DelayRequestedByMe--;
				for (int c = 0; c < ForceFramesMultiplier; c++)
				{
					var lccp = ProgressCreationCycle();
					if (lccp != null) forceupdatelist.Add(lccp);
					for (int cp = forceupdatelist.Count - 1; cp > -1; cp--)
					{
						forceupdatelist[cp].Update(true);
						if (forceupdatelist[cp].slatedForDeletetion) forceupdatelist.RemoveAt(cp);
					}
				}
			}
			else
			{
				ProgressCreationCycle();
			}

			base.Update(eu);
		}
		/// <summary>
		/// progresses cooldown and spawns things when necessary
		/// </summary>
		/// <returns>newly created <see cref="GenericParticle"/>, null if none was made</returns>
		protected virtual GenericParticle ProgressCreationCycle()
		{
			GenericParticle p = null;
			cooldown--;
			if (cooldown <= 0)
			{
				var PossibleBirths = BirthEvent?.GetInvocationList();
				if (PossibleBirths != null && PossibleBirths.Length > 0)
				{
					p = (GenericParticle)
						PossibleBirths.RandomOrDefault().DynamicInvoke(
							PSD.DataForNew(),
							Visuals.RandomOrDefault()?.DataForNew() ?? default);
					p.pos = PickSpawnPos();
					foreach (var provider in Modifiers)
					{
						var newmodule = provider.GetNewForParticle(p);
						if (newmodule == null) continue;
						p.addModule(newmodule);
					}
					room.AddObject(p);

				}
				cooldown = UnityEngine.Random.Range(PSD.minCooldown, PSD.maxCooldown);
			}
			return p;
		}

		protected ParticleSystemData PSD => Owner.data as ParticleSystemData ?? backupPSD;
		//use this if you want to have PSD without having an owner
		public ParticleSystemData backupPSD;

		protected PlacedObject Owner;

		//same as aboove
		public Vector2 overridePos;
		protected Vector2 MyPos => Owner?.pos ?? overridePos;

		protected int cooldown;
		/// <summary>
		/// Acquires references to all relevant <see cref="ParticleVisualCustomizer"/>s and <see cref="ParticleBehaviourProvider"/>s
		/// </summary>
		/// <param name="room"></param>
		protected virtual void FetchVisualsAndBM(Room room)
		{
			Visuals.Clear();
			Modifiers.Clear();
			for (int i = 0; i < room.roomSettings.placedObjects.Count; i++)
			{
				if (room.roomSettings.placedObjects[i].data is ParticleVisualCustomizer f_PVC
					&& (MyPos - f_PVC.owner.pos).sqrMagnitude < f_PVC.p2.sqrMagnitude)
				{ Visuals.Add(f_PVC); }
				if (room.roomSettings.placedObjects[i].data is ParticleBehaviourProvider f_BMD
					&& (MyPos - f_BMD.owner.pos).sqrMagnitude < f_BMD.p2.sqrMagnitude)
				{ Modifiers.Add(f_BMD); }
			}
			//sorted for apply order to work
			Modifiers.Sort(new Comparison<ParticleBehaviourProvider>(
				(x, y) =>
				{
					if (x.applyOrder > y.applyOrder) return 1;
					else if (x.applyOrder == y.applyOrder) return 0;
					else return -1;
				}));
		}
		protected readonly List<ParticleVisualCustomizer> Visuals = new();
		protected readonly List<ParticleBehaviourProvider> Modifiers = new();
		/// <summary>
		/// 
		/// </summary>
		/// <param name="suggestedStart">suggested starting move params</param>
		/// <param name="suggestedVis">suggested visuals </param>
		/// <returns></returns>
		public delegate GenericParticle ParticleCreate(PMoveState suggestedStart, PVisualState suggestedVis);
		/// <summary>
		/// A random subscriber is invoked whenever a particle needs to be created
		/// </summary>
		public event ParticleCreate BirthEvent;

		/// <summary>
		/// pulls a random tile and returns a random position within it
		/// </summary>
		/// <returns></returns>
		protected virtual Vector2 PickSpawnPos()
		{
			var tiles = PSD.ReturnSuitableTiles(room);
			if (tiles.Count == 0) { tiles.Add((MyPos / 20).ToIntVector2()); }
			var tile = tiles[UnityEngine.Random.Range(0, tiles.Count)];
			return new Vector2()
			{
				x = Lerp(tile.x * 20, (tile.x + 1) * 20, UnityEngine.Random.value),
				y = Lerp(tile.y * 20, (tile.y + 1) * 20, UnityEngine.Random.value),
			};
		}
	}
}
