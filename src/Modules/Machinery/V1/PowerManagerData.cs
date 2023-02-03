using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace RegionKit.Modules.Machinery.V1;

/// <summary>
/// Data for a room power modifier engine 
/// </summary>
public class PowerManagerData : ManagedData
{
	[FloatField("basePower", 0f, 1f, 1f, increment: 0.02f, displayName: "Base power")]
	internal float basePowerLevel;
	[EnumField<PowerMode>("mode", PowerMode.Overwrite)]
	internal PowerMode pm = PowerMode.Overwrite;

	public PowerManagerData(PlacedObject? owner) : base(owner!, new ManagedField[] { })
	{

	}

	/// <summary>
	/// How PowerManager interacts with base power level
	/// </summary>
	public enum PowerMode
	{
		///
		Add,
		///
		Multiply,
		///
		Overwrite
	}
}
