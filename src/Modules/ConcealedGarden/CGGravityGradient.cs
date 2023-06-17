using System;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;
 

public class CGGravityGradient : UpdatableAndDeletable
{
        private readonly PlacedObject pObj;
	private CGGravityGradientData data => (CGGravityGradientData)pObj.data;


	public CGGravityGradient(Room room, PlacedObject pObj)
	{
		this.room = room;
            this.pObj = pObj;
        }

	public override void Update(bool eu)
	{
		base.Update(eu);
		for (int i = 0; i < this.room.physicalObjects.Length; i++)
		{
			for (int j = 0; j < this.room.physicalObjects[i].Count; j++)
			{
				if (this.room.physicalObjects[i][j] is Player)
				{
					this.room.gravity = Mathf.Lerp(data.gravityA, data.gravityB, Mathf.Pow(InverseLerp(pObj.pos, pObj.pos + data.handle, this.room.physicalObjects[i][j].bodyChunks[0].pos), data.exponent));

		//= Mathf.InverseLerp(700f, this.room.PixelHeight - 400f, this.room.physicalObjects[i][j].bodyChunks[0].pos.y);
				}
			}
		}
	}

	// https://answers.unity.com/questions/1271974/inverselerp-for-vector3.html
	public static float InverseLerp(Vector2 a, Vector2 b, Vector2 value)
	{
		Vector2 AB = b - a;
		Vector2 AV = value - a;
		return Mathf.Clamp01(Vector2.Dot(AV, AB) / AB.sqrMagnitude);
	}

	public class CGGravityGradientData : ManagedData
	{
#pragma warning disable 0649
		[FloatField("1g", 0f, 1f, 0.1f, 0.01f, displayName: "Gravity A")]
		public float gravityA;
		[FloatField("2g", 0f, 1f, 0.1f, 0.01f, displayName: "Gravity B")]
		public float gravityB;
		[FloatField("3x", 0.01f, 10f, 1f, 0.01f, displayName: "Exponent")]
		public float exponent;
		private static readonly ManagedField[] customFields = new ManagedField[]
		   {
				new Vector2Field("4h", new Vector2(-100, -40), Vector2Field.VectorReprType.line),
		   };
		[BackedByField("4h")]
		public Vector2 handle;
#pragma warning restore 0649
		public CGGravityGradientData(PlacedObject owner) : base(owner, customFields) { }
	}

}
