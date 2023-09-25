using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RWCustom;
using UnityEngine;
using static RWCustom.Custom;
using static UnityEngine.Mathf;

namespace RegionKit.Modules.Particles.V1;
/// <summary>
/// Working unit for <see cref="RoomParticleSystem"/>.
/// </summary>
public class GenericParticle : CosmeticSprite
{
	internal static GenericParticle MakeNew(PMoveState start, ParticleVisualState visuals)
	{
		return new GenericParticle(start, visuals);
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="bSt">instantiation movement and fade in/out data</param>
	/// <param name="vSt">visuals package</param>
	public GenericParticle(PMoveState bSt, ParticleVisualState vSt) : base()
	{
		//throw null;
		vSt.atlasElement ??= "SkyDandelion";
		start = bSt;
		visuals = vSt;
		vel = DegToVec(bSt.dir).normalized * bSt.speed;
		pos = bSt.pos;
		lastPos = pos;
	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		//lastRot = rot;
		lifetime += 1f;
		//every frame, velocity is set to initial. Make sure to treat it accordingly in your custom behaviour modules
		var cpw = CurrentPower;
		var crd = currentRadius(cpw);
		var cLInt = (visuals.lightIntensity > 0f) ? Lerp(0f, visuals.lightIntensity, cpw) : 0f;
		if (!SetUpRan)
		{
			foreach (var m in Modules) m.Enable();
			OnCreate?.Invoke();
			if (visuals.lightIntensity > 0f && visuals.lightRadiusMax > 0f)
			{
				myLight = new(pos, false, visuals.lightColor, this)
				{
					requireUpKeep = true
				};
				myLight.HardSetAlpha(cLInt);
				myLight.HardSetRad(crd);
				myLight.flat = visuals.flat;
				room.AddObject(myLight);
			}
		}
		SetUpRan = true;
		ProgressLifecycle();
		if (myLight != null)
		{
			myLight.setAlpha = cLInt;
			myLight.setRad = crd;
			myLight.setPos = this.pos;
			myLight.stayAlive = true;
			myLight.color = visuals.lightColor;
			myLight.flat = this.visuals.flat;
		}
		vel = DegToVec(start.dir) * start.speed;
		OnUpdatePreMove?.Invoke();
		lastRot = rot;
		base.Update(eu);
		OnUpdatePostMove?.Invoke();
	}
	///<inheritdoc/>
	public override void Destroy()
	{
		OnDestroy?.Invoke();
		foreach (var m in Modules) m.Disable();
		base.Destroy();
	}

	#region modules
	/// <summary>
	/// Adds a new behaviour module to the particle
	/// </summary>
	public void AddModule(PBehaviourModule m)
	{
		Modules.Add(m);
	}
	/// <summary>
	/// Behaviour modules of this instance
	/// </summary>
	/// <returns></returns>
	public readonly List<PBehaviourModule> Modules = new();
	/// <summary>
	/// invoked near the end of every frame
	/// </summary>
	public event LifecycleFunction? OnUpdatePreMove;
	/// <summary>
	/// Invoked after base update call. Can be used to undo position changes.
	/// </summary>
	public event LifecycleFunction? OnUpdatePostMove;
	/// <summary>
	/// invoked on first frame
	/// </summary>
	public event LifecycleFunction? OnCreate;
	/// <summary>
	/// invoked when particle is about to be destroyed
	/// </summary>
	public event LifecycleFunction? OnDestroy;
	#endregion

	#region lifecycle
	/// <summary>
	/// Radius depending on current power
	/// </summary>
	protected virtual float currentRadius(float power) => Lerp(visuals.lightRadiusMin, visuals.lightRadiusMax, power);
	/// <summary>
	/// 0 to 1; represents how thick/transparent a particle is at the moment
	/// </summary>
	public virtual float CurrentPower
	{
		get
		{
			return phase switch
			{
				0 => Lerp(0f, 1f, (float)Progress / (float)GetPhaseLimit(0)),
				1 => 1f,
				2 => Lerp(1f, 0f, (float)Progress / (float)GetPhaseLimit(2)),
				_ => 0f,
			};
		}
	}
	/// <summary>
	/// every frame, ticks down the clock of a particle's birth, thrive and inevitable demise
	/// </summary>
	internal void ProgressLifecycle()
	{
		Progress++;
		if (Progress > GetPhaseLimit(phase))
		{
			Progress = 0;
			phase++;
		}
		if (phase > 2) this.Destroy();
	}
	/// <summary>
	/// The number of frames passed since beginning of current phase
	/// </summary>
	/// <value></value>
	public int Progress
	{
		get => _pr;
		private set { _pr = value; }
	}
	private int _pr;
	/// <summary>
	/// returns length of current life phase
	/// </summary>
	/// <param name="phase"></param>
	/// <returns></returns>
	private int GetPhaseLimit(byte phase)
	{
		return phase switch
		{
			0 => start.fadeIn,
			1 => start.lifetime,
			2 => start.fadeOut,
			_ => 0,
		};
	}
	/// <summary>
	/// What part of lifecycle particle is currently at. 0 - fadein, 1 - main lifetime, 2 - fadeout
	/// </summary>
	public byte phase = 0;
	#endregion

	/// <summary>
	/// starting movement parameters and fade in/out settings
	/// </summary>
	public PMoveState start;
	/// <summary>
	/// visual package - atlas element, container, etc
	/// </summary>
	public ParticleVisualState visuals;
	/// <summary>
	/// attached light source
	/// </summary>
	protected LightSource? myLight;
	/// <summary>
	/// Whether the first update has been executed
	/// </summary>
	protected bool SetUpRan = false;
	/// <summary>
	/// How much time the particle has left
	/// </summary>
	/// <value></value>
	public float lifetime { get; protected set; } = 0f;
	private float lastRot;
	/// <summary>
	/// to make your sprite go speen
	/// </summary>
	public float rot;
	//protected Vector2 VEL;

	#region IDrawable things
	///<inheritdoc/>
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{

		base.InitiateSprites(sLeaser, rCam);
		sLeaser.sprites = new FSprite[1];
		try
		{
			sLeaser.sprites[0] = new FSprite(visuals.atlasElement);
		}
		catch (Exception fue)
		{
			LogError($"Invalid atlas element {visuals.atlasElement}!");
			LogError(fue);
			sLeaser.sprites[0] = new FSprite("SkyDandelion", true);// .element = Futile.atlasManager.GetElementWithName("SkyDandelion");
		}
		room.game.rainWorld.Shaders.TryGetValue("Basic", out var sh);
		sLeaser.sprites[0].color = visuals.spriteColor;
		sLeaser.sprites[0].shader = sh;
		sLeaser.sprites[0].scale = visuals.scale;
		AddToContainer(sLeaser, rCam, rCam.ReturnFContainer(visuals.container.ToString()));
	}
	///<inheritdoc/>
	public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		base.AddToContainer(sLeaser, rCam, newContatiner);
	}
	///<inheritdoc/>
	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		base.ApplyPalette(sLeaser, rCam, palette);
	}
	///<inheritdoc/>
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		var cpos = Vector2.Lerp(lastPos, pos, timeStacker);
		sLeaser.sprites[0].SetPosition(cpos - camPos);
		sLeaser.sprites[0].alpha = CurrentPower;
		sLeaser.sprites[0].rotation = LerpAngle(lastRot, rot, timeStacker);
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}
	#endregion

	/// <summary>
	/// Use this to indicate how computationally heavy is your <see cref="GenericParticle"/> derivative. Used to smoothen loading process.
	/// </summary>
	public virtual float ComputationalCost => 1f;
	/// <summary>
	/// Delegate for lifecycle update for behaviour modules
	/// </summary>
	public delegate void LifecycleFunction();
}

