using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static RWCustom.Custom;
using static UnityEngine.Mathf;

namespace RegionKit.Modules.Machinery.V1;

/// <summary>
/// A sprite moving in waves
/// </summary>
public class SimplePiston : UpdatableAndDeletable, IDrawable
{
	/// <summary>
	/// POM ctor
	/// </summary>
	public SimplePiston(Room rm, PlacedObject pobj) : this(rm, pobj, null) { }
	/// <summary>
	/// Primary ctor
	/// </summary>
	public SimplePiston(Room rm, PlacedObject? pobj, PistonData? assignedData = null)
	{
		_PO = pobj;
		this._assignedMData = assignedData;
		__logger.LogDebug($"({rm.abstractRoom.name}): Created simplePiston" + assignedData == null ? "." : "as a part of an array.");
	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);
		_oldPos = _currentPos;
		_lt += room.ElectricPower;
		_currentPos = _OriginPoint + DegToVec(_EffRot) * _Shift;
	}

	internal PistonData _PistonData
	{
		get
		{
			var r = _assignedMData ?? _PO?.data as PistonData;
			if (r == null) { _bpd = _bpd ?? new PistonData(null); return _bpd; }
			return r;
		}
	}
	private PistonData? _bpd;
	private readonly PistonData? _assignedMData;

	internal readonly PlacedObject? _PO;
	private double _lt = 0f;
	private Vector2 _OriginPoint => _PO?.pos ?? _assignedMData?.forcePos ?? default;
	private float _EffRot => _PistonData.align ? ((int)_PistonData.rotation / 45 * 45) : _PistonData.rotation;
	private float _Shift
	{
		get
		{
			var res = _PistonData.amplitude;
			Func<double, double> chosenFunc;
			switch (_PistonData.opmode)
			{
			default:
			case OperationMode.Sinal:
				chosenFunc = Math.Sin;
				break;
			case OperationMode.Cosinal:
				chosenFunc = Math.Cos;
				break;
			}
			res *= (float)chosenFunc((_lt + _PistonData.phase) * _PistonData.frequency);
			return res;
		}
	}
	private Vector2 _currentPos;
	private Vector2 _oldPos;

	internal MachineryCustomizer? _mc;

	private void _GrabMC()
	{
		if (_PO is null) return;
		_mc = _mc
			?? room.roomSettings.placedObjects.FirstOrDefault(
				x => x.data is MachineryCustomizer nmc && nmc.affectedMachinesID == MachineryID.Piston && (x.pos - this._PO.pos).sqrMagnitude <= nmc.radius.sqrMagnitude)?.data as MachineryCustomizer
			?? new MachineryCustomizer(null);
	}

	#region irawable things
	///<inheritdoc/>
	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		_GrabMC();
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new FSprite("pixel");
		this.AddToContainer(sLeaser, rCam, null);
	}
	///<inheritdoc/>
	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		var pos = Vector2.Lerp(_oldPos, _currentPos, timeStacker);
		_mc?.BringToKin(sLeaser.sprites[0]);
		sLeaser.sprites[0].rotation = _EffRot + _mc?.addedRotation ?? 0f;
		sLeaser.sprites[0].x = pos.x - camPos.x;
		sLeaser.sprites[0].y = pos.y - camPos.y;
	}
	///<inheritdoc/>
	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{

	}
	///<inheritdoc/>
	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		foreach (var fs in sLeaser.sprites) fs.RemoveFromContainer();
		try { (newContatiner ?? rCam.ReturnFContainer(_mc?.containerName ?? ContainerCodes.Items)).AddChild(sLeaser.sprites[0]); }
		catch { rCam.ReturnFContainer("Items").AddChild(sLeaser.sprites[0]); }

	}
	#endregion
}
