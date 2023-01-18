using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static UnityEngine.Mathf;

namespace RegionKit.Modules.Machinery.V1;
public class SimpleCog : UpdatableAndDeletable, IDrawable
{
	public SimpleCog(Room rm, PlacedObject pobj) : this(rm, pobj, null) { }
	public SimpleCog(Room rm, PlacedObject? pobj, SimpleCogData? acd = null)
	{
		_PO = pobj;
		this.room = rm;
		_assignedCD = acd;
		//PetrifiedWood.WriteLine($"Cog created in {rm.abstractRoom?.name}");
		__logger.LogDebug($"({rm.abstractRoom.name}): Created a Cog.");
	}
	public override void Update(bool eu)
	{
		base.Update(eu);
		_lt += room.ElectricPower;
		_lastRot = _rot;
		_rot = (_rot + _CAngVel) % 360f;
		//if (Input.GetKeyDown(KeyCode.F3)) Console.WriteLine($"{cAngVel} : {cData.angVelShiftAmp}, {cData.angVelShiftFrq}");
	}

	private SimpleCogData _CogData
	{
		get
		{
			var r = _assignedCD ?? _PO?.data as SimpleCogData;
			if (r == null) { _bcd = _bcd ?? new SimpleCogData(null); return _bcd; }
			return r;
		}
	}

	private SimpleCogData? _bcd;
	private readonly SimpleCogData? _assignedCD;
	private PlacedObject? _PO;

	private MachineryCustomizer? _customizer;
	private void _GrabMC()
	{
		if (this._PO is null) return;
		_customizer = _customizer
			?? room.roomSettings.placedObjects.FirstOrDefault(
				x => x.data is MachineryCustomizer nmc && nmc.GetValue<MachineryID>("amID") == MachineryID.Cog && (x.pos - this._PO.pos).sqrMagnitude <= nmc.GetValue<Vector2>("radius").sqrMagnitude)?.data as MachineryCustomizer
			?? new MachineryCustomizer(null);
	}

	private Vector2 _CPos => _PO?.pos ?? _assignedCD?.owner.pos ?? default;
	private float _lt;
	private float _lastRot;
	private float _rot;
	private float _CAngVel
	{
		get
		{
			var res = Lerp(0f, _CogData.baseAngVel, room.ElectricPower);
			Func<float, float> targetFunc;
			switch (_CogData.opmode)
			{
			default:
			case OperationMode.Sinal:
				targetFunc = Sin;
				break;
			case OperationMode.Cosinal:
				targetFunc = Cos;
				break;
			}
			res += _CogData.angVelShiftAmp * targetFunc(_lt * _CogData.angVelShiftFrq);
			res = Lerp(0f, res, room.ElectricPower);
			return res;
		}
	}

	#region idrawable things
	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		_GrabMC();
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new FSprite("ShelterGate_cog");
		AddToContainer(sLeaser, rCam, null!);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		sLeaser.sprites[0].SetPosition(_CPos - camPos);
		_customizer?.BringToKin(sLeaser.sprites[0]);
		sLeaser.sprites[0].rotation = LerpAngle(_lastRot, _rot, timeStacker);

	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{

	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		foreach (var fs in sLeaser.sprites) fs.RemoveFromContainer();
		try { (newContatiner ?? rCam.ReturnFContainer(_customizer?.ContainerName ?? ContainerCodes.Items)).AddChild(sLeaser.sprites[0]); }
		catch { rCam.ReturnFContainer("Items").AddChild(sLeaser.sprites[0]); }
	}
	#endregion
}
