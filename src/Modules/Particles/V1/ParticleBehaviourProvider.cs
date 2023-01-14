using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RegionKit.Modules.Particles.V1;
/// <summary>
/// "Empty" data class for attaching <see cref="PBehaviourModule"/> descendants to newly created particles.
/// </summary>
public abstract class ParticleBehaviourProvider : ManagedData
{
	public abstract PBehaviourModule? GetNewForParticle(GenericParticle p);
	/// <summary>
	/// Affects sorting of providers in <see cref="RoomParticleSystem.Modifiers"/>. Higher value -> applied later.
	/// </summary>
	[IntegerField("order", 1, 100, 1, ManagedFieldWithPanel.ControlType.arrows, displayName: "Load order")]
	public int applyOrder;
	/// <summary>
	/// circle for checking whether a provider affects a given particle system
	/// </summary>
	[Vector2Field("p2", 30f, 30f, Vector2Field.VectorReprType.circle)]
	public Vector2 p2;
	/// <summary>
	/// 
	/// </summary>
	/// <param name="owner"></param>
	/// <param name="addFields">List of additional <see cref="ManagedField"/>. Can be null.</param>
	public ParticleBehaviourProvider(PlacedObject owner, List<ManagedField>? addFields) :
		base(owner, null)
	{

	}
	/// <summary>
	/// Meant to handle modifiers that don't require additional init data and thus can live without custom manageddata descendants
	/// </summary>
	public sealed class PlainModuleRegister : ParticleBehaviourProvider
	{
		static PlainModuleRegister()
		{
			RegisteredDelegates = new Dictionary<string, Func<GenericParticle, PBehaviourModule>>();
			RegisterPModType<PBehaviourModule.AFFLICTION>("affliction");
			RegisterPModType<PBehaviourModule.ANTIBODY>("antibody");
			RegisterPModType<PBehaviourModule.AvoidWater>("avoidwater");
			RegisterPModType<PBehaviourModule.wallCollision>("wallcollision");
			RegisterPModType<PBehaviourModule.stickToSurface>("sticktowalls");
		}
		public PlainModuleRegister(PlacedObject owner) : base(owner, null) { }
		private static readonly Dictionary<string, Func<GenericParticle, PBehaviourModule>> RegisteredDelegates;
		/// <summary>
		/// registers a new behaviormodule type with a specified key.
		/// </summary>
		/// <typeparam name="T">MUST have a constructor that takes argument set of (<see cref="GenericParticle"/>)! </typeparam>
		/// <param name="key">use it to spawn later</param>
		public static void RegisterPModType<T>(string key)
			where T : PBehaviourModule
		{
			if (string.IsNullOrEmpty(key)) { __log.LogError("Can not register a null/empty key!"); return; }//throw new ArgumentException();
			if (RegisteredDelegates.ContainsKey(key)) { __log.LogError($"Duplicate key: {key}!"); return; }
			RegisteredDelegates.Add
				(key, new Func<GenericParticle, PBehaviourModule>
				(x => { return (PBehaviourModule)Activator.CreateInstance(typeof(T), x); }));
			try
			{
				RegisteredDelegates[key](new GenericParticle(default, default));
				__log.LogMessage($"Registered plain module: {key}.");
			}
			catch (Exception e)
			{
				RegisteredDelegates.Remove(key);
				__log.LogError($"Could not register {key}. Exception on test instantiate.\n{e}");
			}
		}
		/// <summary>
		/// directly registers a spawn method via delegate. Still does a simple check to see if it throws.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="del"></param>
		public static void RegisterSpawnMethod(string key, Func<GenericParticle, PBehaviourModule> del)
		{
			try
			{
				del(new GenericParticle(default, default));
				RegisteredDelegates.Add(key, del);
				__log.LogMessage($"Registered plain module: {key}");
			}
			catch (Exception e)
			{
				__log.LogError($"Error registering delegate {del} under {key}: exception on test instatiate.\n{e}");
			}
		}
		/// <summary>
		/// key to get new instances of <see cref="PBehaviourModule"/> descendants by
		/// </summary>
		[StringField("sk", "affliction", displayName: "Addon key")]
		public string SelectedKey = "";
		public PlainModuleRegister(PlacedObject owner, List<ManagedField> addFields) : base(owner, addFields) { }

		public override PBehaviourModule? GetNewForParticle(GenericParticle p)
		{
			if (RegisteredDelegates.TryGetValue(SelectedKey, out var del)) { return del(p); }
			return null;
		}
	}
	/// <summary>
	/// applies a <see cref="PBehaviourModule.Wavy"/>
	/// </summary>
	public class WavinessProvider : ParticleBehaviourProvider
	{
		[FloatField("amp", 0.1f, 40f, 15f, displayName: "Amplitude")]
		public float amp;
		[FloatField("ampFluke", 0.1f, 40f, 0f, displayName: "Ampfluke")]
		public float ampFluke;
		[FloatField("frq", 0.02f, 5f, 1f, increment: 0.02f, displayName: "Frequency")]
		public float frq;
		[FloatField("frqFluke", 0.02f, 5f, 0f, increment: 0.02f, displayName: "Freq fluke")]
		public float frqFluke;
		[FloatField("phs", -5f, 5f, 0f, displayName: "Phase")]
		public float phase;
		[FloatField("phsFluke", -5f, 5f, 0f, displayName: "Phase fluke")]
		public float phaseFluke;

		protected Modules.Machinery.OscillationParams default_op => new(amp, frq, phase, Mathf.Sin);
		protected Modules.Machinery.OscillationParams dev_op => new(ampFluke, frqFluke, phaseFluke, Mathf.Cos);

		public Modules.Machinery.OscillationParams GetOscParams()
		{
			return default_op.Deviate(dev_op);
		}
		public WavinessProvider(PlacedObject owner) : base(owner, null) { }

		public override PBehaviourModule GetNewForParticle(GenericParticle p)
		{
			//PetrifiedWood.WriteLine("Creating a new module");
			return new PBehaviourModule.Wavy(p, GetOscParams());//throw new NotImplementedException();
		}
	}
	public class SpinProvider : WavinessProvider
	{
		[FloatField("avB", -60f, 60f, 0f, displayName: "base angular velocity")]
		public float angVec;


		public SpinProvider(PlacedObject owner) : base(owner)
		{
		}

		public override PBehaviourModule GetNewForParticle(GenericParticle p)
		{
			return new PBehaviourModule.Spin(p, angVec, GetOscParams());
		}
	}
}

