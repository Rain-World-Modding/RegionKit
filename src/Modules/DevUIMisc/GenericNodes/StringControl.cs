using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

public class StringControl : DevUILabel
{
	protected FSprite[] outlineSprites;
	public event OnValueChangedHandler? OnValueChanged;

	public StringControl(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text, IsTextValid del) : base(owner, IDstring, parentNode, pos, width, text)
	{
		outlineSprites = new FSprite[4];
		for (int i = 0; i < outlineSprites.Length; i++)
		{
			outlineSprites[i] = new FSprite("pixel")
			{
				anchorX = 0f,
				anchorY = 0f,
				color = Color.white,
				isVisible = false,
			};
			fSprites.Add(outlineSprites[i]);
			if (owner != null)
			{
				Futile.stage.AddChild(outlineSprites[i]);
			}
		}
		isTextValid = del;
		actualValue = text;
		Text = text;
		Refresh();
	}

	protected bool clickedLastUpdate = false;


	public bool sendSignal = true;
	public string actualValue;

	public override void Refresh()
	{
		// No data refresh until the transaction is complete :/
		// TrySet happens on input and focus loss
		base.Refresh();

		// Update outline sprites
		outlineSprites[0].SetPosition(absPos + Vector2.one * 0.01f);
		outlineSprites[0].scaleX = size.x;
		outlineSprites[1].SetPosition(absPos + Vector2.one * 0.01f);
		outlineSprites[1].scaleY = size.y;
		outlineSprites[2].SetPosition(absPos + new Vector2(0f, size.y - 1f) + Vector2.one * 0.01f);
		outlineSprites[2].scaleX = size.x;
		outlineSprites[3].SetPosition(absPos + new Vector2(size.x - 1f, 0f) + Vector2.one * 0.01f);
		outlineSprites[3].scaleY = size.y;
	}

	public override void Update()
	{
		if (owner.mouseClick && !clickedLastUpdate)
		{
			if (MouseOver && ManagedStringControl.activeStringControl != this)
			{
				// replace whatever instance/null that was focused
				Text = actualValue;
				ManagedStringControl.activeStringControl = this;
				fLabels[0].color = new Color(0.1f, 0.4f, 0.2f);
			}
			else if (ManagedStringControl.activeStringControl == this)
			{
				// focus lost
				TrySetValue(Text, true);
				ManagedStringControl.activeStringControl = null;
				fLabels[0].color = Color.black;
			}

			clickedLastUpdate = true;
		}
		else if (!owner.mouseClick)
		{
			clickedLastUpdate = false;
		}

		if (ManagedStringControl.activeStringControl == this)
		{
			foreach (char c in Input.inputString)
			{
				if (c == '\b')
				{
					if (Text.Length != 0)
					{
						Text = Text[..^1];
						TrySetValue(Text, false);
					}
				}
				else if (c == '\n' || c == '\r')
				{
					// should lose focus
					TrySetValue(Text, true);
					ManagedStringControl.activeStringControl = null;
					fLabels[0].color = Color.black;
				}
				else
				{
					Text += c;
					TrySetValue(Text, false);
				}
			}
		}

		// Update outline sprite visibility
		bool focused = ManagedStringControl.activeStringControl == this;
		Color outlineColor = isTextValid(actualValue) ? Color.white : Color.red;
		foreach (FSprite sprite in outlineSprites)
		{
			sprite.isVisible = focused;
			sprite.color = outlineColor;
		}
	}

	public delegate bool IsTextValid(string value);

	public IsTextValid isTextValid;


	protected virtual void TrySetValue(string newValue, bool endTransaction)
	{
		if (fLabels.Count == 0) return;
		if (isTextValid(newValue))
		{
			string oldValue = actualValue;
			actualValue = newValue;
			fLabels[0].color = new Color(0.1f, 0.4f, 0.2f);
			foreach (FSprite sprite in outlineSprites)
			{
				sprite.color = Color.white;
			}
			if (sendSignal)
				this.SendSignal(StringEdit, this, "");
			OnValueChanged?.Invoke(newValue, oldValue);
		}
		else
		{
			fLabels[0].color = Color.red;
			foreach (FSprite sprite in outlineSprites)
			{
				sprite.color = Color.red;
			}
		}
		if (endTransaction)
		{
			Text = actualValue;
			fLabels[0].color = Color.black;
			Refresh();
			if (sendSignal)
				this.SendSignal(StringFinish, this, "");
		}
	}

	public static bool TextIsFloat(string value)
	{
		return float.TryParse(value, out _);
	}

	public static bool TextIsInt(string value)
	{
		return (int.TryParse(value, out int i) && i.ToString() == value);
	}

	public static bool TextIsIntNonNegative(string value)
	{
		return (int.TryParse(value, out int i) && i >= 0 && i.ToString() == value);
	}
	public static bool TextIsColor(string value)
	{
		try { Color color = hexToColor(value); return colorToHex(color) == value; }
		catch { return false; }
	}

	public static bool TextIsExtEnum<T>(string value) where T : ExtEnum<T>
	{
		return ExtEnumBase.TryParse(typeof(T), value, false, out _);
	}

	public static bool TextIsAny(string value)
	{ return true; }

	public static bool TextIsValidFilename(string value)
	{ return value.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0; }

	public static readonly DevUISignalType StringEdit = new DevUISignalType("StringEdit", true);
	public static readonly DevUISignalType StringFinish = new DevUISignalType("StringFinish", true);
	public delegate void OnValueChangedHandler(string value, string oldValue);
}
