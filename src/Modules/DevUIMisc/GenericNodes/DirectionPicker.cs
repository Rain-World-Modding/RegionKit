using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes
{
	public class DirectionPicker : RectangularDevUINode
	{
		private float Radius => size.x / 2f;
		private readonly PositionedHolder holder;
		private readonly DirectionHandle handle;
		private readonly FSprite bgCircle;

		public Vector2 Dir => handle.Dir;

		public DirectionPicker(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float size, Vector2 defaultDir) : base(owner, IDstring, parentNode, pos, new Vector2(size, size))
		{
			bgCircle = new FSprite("Futile_White")
			{
				anchorX = 0f,
				anchorY = 0f,
				shader = owner.game.rainWorld.Shaders["VectorCircleFadable"],
				color = new Color(0f, 0f, 0.5f), // white with 0.5 alpha because idk why this shader is set up like this
			};
			fSprites.Add(bgCircle);
			owner.placedObjectsContainer.AddChild(bgCircle);

			holder = new PositionedHolder(owner, "Holder", this, Vector2.one * Radius);
			subNodes.Add(holder);

			handle = new DirectionHandle(owner, "Handle", this, defaultDir.normalized);
			holder.subNodes.Add(handle);
		}

		public override void Update()
		{
			base.Update();
			size.y = size.x;
		}

		public override void Refresh()
		{
			base.Refresh();
			bgCircle.scale = size.x;
			bgCircle.SetPosition(absPos);
			bgCircle.alpha = 2f / Radius;
		}

		private class DirectionHandle : Handle
		{
			private readonly DirectionPicker dirOwner;
			private readonly FSprite dirLine;

			public Vector2 Dir
			{
				get => pos.sqrMagnitude > 0f ? pos.normalized : Vector2.up;
				set => pos = value.normalized;
			}

			public DirectionHandle(DevUI owner, string IDstring, DirectionPicker parentNode, Vector2 defaultDir) : base(owner, IDstring, parentNode, defaultDir * parentNode.Radius)
			{
				dirOwner = parentNode;

				dirLine = new FSprite("pixel") { anchorY = 0f };
				fSprites.Add(dirLine);
				owner.placedObjectsContainer.AddChild(dirLine);

				fSprites[0].element = Futile.atlasManager.GetElementWithName("keyArrowB");
				fSprites[0].scale = 0.75f;
			}

			public override void Update()
			{
				base.Update();
				pos = Dir * dirOwner.Radius;
			}

			public override void Refresh()
			{
				base.Refresh();
				float angle = Custom.VecToDeg(Dir);

				dirLine.SetPosition(absPos);
				dirLine.scaleY = dirOwner.Radius;
				dirLine.rotation = angle;

				fSprites[0].rotation = angle;
			}
		}

		private class PositionedHolder(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : PositionedDevUINode(owner, IDstring, parentNode, pos);
	}
}
