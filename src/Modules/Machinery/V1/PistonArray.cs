using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static RWCustom.Custom;

namespace RegionKit.Modules.Machinery.V1;
/// <summary>
/// A linear set of pistons
/// </summary>
public class PistonArray : UpdatableAndDeletable
{
	/// <summary>
	/// Constructor for POM
	/// </summary>
	/// <param name="rm"></param>
	/// <param name="obj"></param>
	public PistonArray(Room rm, PlacedObject obj)
	{
		this._PO = obj;
		this.room = rm;
		__logger.LogDebug($"({rm.abstractRoom.name}): Creating piston array...");
		_GeneratePistons();
	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (room.game?.devUI is null || _pistons is null) return;
		for (int i = 0; i < _pistons.Length; i++)
		{
			var pair = _pistons[i];
			var ndt = _PistonDataByIndex(i);
			ndt.BringToKin(pair.Item1);
		}
	}

	private readonly PlacedObject _PO;
	private PistonArrayData _PistonArrData => (_PO?.data as PistonArrayData)!;
	internal (PistonData, SimplePiston)[]? _pistons;
	internal MachineryCustomizer? _mc;
	internal Vector2 _P1 => _PO.pos;
	internal Vector2 _P2 => _PistonArrData.point2;
	internal float _BaseDir => VecToDeg(PerpendicularVector(_P2));

	#region child gen by index
	private PistonData _PistonDataByIndex(int index)
	{
		var res = new PistonData(null)
		{
			forcePos = _PosByIndex(index),
			rotation = _BaseDir + _PistonArrData.relativeRotation,
			//sharpFac = pArrData.sharpFac,
			align = _PistonArrData.align,
			phase = _PistonArrData.phaseIncrement * index,
			amplitude = _PistonArrData.amplitude,
			frequency = _PistonArrData.frequency,
			opmode = _OperModeByIndex(index),
		};
		return res;
	}

	private OperationMode _OperModeByIndex(int index)
	{
		return OperationMode.Sinal;
		//return (index % 2 == 0) ? OperationMode.Cosinal : OperationMode.Sinal;
	}
	private Vector2 _PosByIndex(int index)
	{
		return Vector2.Lerp(_P1, _P1 + _P2, (float)index / _PistonArrData.pistonCount);
	}
	private float _RotByIndex(int index) { return _BaseDir + _PistonArrData.relativeRotation; }
	#endregion
	private void _GrabMC()
	{
		_mc = _mc
			?? room.roomSettings.placedObjects.FirstOrDefault(
				x => x.data is MachineryCustomizer nmc && nmc.affectedMachinesID == MachineryID.Piston && (x.pos - this._PO.pos).sqrMagnitude <= nmc.radius.sqrMagnitude)?.data as MachineryCustomizer
			?? new MachineryCustomizer(null);
	}

	private void _CleanUpPistons()
	{
		if (_pistons != null)
		{
			foreach (var pair in _pistons) { pair.Item2.Destroy(); }
		}
		_pistons = null;
	}
	private void _GeneratePistons()
	{
		_GrabMC();
		_CleanUpPistons();
		_pistons = new (PistonData, SimplePiston)[_PistonArrData.pistonCount];
		for (int i = 0; i < _pistons.Length; i++)
		{
			var pdata = _PistonDataByIndex(i);
			var piston = new SimplePiston(room, null, pdata);
			this.room.AddObject(piston);
			piston._mc = this._mc;
			_pistons[i] = (pdata, piston);

		}
	}
	///<inheritdoc/>
	public override void Destroy()
	{
		base.Destroy();
		if (_pistons is null) return;
		foreach (var pair in _pistons) pair.Item2.Destroy();
	}
}
