using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RegionKit.Particles
{
	/// <summary>
	/// carries a couple movement parameters for a particle
	/// </summary>
	public struct PMoveState
	{
		//TODO(thalber): finalize what goes here
		public float dir;
		public float speed;
		public int fadeIn;
		public int lifetime;
		public int fadeOut;
		public Vector2 pos;

		public PMoveState(float dir, float speed, int fadeIn, int lifetime, int fadeOut, Vector2 pos)
		{
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
}
