using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RegionKit.Modules.Particles;

/// <summary>
/// carries sprite settings
/// </summary>
public struct ParticleVisualState
{
	public string atlasElement;
	public string shader;
	public ContainerCodes container;
	public Color spriteColor;
	public Color lightColor;
	public float lightIntensity;
	public float lightRadiusMax;
	public float lightRadiusMin;
	public float affectedByDark;
	public bool flat;
	//todo: document in example json
	public bool submersible;
	public float scale;

	public ParticleVisualState(
		string aElm,
		string shader,
		ContainerCodes container,
		Color sCol,
		Color lCol,
		float lInt,
		float lRadMax,
		float lRadMin,
		float affByDark,
		bool flat,
		float scale)
	{
		this.scale = scale;
		this.atlasElement = aElm;
		this.shader = shader;
		this.container = container;
		this.spriteColor = sCol;
		this.lightColor = lCol;
		this.lightIntensity = lInt;
		this.lightRadiusMax = lRadMax;
		this.lightRadiusMin = lRadMin;
		this.affectedByDark = affByDark;
		this.flat = flat;
	}

	public static ParticleVisualState Blank { get; private set; }
		= new("SkyDandelion", "Basic", ContainerCodes.Items, new(1f, 1f, 1f), new(1f, 1f, 1f), 1f, 35f, 25f, 0f, false, 1f);
}
