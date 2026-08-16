using DevInterface;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class BarbedWireRep : ConsumableRepresentation
	{
		public class BarbedWireHandle : Handle
		{
			public FSprite lineSprite;
			public BarbedWireHandle(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
			{
				fSprites.Add(lineSprite = new FSprite("pixel"));
				lineSprite.anchorY = 0f;
				if (owner != null)
				{
					Futile.stage.AddChild(lineSprite);
				}
			}

			public override void Refresh()
			{
				base.Refresh();
				MoveSprite(fSprites.IndexOf(lineSprite), (parentNode as BarbedWireRep).absPos);
				lineSprite.scaleY = pos.magnitude;
				lineSprite.scaleX = 1f;
				lineSprite.rotation = Custom.VecToDeg(pos);
			}

			public override void Move(Vector2 newPos)
			{
				base.Move(newPos);
				((parentNode as BarbedWireRep).pObj.data as BarbedWireData).endPos = pos;
			}
		}

		public BarbedWireRep(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
		{
			if (pObj.type == _Enums.BarbedWire)
			{
				subNodes[0].ClearSprites();
				subNodes[0] = null;
				subNodes[0] = new BarbedWireHandle(owner, "endPosHandle", this, (pObj.data as BarbedWireData).endPos);
			}
			else
			{
				subNodes.Insert(subNodes.Count - 1, new BarbedWireHandle(owner, "endPosHandle", this, (pObj.data as BarbedWireData).endPos));
			}
		}

		public override void Refresh()
		{
			if (pObj.type != _Enums.BarbedWire)
			{
				base.Refresh();
			}
			else
			{
				MoveLabel(0, absPos + new Vector2(20f, 20f));
				MoveSprite(0, absPos);
				for (int num = subNodes.Count - 1; num >= 0; num--)
				{
					subNodes[num].Refresh();
				}
			}
		}
	}
}
