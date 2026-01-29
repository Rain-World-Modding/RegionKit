using DevInterface;

namespace RegionKit.Modules.Objects.AdvancedShaderController
{
	public class UnboundRGBAControl : PositionedDevUINode
	{
		public DevUILabel label;
		public UnboundSlider redPicker, greenPicker, bluePicker, alphaPicker;

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
					redPicker.width = value;
					greenPicker.width = value;
					bluePicker.width = value;
					alphaPicker.width = value;
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
					redPicker.title = $"{value} Red";
					greenPicker.title = $"{value} Green";
					bluePicker.title = $"{value} Blue";
					alphaPicker.title = $"{value} Alpha";
				}
			}
		}

		public bool Restrict
		{
			get => redPicker.restrict;
			set
			{
				redPicker.restrict = value;
				greenPicker.restrict = value;
				bluePicker.restrict = value;
				alphaPicker.restrict = value;
			}
		}

		public Color Value
		{
			get => new(redPicker.Value, greenPicker.Value, bluePicker.Value, alphaPicker.Value);
			set
			{
				if (value != Value)
				{
					redPicker.Value = value.r;
					greenPicker.Value = value.g;
					bluePicker.Value = value.b;
					alphaPicker.Value = value.a;
				}
			}
		}

		public UnboundRGBAControl(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, Color color, bool restrict, string title) : base(owner, IDstring, parentNode, pos)
		{
			subNodes.Add(label = new DevUILabel(owner, "Label", this, new Vector2(0f, 80f), width, title));
			subNodes.Add(redPicker = new UnboundSlider(owner, "RedSlider", this, new Vector2(0f, 60f), width, color.r, restrict, "Red") { TrackColor = Color.red });
			subNodes.Add(greenPicker = new UnboundSlider(owner, "GreenSlider", this, new Vector2(0f, 40f), width, color.g, restrict, "Green") { TrackColor = Color.green });
			subNodes.Add(bluePicker = new UnboundSlider(owner, "BlueSlider", this, new Vector2(0f, 20f), width, color.b, restrict, "Blue") { TrackColor = Color.blue });
			subNodes.Add(alphaPicker = new UnboundSlider(owner, "AlphaSlider", this, new Vector2(0f, 0f), width, color.a, restrict, "Alpha"));

			_width = width;

			_title = null!; // to prevent warning
			Title = title; // now actually set it
		}
	}
}
