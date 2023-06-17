using System;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;

internal class CGSlipperySlope : UpdatableAndDeletable
{
	private PlacedObject pObj;
	private CGSlipperySlopeData data => (CGSlipperySlopeData)pObj.data;
	public CGSlipperySlope(Room room, PlacedObject pObj)
	{
		this.room = room;
		this.pObj = pObj;
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		Rect aarect = new Rect(pObj.pos.x, pObj.pos.y, data.xhandle.magnitude, data.yhandle.magnitude);
		Vector2 centerOfRect = aarect.center;
		float angleOfTheDangle = RWCustom.Custom.VecToDeg(data.yhandle);
		foreach (var upd in room.updateList)
		{
			if (upd is PhysicalObject phys)
			{
				foreach (var chunk in phys.bodyChunks)
				{
					Vector2 rotatedPosition = RWCustom.Custom.RotateAroundVector(chunk.pos, pObj.pos, -angleOfTheDangle);
					Vector2 centerBias = (centerOfRect - rotatedPosition).normalized * 0.01f; ;
					Vector2 collisionCandidate = RWCustom.Custom.RotateAroundVector(aarect.GetClosestInteriorPoint(rotatedPosition), pObj.pos, angleOfTheDangle);
					centerBias = RWCustom.Custom.RotateAroundOrigo(centerBias, angleOfTheDangle);
					phys.PushOutOf(collisionCandidate + centerBias, 0f, -1);
				}
			}
		}
	}
	internal class CGSlipperySlopeData : ManagedData
	{
		private static readonly ManagedField[] customFields = new ManagedField[]
		{
				new Vector2Field("ev2", new Vector2(-100, -40), Vector2Field.VectorReprType.none),
				new DrivenVector2Field("ev3", "ev2", new Vector2(-100, -40), DrivenVector2Field.DrivenControlType.rectangle),
		};
		[BackedByField("ev2")]
		public Vector2 xhandle;
		[BackedByField("ev3")]
		public Vector2 yhandle;
		public CGSlipperySlopeData(PlacedObject owner) : base(owner, customFields) { }
	}
}
