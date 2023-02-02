using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RegionKit.Modules.Particles.V1;

/// <summary>
/// carries a couple movement parameters for a particle
/// </summary>
public struct PMoveState
{
	/// <summary>
	/// Direction in degrees
	/// </summary>
	public float dir;
	/// <summary>
	/// Speed in pixels/frame
	/// </summary>
	public float speed;
	/// <summary>
	/// Fade in in frames
	/// </summary>
	public int fadeIn;
	/// <summary>
	/// Lifetime in frames
	/// </summary>
	public int lifetime;
	/// <summary>
	/// Fade out in frames
	/// </summary>
	public int fadeOut;
	/// <summary>
	/// Current position
	/// </summary>
	public Vector2 pos;
	///<inheritdoc/>
	public PMoveState(
		float dir,
		float speed,
		int fadeIn,
		int lifetime,
		int fadeOut,
		Vector2 pos)
	{
		this.dir = dir;
		this.speed = speed;
		this.fadeIn = fadeIn;
		this.lifetime = lifetime;
		this.fadeOut = fadeOut;
		this.pos = pos;
	}
}
