using System.Globalization;
using DevInterface;
using Watcher;

namespace RegionKit.Modules.DevUIMisc.GenericNodes
{
	/// <summary>
	/// Slider with text input and allows unbound edges. Takes up 2 rows, so give 40 pixels of space with margins.
	/// </summary>
	public class ExtendedSlider : PositionedDevUINode, IDevUISignals
	{
		public float leftValue, rightValue;
		public bool restrict;
		protected float width;
		public string title;

		public bool ValueLocked => lockedValue.HasValue;

		protected float? lockedValue;
		protected DevUILabel titleLabel;
		protected FSprite sliderBkgSprite, sliderLineSprite;
		protected Button? inheritButton;
		protected StringControl? numberInput;
		protected AxisHandle handle;

		public float Value
		{
			get => lockedValue ?? Mathf.LerpUnclamped(leftValue, rightValue, handle.value / width);
			set
			{
				if (ValueLocked) return;

				if (restrict)
				{
					handle.value = Mathf.InverseLerp(leftValue, rightValue, value) * width;
				}
				else
				{
					handle.value = (value - leftValue) / (rightValue - leftValue) * width;
				}
				handle.Refresh();
				if (numberInput != null && ManagedStringControl.activeStringControl != numberInput)
				{
					numberInput.Text = numberInput.actualValue = value.ToString(CultureInfo.InvariantCulture);
				}
			}
		}

		public virtual string HandleText
		{
			get
			{
				return $"{title} ({Value:0.0000})";
			}
		}

		public ExtendedSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, float width, float titleWidth, 
			float leftValue = 0f, float rightValue = 1f, float initValue = 1f, bool restrict = false, bool inheritButton = false, bool lockValue = false) 
			: base(owner, IDstring, parentNode, pos)
		{
			this.leftValue = leftValue;
			this.rightValue = rightValue;
			this.restrict = restrict;
			this.width = width;
			this.title = title;
			if (lockValue)
			{
				lockedValue = initValue;
			}

			// Set up top layer of subnodes
			subNodes.Add(titleLabel = new DevUILabel(owner, "Title", this, new Vector2(0f, 20f), titleWidth, title));
			float inheritButtonSpace = inheritButton ? 35f : 0f;
			Vector2 numberInputPos = new Vector2(titleWidth + 5f + inheritButtonSpace, 20f);
			float numberInputWidth = width - titleWidth - 5f - inheritButtonSpace;
			if (!lockValue)
			{
				subNodes.Add(numberInput = new StringControl(owner, "Number", this, numberInputPos, numberInputWidth, initValue.ToString(CultureInfo.InvariantCulture), StringControl.TextIsFloat));
			}
			else
			{
				subNodes.Add(new DevUILabel(owner, "Number", this, numberInputPos, numberInputWidth, initValue.ToString(CultureInfo.InvariantCulture)));
			}

			if (inheritButton)
			{
				subNodes.Add(this.inheritButton = new Button(owner, "Inherit_Button", this, new Vector2(titleWidth + 5f, 20f), 30f, "Inhrt"));
			}

			// Set up bottom layer of subnodes
			fSprites.Add(sliderBkgSprite = new FSprite("pixel", true)
			{
				scaleY = 16f,
				scaleX = width,
				anchorX = 0f,
				anchorY = 0f,
				color = new Color(1f, 1f, 1f),
				alpha = 0.5f
			});
			fSprites.Add(sliderLineSprite = new FSprite("pixel", true)
			{
				scaleY = 2f,
				scaleX = width,
				anchorX = 0f,
				anchorY = 0f,
				color = new Color(0f, 0f, 0f)
			});
			foreach (FSprite sprite in fSprites)
			{
				Futile.stage.AddChild(sprite);
			}

			// Set up handle
			var holder = new AxisHandleHolder(owner, "HandleHolder", this, new Vector2(0f, 8f));
			subNodes.Add(holder);
			holder.subNodes.Add(handle = new AxisHandle(owner, "Handle", holder, new Vector2(initValue * width, 0f), new Vector2(0f, 0f), AxisHandle.Axis.X, title, restrict, 0f, width));
			handle.value = (initValue - leftValue) / (rightValue - leftValue) * width;
			handle.fSprites[1].shader = Custom.rainWorld.Shaders["ASAxisHandleLine"];
			//handle.fSprites[1].color = Color.black;
			handle.fSprites[1].anchorY = 0f;
			Futile.stage.AddChild(handle.fSprites[1]);
			handle.fSprites[1].MoveBehindOtherNode(sliderLineSprite);
			Futile.stage.AddChild(handle.fSprites[0]);
			Futile.stage.AddChild(handle.fLabels[0]);
		}

		public virtual void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			if (inheritButton != null && sender == inheritButton)
			{
				ClickedInheritButton();
			}
			else if ((type == StringControl.StringEdit || type == StringControl.StringFinish) && sender == numberInput)
			{
				if (float.TryParse(numberInput.actualValue, out float value))
				{
					Value = value;
				}
			}
		}

		public override void Refresh()
		{
			base.Refresh();

			if (lockedValue != null)
			{
				Value = lockedValue.Value;
			}

			sliderBkgSprite.SetPosition(absPos + new Vector2(0.01f, 0.01f));
			sliderLineSprite.SetPosition(absPos + new Vector2(0.01f, 7.01f));

			titleLabel.Text = title;
			handle.clamp = restrict;
			handle.SetName(HandleText);
		}

		public virtual void ClickedInheritButton()
		{
			//LogMessage($"{nameof(ExtendedSlider)}'s inherit button was clicked. Implementation needs to subclass and override {nameof(ClickedInheritButton)} to implement functionality.");

			// I was going to do a log message but I figure this will make it more clear
			throw new NotImplementedException($"{nameof(ExtendedSlider)} implementation needs to subclass and override {nameof(ClickedInheritButton)} to implement inherit functionality.");
		}

		protected sealed class AxisHandleHolder : PositionedDevUINode
		{
			public AxisHandleHolder(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
			{
			}
		}
	}
}
