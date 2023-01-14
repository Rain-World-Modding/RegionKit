using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static RWCustom.Custom;
using static UnityEngine.Mathf;

namespace RegionKit.Modules.Machinery.V1;
public class SimplePiston : UpdatableAndDeletable, IDrawable
{
	public SimplePiston(Room rm, PlacedObject pobj) : this(rm, pobj, null) { }
	public SimplePiston(Room rm, PlacedObject? pobj, PistonData? mdt = null)
	{
		PO = pobj;
		this._assignedMData = mdt;
		__log.LogDebug($"({rm.abstractRoom.name}): Created simplePiston" + mdt == null ? "." : "as a part of an array.");
	}
	public override void Update(bool eu)
	{
		base.Update(eu);
		oldPos = currentPos;
		_lt += room.ElectricPower;
		currentPos = originPoint + DegToVec(effRot) * Shift;
	}

	internal PistonData mData
	{
		get
		{
			var r = _assignedMData ?? PO?.data as PistonData;
			if (r == null) { _bpd = _bpd ?? new PistonData(null); return _bpd; }
			return r;
		}
	}
	private PistonData? _bpd;
	private readonly PistonData? _assignedMData;

	internal readonly PlacedObject? PO;
	private double _lt = 0f;
	internal Vector2 originPoint => PO?.pos ?? _assignedMData?.forcePos ?? default;
	internal float effRot => mData.align ? ((int)mData.rotation / 45 * 45) : mData.rotation;
	internal float Shift
	{
		get
		{
			var res = mData.amplitude;
			Func<double, double> chosenFunc;
			switch (mData.opmode)
			{
			default:
			case OperationMode.Sinal:
				chosenFunc = Math.Sin;
				break;
			case OperationMode.Cosinal:
				chosenFunc = Math.Cos;
				break;
			}
			res *= (float)chosenFunc((_lt + mData.phase) * mData.frequency);
			return res;
		}
	}
	internal Vector2 currentPos;
	internal Vector2 oldPos;

	internal MachineryCustomizer? _mc;

	internal void GrabMC()
	{
		if (PO is null) return;
		_mc = _mc
			?? room.roomSettings.placedObjects.FirstOrDefault(
				x => x.data is MachineryCustomizer nmc && nmc.GetValue<MachineryID>("amID") == MachineryID.Piston && (x.pos - this.PO.pos).sqrMagnitude <= nmc.GetValue<Vector2>("radius").sqrMagnitude)?.data as MachineryCustomizer
			?? new MachineryCustomizer(null);
	}

	#region irawable things
	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		GrabMC();
		sLeaser.sprites = new FSprite[1];
		sLeaser.sprites[0] = new FSprite("pixel");
		this.AddToContainer(sLeaser, rCam, null);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		var pos = Vector2.Lerp(oldPos, currentPos, timeStacker);
		_mc?.BringToKin(sLeaser.sprites[0]);
		sLeaser.sprites[0].rotation = effRot + _mc?.addRot ?? 0f;
		sLeaser.sprites[0].x = pos.x - camPos.x;
		sLeaser.sprites[0].y = pos.y - camPos.y;
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{

	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		foreach (var fs in sLeaser.sprites) fs.RemoveFromContainer();
		try { (newContatiner ?? rCam.ReturnFContainer(_mc?.ContainerName ?? ContainerCodes.Items)).AddChild(sLeaser.sprites[0]); }
		catch { rCam.ReturnFContainer("Items").AddChild(sLeaser.sprites[0]); }

	}
	#endregion
}
