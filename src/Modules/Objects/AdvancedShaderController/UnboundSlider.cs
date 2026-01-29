using DevInterface;
using UnityEngine;
using Watcher;

namespace RegionKit.Modules.Objects.AdvancedShaderController
{
	public class UnboundSlider : PositionedDevUINode
	{
		public string title;
		public float width;
		public bool restrict;
		public AxisHandle handle;
		private FSprite leftSprite, rightSprite, trackSprite;

		public Color TrackColor
		{
			get => trackSprite.color;
			set
			{
				trackSprite.color = value;
				handle.fSprites[1].color = value;
			}
		}

		public float Value
		{
			get => handle.value / width;
			set => handle.value = restrict ? Mathf.Clamp(value * width, 0, width) : value * width;
		}

		public UnboundSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, float value, bool restrict, string title) : base(owner, IDstring, parentNode, pos)
		{
			if (restrict)
			{
				value = Mathf.Clamp01(value);
			}

			this.title = title;
			this.width = width;
			this.restrict = restrict;

			// fSprites.Add(bgSprite = new FSprite("pixel") { anchorX = 0f, anchorY = 0f, scaleX = width, scaleY = 16f, color = Color.white, alpha = 0.5f });
			fSprites.Add(trackSprite = new FSprite("pixel") { anchorX = 0f, scaleX = width, color = Color.white });
			fSprites.Add(leftSprite = new FSprite("pixel") { anchorY = 0f, scaleY = 16f, color = Color.white });
			fSprites.Add(rightSprite = new FSprite("pixel") { anchorY = 0f, scaleY = 16f, color = Color.white });

			// owner.placedObjectsContainer.AddChild(bgSprite);
			owner.placedObjectsContainer.AddChild(leftSprite);
			owner.placedObjectsContainer.AddChild(rightSprite);
			owner.placedObjectsContainer.AddChild(trackSprite);

			var holder = new AxisHandleHolder(owner, "HandleHolder", this, new Vector2(0f, 8f));
			subNodes.Add(holder);
			holder.subNodes.Add(handle = new AxisHandle(owner, "Handle", holder, new Vector2(value * width, 0f), new Vector2(0f, 0f), AxisHandle.Axis.X, $"{title} ({value:0.0000})", restrict, 0f, width));
			handle.fSprites[1].shader = Custom.rainWorld.Shaders["ASAxisHandleLine"];
			handle.fSprites[0].MoveInFrontOfOtherNode(handle.fSprites[1]);
		}

		public override void Refresh()
		{
			base.Refresh();

			leftSprite.SetPosition(absPos + new Vector2(0.01f, 0.01f));
			rightSprite.SetPosition(absPos + new Vector2(0.01f + width, 0.01f));
			trackSprite.SetPosition(absPos + new Vector2(0.01f, 8.01f));

			handle.clamp = restrict;
			handle.max = width;
			handle.SetName($"{title} ({Value:0.0000})");
		}

		private class AxisHandleHolder : PositionedDevUINode
		{
			public AxisHandleHolder(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
			{
			}
		}
	}
}
