using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
//todo: light source radius and other bullshit
namespace RegionKit.Modules.Particles.V2;

/// <summary>
/// carries a couple movement parameters for a particle
/// </summary>
public struct ParticleState
{
	/// <summary>
	/// Indicates how many elements will LastPos and LastRot have.
	/// </summary>
	public const int STATE_BUFFER_SIZE = 10;
	public int index;
	public int age;
	public Phase phase;
	public int Progress;
	/// <summary>
	/// indicates whether change of state is slated for next frame.<para/>
	/// larger than 0 to toggle on, lesser than 0 to toggle off.
	/// </summary>
	public int stateChangeSlated;
	/// <summary>
	/// Particle heading, in degrees.
	/// </summary>
	public float dir;
	/// <summary>
	/// How much to move each frame.
	/// </summary>
	public float speed;
	/// <summary>
	/// How many degrees per frame particle rotates.
	/// </summary>
	public float rotSpeed;
	/// <summary>
	/// How many frames the fade in phase lasts.
	/// </summary>
	public int fadeIn;
	/// <summary>
	/// How many frames neutral phase lasts.
	/// </summary>
	public int lifetime;
	/// <summary>
	/// How many frames fade out phase lasts.
	/// </summary>
	public int fadeOut;
	/// <summary>
	/// Current position.
	/// </summary>
	public Vector2 pos;
	/// <summary>
	/// Previous positions. 0 is 1 frame ago, 1 is 2 frames ago etc.
	/// </summary>
	public Vector2[] lastPos = new Vector2[STATE_BUFFER_SIZE];

	/// <summary>
	/// Current rotation.
	/// </summary>
	public float rot;
	/// <summary>
	/// Previous rotations. 0 is 1 frame ago, 1 is 2 frames ago etc.
	/// </summary>
	public float[] lastRot = new float[STATE_BUFFER_SIZE];

	public ParticleState(
		int index,
		float dir,
		float speed,
		float rotSpeed,
		int fadeIn,
		int lifetime,
		int fadeOut,
		Vector2 pos)
	{
		this.age = 0;
		this.index = index;
		this.dir = dir;
		this.speed = speed;
		this.rotSpeed = rotSpeed;
		this.fadeIn = fadeIn;
		this.lifetime = lifetime;
		this.fadeOut = fadeOut;
		this.pos = pos;
		lastRot = new float[STATE_BUFFER_SIZE];
		lastPos = new Vector2[STATE_BUFFER_SIZE];
	}
	/// <summary>
	/// ran every frame while the particle is active.
	/// </summary>
	public void Update()
	{
		if (stateChangeSlated > 0) stateChangeSlated = 0;
		for (int i = STATE_BUFFER_SIZE - 1; i > 0; i--)
		{
			lastPos[i] = lastPos[i - 1];
			lastRot[i] = lastRot[i - 1];
		}
		lastPos[0] = pos;
		lastRot[0] = rot;
		pos = pos + DegToVec(dir) * speed;
		rot = (rot + rotSpeed) % 360f;
		age++;
		if (age > fadeIn + lifetime + fadeOut) stateChangeSlated = -1;
	}
	
	/// <summary>
	/// 0 to 1; represents how thick/transparent a particle is at the moment
	/// </summary>
	public float CurrentPower
	=> phase switch
	{
		Phase.In => Lerp(0f, 1f, (float)Progress / (float)GetPhaseLimit(Phase.In)),
		Phase.Life => 1f,
		Phase.Out => Lerp(1f, 0f, (float)Progress / (float)GetPhaseLimit(Phase.Out)),
		_ => 0f,
	};
	public int GetPhaseLimit(Phase phase)
	{
		return phase switch
		{
			Phase.In => fadeIn,
			Phase.Life => lifetime,
			Phase.Out => fadeOut,
			_ => 0,
		};
	}
	public enum Phase { In = 0, Life = 1, Out = 2 };
}
