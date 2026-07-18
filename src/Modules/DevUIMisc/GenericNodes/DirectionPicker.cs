using DevInterface;
using RegionKit.Modules.Iggy;

namespace RegionKit.Modules.DevUIMisc.GenericNodes
{
	public class DirectionPicker : RectangularDevUINode
	{
		private float Radius => size.x / 2f;
		private readonly PositionedHolder holder;
		private readonly DirectionHandle handle;
		private readonly FSprite? bgCircle;

		public Vector2 Dir => handle.Dir;

		public DirectionPicker(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float radius, Vector2 defaultDir, bool bg = true) : base(owner, IDstring, parentNode, pos, new Vector2(radius * 2f, radius * 2f))
		{
			if (bg)
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
			}

			holder = new PositionedHolder(owner, "Holder", this, new Vector2(radius, radius));
			subNodes.Add(holder);

			handle = new DirectionHandle(owner, "Handle", this, defaultDir.normalized);
			holder.subNodes.Add(handle);
		}

		public override void Update()
		{
			base.Update();
			size.y = size.x;
			holder.pos = size / 2f;
		}

		public override void Refresh()
		{
			base.Refresh();
			if (bgCircle != null)
			{
				bgCircle.scale = size.x;
				bgCircle.SetPosition(absPos);
				bgCircle.alpha = 2f / Radius;
			}
		}

		private class DirectionHandle : Handle, IGiveAToolTip
		{
			private readonly DirectionPicker dirOwner;
			private readonly FSprite dirLine;

			public Vector2 Dir
			{
				get => pos.sqrMagnitude > 0f ? pos.normalized : Vector2.up;
				set => pos = value.normalized * dirOwner.Radius;
			}

			public ToolTip? ToolTip => new ToolTip("Drag to pick a direction.", 5, this);

			public bool MouseOverMe => MouseOver;

			public DirectionHandle(DevUI owner, string IDstring, DirectionPicker parentNode, Vector2 defaultDir) : base(owner, IDstring, parentNode, defaultDir * parentNode.Radius)
			{
				dirOwner = parentNode;

				dirLine = new FSprite("pixel") { anchorY = 1f };
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
