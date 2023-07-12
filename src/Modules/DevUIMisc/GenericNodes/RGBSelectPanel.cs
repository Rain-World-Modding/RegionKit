using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes;

public class RGBSelectPanel : Panel, IDevUISignals
{
	public class ColorBox : DevUILabel
	{
		public ColorBox(DevUI owner, DevUINode parentNode, Vector2 pos, string idstring, float width, float height, Color color) : base(owner, idstring, parentNode, pos, width, "")
		{
			fSprites[0].scaleY = height;
			fSprites[0].color = color;
		}

		public Color color
		{
			get => fSprites[0].color;
			set => fSprites[0].color = value;
		}
	}

	public RGBSelectPanel(DevUI owner, DevUINode parentNode, Vector2 pos, string idstring, string title, Color color)
		: base(owner, idstring, parentNode, pos, new Vector2(305f, 200f), title)
	{
		this.idstring = idstring;
		actualValue = color;
		PlaceNodes();
	}

	public Color actualValue = new(0f, 0f, 0f);

	public void PlaceNodes()
	{
		RGBSliders = new GenericSlider[3];
		HSLSliders = new GenericSlider[3];

		Vector2 pos = new Vector2(size.x - 185f, size.y - 25f);
		AddSlider(ref RGBSliders[0], "R_RGB", " R", pos);

		pos.y -= 20f;
		AddSlider(ref RGBSliders[1], "G_RGB", " G", pos);

		pos.y -= 20f;
		AddSlider(ref RGBSliders[2], "B_RGB", " B", pos);

		pos.y -= 40f;
		Hex = new StringControl(owner, "HEX", this, pos + new Vector2(77f, 0f), 92f, "000000", StringControl.TextIsColor);
		subNodes.Add(Hex);
		subNodes.Add(new DevUILabel(owner, "HEX_LABEL", this, pos, 60f, "HEX"));
		pos.y -= 40f;

		AddSlider(ref HSLSliders[0], "H_HSL", " H", pos);

		pos.y -= 20f;
		AddSlider(ref HSLSliders[1], "S_HSL", " S", pos);

		pos.y -= 20f;
		AddSlider(ref HSLSliders[2], "L_HSL", " L", pos);

		colorBox = new ColorBox(owner, this, new Vector2(5f, size.y - 25f - 64f), "colorbox", 64f, 64f, actualValue);
		subNodes.Add(colorBox);
		UpdateRGBSliders();
		UpdateHSLSliders();
		UpdateHex();
	}

	public void AddSlider(ref GenericSlider slider, string name, string display, Vector2 pos)
	{
		if (slider != null && subNodes.Contains(slider)) subNodes.Remove(slider);
		slider = new GenericSlider(owner, name, this, pos, display, false, 20f, 0f, true, 32) { valueRounding = 2, displayRounding = 2 };
		subNodes.Add(slider);
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (RGBSliders.Contains(sender))
		{
			actualValue = new Color(RGBSliders[0].actualValue, RGBSliders[1].actualValue, RGBSliders[2].actualValue);

			UpdateHSLSliders();
			UpdateHex();
			colorBox.color = actualValue;
		}
		else if (HSLSliders.Contains(sender))
		{
			actualValue = HSL2RGB(HSLSliders[0].actualValue, HSLSliders[1].actualValue, HSLSliders[2].actualValue);

			UpdateRGBSliders();
			UpdateHex();
			colorBox.color = actualValue;
		}

		else if (sender == Hex)
		{
			actualValue = hexToColor(Hex.actualValue);

			UpdateRGBSliders();
			UpdateHSLSliders();
			colorBox.color = actualValue;
		}

		 { this.SendSignal(type, this, message); }
	}

	public void UpdateRGBSliders()
	{
		RGBSliders[0].actualValue = actualValue.r;
		RGBSliders[0].Refresh();
		RGBSliders[1].actualValue = actualValue.g;
		RGBSliders[1].Refresh();
		RGBSliders[2].actualValue = actualValue.b;
		RGBSliders[2].Refresh();
	}

	public void UpdateHSLSliders()
	{
		Vector3 HSLpos = RGB2HSL(actualValue);
		HSLSliders[0].actualValue = HSLpos.x;
		HSLSliders[0].Refresh();
		HSLSliders[1].actualValue = HSLpos.y;
		HSLSliders[1].Refresh();
		HSLSliders[2].actualValue = HSLpos.z;
		HSLSliders[2].Refresh();
	}

	public void UpdateHex()
	{
		Hex.actualValue = colorToHex(actualValue);
		Hex.Text = Hex.actualValue;
		Hex.Refresh();
	}

	private GenericSlider[] RGBSliders;
	private GenericSlider[] HSLSliders;
	private StringControl Hex;
	private ColorBox colorBox;
	public override void Update()
	{
		base.Update();
	}

	public string idstring;

}
