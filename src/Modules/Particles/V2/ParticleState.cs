using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RegionKit.Modules.Particles.V2;

/// <summary>
/// carries a couple movement parameters for a particle
/// </summary>
public struct ParticleState
{
	//>0 for toggle on,
	//<0 for toggle off
	public int stateChangeSlated;
	public int index;
	public float dir;
	public float speed;
	public int fadeIn;
	public int lifetime;
	public int fadeOut;
	public Vector2 pos;

	public ParticleState(
		int index,
		float dir,
		float speed,
		int fadeIn,
		int lifetime,
		int fadeOut,
		Vector2 pos)
	{
		this.index = index;
		this.dir = dir;
		this.speed = speed;
		this.fadeIn = fadeIn;
		this.lifetime = lifetime;
		this.fadeOut = fadeOut;
		this.pos = pos;
	}

	public void Slice(float ltF)
	{

	}
}
