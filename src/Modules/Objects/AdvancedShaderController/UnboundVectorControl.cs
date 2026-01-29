using DevInterface;

namespace RegionKit.Modules.Objects.AdvancedShaderController
{
	internal class UnboundVectorControl : PositionedDevUINode
	{
		public DevUILabel label;
		public UnboundSlider xSlider, ySlider;

		private float _width;
		public float Width
		{
			get => _width;
			set
			{
				if (_width != value)
				{
					_width = value;
					label.size = label.size with { x = _width };
					xSlider.width = value;
					ySlider.width = value;
				}
			}
		}

		private string _title;
		public string Title
		{
			get => _title;
			set
			{
				if (_title != value)
				{
					_title = value;
					label.Text = value;
					xSlider.title = $"{value} X";
					ySlider.title = $"{value} Y";
				}
			}
		}

		public bool Restrict
		{
			get => xSlider.restrict;
			set
			{
				xSlider.restrict = value;
				ySlider.restrict = value;
			}
		}

		public Vector2 Value
		{
			get => new(xSlider.Value, ySlider.Value);
			set
			{
				if (value != Value)
				{
					xSlider.Value = value.x;
					ySlider.Value = value.y;
				}
			}
		}

		public UnboundVectorControl(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, Vector2 value, bool restrict, string title) : base(owner, IDstring, parentNode, pos)
		{
			subNodes.Add(label = new DevUILabel(owner, "Label", this, new Vector2(0f, 40f), width, title));
			subNodes.Add(xSlider = new UnboundSlider(owner, "XSlider", this, new Vector2(0f, 20f), width, value.x, restrict, "X"));
			subNodes.Add(ySlider = new UnboundSlider(owner, "YSlider", this, new Vector2(0f, 0f), width, value.y, restrict, "Y"));

			_width = width;

			_title = null!; // to prevent warning
			Title = title; // now actually set it
		}
	}
}
