using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RWCustom;
using UnityEngine;
using static RWCustom.Custom;
using static SharedPhysics;

namespace RegionKit.Modules.Particles.V1;

/// <summary>
/// behaviour modules that can be slapped onto particles. Instantiated by <see cref="ParticleBehaviourProvider"/>s, passed by <see cref="RoomParticleSystem"/>s and used by <see cref="GenericParticle"/>s.
/// </summary>
public abstract class PBehaviourModule
{
	readonly GenericParticle owner;

	/// <summary>
	/// Use this to indicate how computationally heavy is your <see cref="PBehaviourModule"/> derivative. Used to smoothen loading process.
	/// </summary>
	public virtual float ComputationalCost => 0.15f;
	///<inheritdoc/>
	public PBehaviourModule(GenericParticle gp)
	{
		owner = gp;
	}
	/// <summary>
	/// Ran when module is added to a particle
	/// </summary>
	public abstract void Enable();
	/// <summary>
	/// Ran when owner is about to be destroyed
	/// </summary>
	public abstract void Disable();

	/// <summary>
	/// Applies wavy path effect to a particle
	/// </summary>
	public class Wavy : PBehaviourModule
	{
		///<inheritdoc/>
		public Wavy(GenericParticle gp, Modules.Machinery.OscillationParams osp) : base(gp)
		{
			wave = osp;
		}
		Modules.Machinery.OscillationParams wave;
		private void OwnerUdate()
		{
			owner.vel = RotateAroundOrigo(owner.vel, wave.Oscillator((owner.lifetime + wave.phase) * wave.frequency) * wave.amplitude);//+= PerpendicularVector(owner.vel).normalized * wave.oscm((owner.lifetime + wave.phase) * wave.frq) * wave.amp;//
		}
		///<inheritdoc/>
		public override void Disable()
		{
			owner.OnUpdatePreMove -= OwnerUdate;
		}
		///<inheritdoc/>
		public override void Enable()
		{
			owner.OnUpdatePreMove += OwnerUdate;
		}
		///<inheritdoc/>
		public override float ComputationalCost => base.ComputationalCost + 0.06f;
	}
	/// <summary>
	/// Example module: does nothing outside of triggering <see cref="ANTIBODY"/> instances
	/// </summary>
	public class AFFLICTION : PBehaviourModule
	{
		//public static AFFLICTION makeNew(GenericParticle gp) => new AFFLICTION(gp);
		///<inheritdoc/>
		public AFFLICTION(GenericParticle gp) : base(gp) { }
		///<inheritdoc/>
		public override void Disable() { }
		///<inheritdoc/>
		public override void Enable() { }
	}
	/// <summary>
	/// Makes the particle destroy any nearby particles with <see cref="AFFLICTION"/> when found
	/// </summary>
	public class ANTIBODY : PBehaviourModule
	{
		///<inheritdoc/>
		public ANTIBODY(GenericParticle gp) : base(gp) { }
		///<inheritdoc/>
		public override void Disable()
		{
			owner.OnUpdatePreMove -= owner_update;
		}
		///<inheritdoc/>
		public override void Enable()
		{
			owner.OnUpdatePreMove += owner_update;
		}

		private void owner_update()
		{
			for (int i = 9; i > 0; i--)
			{
				var tar = owner.room.updateList.RandomOrDefault();
				if (tar == owner) continue;
				if (tar != null && tar is GenericParticle particle && (owner.pos - particle.pos).magnitude < 40f)
					foreach (var mod in particle.Modules)
					{
						if (mod is AFFLICTION)
						{
							particle.Destroy();
							owner.Destroy();
							owner.room.AddObject
								(new ShockWave(owner.pos, 50f, 0.1f, 20));
							owner.room.PlaySound(SoundID.Seed_Cob_Pop, owner.pos, 0.7f, 1.3f);
						}
					}
			}
		}
		///<inheritdoc/>
		public override float ComputationalCost => 0.09f;
	}
	/// <summary>
	/// Makes the particle stay on surface
	/// </summary>
	public class AvoidWater : PBehaviourModule
	{
		///<inheritdoc/>
		public AvoidWater(GenericParticle gp) : base(gp)
		{
		}
		///<inheritdoc/>
		public override void Disable()
		{
			owner.OnUpdatePostMove -= actionCycle;
		}
		///<inheritdoc/>
		public override void Enable()
		{
			owner.OnUpdatePostMove += actionCycle;
		}
		///<inheritdoc/>
		internal void actionCycle()
		{
			var y = owner.room.FloatWaterLevel(owner.pos);
			if (owner.pos.y < y) owner.pos.y = y;
		}
	}
	/// <summary>
	/// Makes the particles avoid going through walls
	/// </summary>
	public class WallCollision : PBehaviourModule
	{	
		///<inheritdoc/>
		public WallCollision(GenericParticle gp) : base(gp)
		{

		}
		///<inheritdoc/>
		public override void Disable()
		{
			owner.OnUpdatePostMove -= PostMoveAct;
		}
		///<inheritdoc/>
		public override void Enable()
		{
			owner.OnUpdatePostMove += PostMoveAct;
		}
		///<inheritdoc/>
		protected virtual void PostMoveAct()
		{
			var cd = new TerrainCollisionData(owner.pos, owner.lastPos, owner.vel, 1f, default, false);
			cd = VerticalCollision(owner.room, cd);
			cd = HorizontalCollision(owner.room, cd);
			owner.pos = cd.pos;
			owner.vel = cd.vel;
		}
		///<inheritdoc/>
		public override float ComputationalCost => base.ComputationalCost + 0.06f;
	}
	/// <summary>
	/// Makes particles stick to surfaces
	/// </summary>
	public class StickToSurface : WallCollision
	{
		private Vector2 _cpos;
		private bool _stuck;
		///<inheritdoc/>
		public StickToSurface(GenericParticle gp) : base(gp)
		{

		}
		///<inheritdoc/>
		public override void Disable()
		{
			base.Disable();
			owner.OnUpdatePreMove -= preMoveAct;
		}
		///<inheritdoc/>
		public override void Enable()
		{
			base.Enable();
			owner.OnUpdatePreMove += preMoveAct;
		}

		private void preMoveAct()
		{
			if (_stuck) owner.vel = default;
			_cpos = owner.pos;
		}
		///<inheritdoc/>
		protected override void PostMoveAct()
		{
			if (_stuck)
			{
				owner.pos = _cpos;
				owner.lastPos = _cpos;
			}
			var op = owner.pos;
			base.PostMoveAct();
			if (op != owner.pos) _stuck = true;
		}
		///<inheritdoc/>
		public override float ComputationalCost => base.ComputationalCost + 0.07f;
	}
	/// <summary>
	/// Makes particles spin.
	/// </summary>
	public class Spin : PBehaviourModule
	{
		///<inheritdoc/>
		public Spin(GenericParticle gp, float angVb, Modules.Machinery.OscillationParams osp) : base(gp)
		{
			_myosp = osp;
			_angVelBase = angVb;
		}

		private readonly float _angVelBase;
		private Modules.Machinery.OscillationParams _myosp;
		///<inheritdoc/>
		public override void Disable()
		{
			owner.OnUpdatePreMove -= actionCycle;
		}
		///<inheritdoc/>
		public override void Enable()
		{
			owner.OnUpdatePreMove += actionCycle;
		}

		private void actionCycle()
		{
			owner.rot += _angVelBase + _myosp.Oscillator(owner.lifetime * _myosp.frequency) * _myosp.amplitude;
		}
		///<inheritdoc/>
		public override float ComputationalCost => base.ComputationalCost + 0.06f;
	}
}
