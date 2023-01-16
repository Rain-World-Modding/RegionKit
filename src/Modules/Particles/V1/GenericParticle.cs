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
	public static GenericParticle MakeNew(PMoveState start, PVisualState visuals)
	{
		return new GenericParticle(start, visuals);
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="bSt">instantiation movement and fade in/out data</param>
	/// <param name="vSt">visuals package</param>
	public GenericParticle(PMoveState bSt, PVisualState vSt) : base()
	{
		//throw null;
		vSt.aElm ??= "SkyDandelion";
		start = bSt;
		visuals = vSt;
		vel = DegToVec(bSt.dir).normalized * bSt.speed;
		pos = bSt.pos;
		lastPos = pos;
	}

	public override void Update(bool eu)
	{
		//lastRot = rot;
		lifetime += 1f;
		//every frame, velocity is set to initial. Make sure to treat it accordingly in your custom behaviour modules
		var cpw = CurrentPower;
		var crd = cRad(cpw);
		var cLInt = (visuals.lInt > 0f) ? Lerp(0f, visuals.lInt, cpw) : 0f;
		if (!SetUpRan)
		{
			foreach (var m in Modules) m.Enable();
			OnCreate?.Invoke();
			if (visuals.lInt > 0f && visuals.lRadMax > 0f)
			{
				myLight = new(pos, false, visuals.lCol, this)
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
			myLight.color = visuals.lCol;
			myLight.flat = this.visuals.flat;
		}
		vel = DegToVec(start.dir) * start.speed;
		OnUpdatePreMove?.Invoke();
		lastRot = rot;
		base.Update(eu);
		OnUpdatePostMove?.Invoke();
	}

	public override void Destroy()
	{
		OnDestroy?.Invoke();
		foreach (var m in Modules) m.Disable();
		base.Destroy();
	}

	#region modules
	public void addModule(PBehaviourModule m) { Modules.Add(m); }
	public readonly List<PBehaviourModule> Modules = new();

	public delegate void lcStages();
	/// <summary>
	/// invoked near the end of every frame
	/// </summary>
	public event lcStages? OnUpdatePreMove;
	/// <summary>
	/// Invoked after base update call. Can be used to undo position changes.
	/// </summary>
	public event lcStages? OnUpdatePostMove;
	/// <summary>
	/// invoked on first frame
	/// </summary>
	public event lcStages? OnCreate;
	/// <summary>
	/// invoked when particle is about to be destroyed
	/// </summary>
	public event lcStages? OnDestroy;
	#endregion

	#region lifecycle

	protected virtual float cRad(float power) => Lerp(visuals.lRadMin, visuals.lRadMax, power);
	/// <summary>
	/// 0 to 1; represents how thick/transparent a particle is at the moment
	/// </summary>
	public virtual float CurrentPower
	{
		get
		{
			return phase switch
			{
				0 => Lerp(0f, 1f, (float)progress / (float)GetPhaseLimit(0)),
				1 => 1f,
				2 => Lerp(1f, 0f, (float)progress / (float)GetPhaseLimit(2)),
				_ => 0f,
			};
		}
	}
	/// <summary>
	/// every frame, ticks down the clock of a particle's birth, thrive and inevitable demise
	/// </summary>
	internal void ProgressLifecycle()
	{
		progress++;
		if (progress > GetPhaseLimit(phase))
		{
			progress = 0;
			phase++;
		}
		if (phase > 2) this.Destroy();
	}
	public int progress
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
	public byte phase = 0;
	#endregion

	/// <summary>
	/// starting movement parameters and fade in/out settings
	/// </summary>
	public PMoveState start;
	/// <summary>
	/// visual package - atlas element, container, etc
	/// </summary>
	public PVisualState visuals;
	/// <summary>
	/// attached light source
	/// </summary>
	protected LightSource? myLight;
	protected bool SetUpRan = false;
	public float lifetime { get; protected set; } = 0f;
	private float lastRot;
	/// <summary>
	/// to make your sprite go speen
	/// </summary>
	public float rot;
	//protected Vector2 VEL;

	#region IDrawable things
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{

		base.InitiateSprites(sLeaser, rCam);
		sLeaser.sprites = new FSprite[1];
		try
		{
			sLeaser.sprites[0] = new FSprite(visuals.aElm);
		}
		catch (Exception fue)
		{
			__logger.LogError($"Invalid atlas element {visuals.aElm}!");
			__logger.LogError(fue);
			sLeaser.sprites[0] = new FSprite("SkyDandelion", true);// .element = Futile.atlasManager.GetElementWithName("SkyDandelion");
		}
		room.game.rainWorld.Shaders.TryGetValue("Basic", out var sh);
		sLeaser.sprites[0].color = visuals.sCol;
		sLeaser.sprites[0].shader = sh;
		sLeaser.sprites[0].scale = visuals.scale;
		AddToContainer(sLeaser, rCam, rCam.ReturnFContainer(visuals.container.ToString()));
	}
	public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		base.AddToContainer(sLeaser, rCam, newContatiner);
	}
	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		base.ApplyPalette(sLeaser, rCam, palette);
	}
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
}

