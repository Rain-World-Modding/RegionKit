using System.Globalization;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes
{
	public class GenericIntegerControl : IntegerControl
	{
		private StringControl valueInput;

		public int minValue = int.MinValue;
		public int maxValue = int.MaxValue;

		public int value;
		public event OnValueChangeHandler? OnValueChanged;

		private bool IsTextValid(string str) => int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out int i) && i >= minValue && i <= maxValue;

		public GenericIntegerControl(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, int defaultValue) 
			: base(owner, IDstring, parentNode, pos, title)
		{
			value = defaultValue;
			subNodes[1].ClearSprites();
			subNodes[1] = valueInput = new StringControl(owner, "Number", this, new Vector2(140f, 0f), 36f, defaultValue.ToString(), IsTextValid);
			valueInput.OnValueChanged += ValueInput_OnValueChanged;
		}

		public GenericIntegerControl(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, int defaultValue, int minValue, int maxValue) 
			: this(owner, IDstring, parentNode, pos, title, defaultValue)
		{
			this.minValue = minValue;
			this.maxValue = maxValue;
		}

		private void ValueInput_OnValueChanged(string value, string _)
		{
			if (IsTextValid(value))
			{
				int oldValue = this.value;
				this.value = int.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
				OnValueChanged?.Invoke(this.value, oldValue);
			}
		}

		public override void Increment(int change)
		{
			base.Increment(change);
			int oldValue = value;
			value += change;
			value = Mathf.Clamp(value, minValue, maxValue);
			if (oldValue != value)
			{
				OnValueChanged?.Invoke(value, oldValue);
				NumberLabelText = value.ToString(CultureInfo.InvariantCulture);
			}
		}

		public delegate void OnValueChangeHandler(int value, int oldValue);
	}
}
