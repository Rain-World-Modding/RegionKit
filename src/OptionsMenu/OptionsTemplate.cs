using Menu.Remix.MixedUI;

namespace RegionKit;

// Based on the options script from SBCameraScroll by SchuhBaum: https://github.com/SchuhBaum/SBCameraScroll/blob/Rain-World-v1.9/SourceCode/MainModOptions.cs
public abstract class OptionsTemplate : OptionInterface
{
    private const float SPACING = 20.0f;
    private const float FONT_HEIGHT = 20.0f;

    private const int CHECKBOX_COUNT = 2;
    private const float CHECKBOX_SIZE = 60.0f;

    private readonly int DRAGGER_COUNT = 2;
    private readonly float DRAGGER_SIZE = 60.0f;


    private float CheckBoxWithSpacing => CHECKBOX_SIZE + 0.25f * SPACING;
    private float DraggerWithSpacing => DRAGGER_SIZE + 0.25f * SPACING;
    

    private Vector2 MarginX;
    private Vector2 Pos;


    private readonly List<float> BoxEndPositions = [];

    private readonly List<OpLabel> TextLabels = [];

    private readonly List<Configurable<bool>> CheckBoxConfigurables = [];
    private readonly List<OpLabel> CheckBoxesTextLabels = [];

    private readonly List<Configurable<string>> ComboBoxConfigurables = [];
    private readonly List<List<ListItem>> ComboBoxLists = [];
    private readonly List<bool> ComboBoxAllowEmpty = [];
    private readonly List<OpLabel> ComboBoxesTextLabels = [];

    private readonly List<Configurable<int>> IntSliderConfigurables = [];
    private readonly List<string> IntSliderMainTextLabels = [];
    private readonly List<OpLabel> IntSliderTextLabelsLeft = [];
    private readonly List<OpLabel> IntSliderTextLabelsRight = [];


    private readonly List<Configurable<int>> DraggerConfigurables = [];
    private readonly List<OpLabel> DraggerTextLabels = [];

    private readonly List<Configurable<float>> FloatSliderConfigurables = [];
    private readonly List<string> FloatSliderMainTextLabels = [];
    private readonly List<OpLabel> FloatSliderTextLabelsLeft = [];
    private readonly List<OpLabel> FloatSliderTextLabelsRight = [];



    protected void AddTab(ref int tabIndex, string tabName)
    {
        tabIndex++;
        Tabs[tabIndex] = new OpTab(this, Translate(tabName));
        InitializeMarginAndPos();

        AddNewLine();

		AddAndDrawLargeDivider(ref Tabs[tabIndex]);

		AddTextLabel(MOD_FRIENDLYNAME, bigText: true);
        DrawTextLabels(ref Tabs[tabIndex]);

		AddAndDrawLargeDivider(ref Tabs[tabIndex]);

        AddNewLine(0.5f);
        AddTextLabel(Translate("Version") + " " + MOD_VERSION, FLabelAlignment.Left);
        AddTextLabel(Translate("by") + " " + Translate("RegionKit Team"), FLabelAlignment.Right);
        DrawTextLabels(ref Tabs[tabIndex]);

        AddNewLine();
        AddBox();
    }



    protected void InitializeMarginAndPos()
    {
        MarginX = new(50f, 550f);
        Pos = new(50f, 600f);
    }

    protected void AddNewLine(float spacingModifier = 1f)
    {
        Pos.x = MarginX.x;
        Pos.y -= spacingModifier * SPACING;
    }



    protected void AddBox()
    {
        MarginX += new Vector2(SPACING, -SPACING);
        BoxEndPositions.Add(Pos.y);
        AddNewLine();
    }

    protected void DrawBox(ref OpTab tab)
    {
        MarginX += new Vector2(-SPACING, SPACING);

        AddNewLine();

        float boxWidth = MarginX.y - MarginX.x;
        int lastIndex = BoxEndPositions.Count - 1;

		OpRect rect;
        tab.AddItems(rect = new OpRect(Pos, new(boxWidth, BoxEndPositions[lastIndex] - Pos.y)));
        BoxEndPositions.RemoveAt(lastIndex);
		rect.MoveToBack();
    }



	// CHECK BOX
    protected void AddCheckBox(Configurable<bool> configurable, string? text=null)
    {
        text ??= Translate((string)configurable.info.Tags[0]);

        CheckBoxConfigurables.Add(configurable);
        CheckBoxesTextLabels.Add(new(new(), new(), text, FLabelAlignment.Left));
    }

    protected void DrawCheckBoxes(ref OpTab tab) // changes pos.y but not pos.x
    {
        if (CheckBoxConfigurables.Count != CheckBoxesTextLabels.Count) return;

        var width = MarginX.y - MarginX.x;
        var elementWidth = (width - (CHECKBOX_COUNT - 1) * 0.5f * SPACING) / CHECKBOX_COUNT;

        Pos.y -= CHECKBOX_SIZE;

        var _posX = Pos.x;

        for (int checkBoxIndex = 0; checkBoxIndex < CheckBoxConfigurables.Count; ++checkBoxIndex)
        {
            var configurable = CheckBoxConfigurables[checkBoxIndex];

            OpCheckBox checkBox = new(configurable, new Vector2(_posX, Pos.y))
            {
                description = Translate(configurable.info?.description) ?? ""
            };
            tab.AddItems(checkBox);

            _posX += CheckBoxWithSpacing;

            var checkBoxLabel = CheckBoxesTextLabels[checkBoxIndex];

            checkBoxLabel.pos = new(_posX, Pos.y + 2f);
            checkBoxLabel.size = new(elementWidth - CheckBoxWithSpacing, FONT_HEIGHT);

            tab.AddItems(checkBoxLabel);

            if (checkBoxIndex < CheckBoxConfigurables.Count - 1)
            {
                if ((checkBoxIndex + 1) % CHECKBOX_COUNT == 0)
                {
                    AddNewLine();

                    Pos.y -= CHECKBOX_SIZE;
                    _posX = Pos.x;
                }
                else
                {
                    _posX += elementWidth - CheckBoxWithSpacing + 0.5f * SPACING;
                }
            }
        }

        CheckBoxConfigurables.Clear();
        CheckBoxesTextLabels.Clear();
    }



	// COMBO BOX
    protected void AddComboBox(Configurable<string> configurable, List<ListItem> list, string? text = null, bool allowEmpty = false)
    {
        text ??= Translate((string)configurable.info.Tags[0]);

        OpLabel opLabel = new(new Vector2(), new Vector2(0.0f, FONT_HEIGHT), Translate(text), FLabelAlignment.Center, false);

        ComboBoxesTextLabels.Add(opLabel);
        ComboBoxConfigurables.Add(configurable);
        ComboBoxLists.Add(list);
        ComboBoxAllowEmpty.Add(allowEmpty);
    }

    protected void DrawComboBoxes(ref OpTab tab)
    {
        if (ComboBoxConfigurables.Count != ComboBoxesTextLabels.Count) return;
        if (ComboBoxConfigurables.Count != ComboBoxLists.Count) return;
        if (ComboBoxConfigurables.Count != ComboBoxAllowEmpty.Count) return;

        var offsetX = (MarginX.y - MarginX.x) * 0.1f;
        var width = (MarginX.y - MarginX.x) * 0.4f;

        for (int comboBoxIndex = 0; comboBoxIndex < ComboBoxConfigurables.Count; ++comboBoxIndex)
        {
            AddNewLine(1.25f);
            Pos.x += offsetX;

            var opLabel = ComboBoxesTextLabels[comboBoxIndex];
            opLabel.pos = Pos;
            opLabel.size += new Vector2(width, 2f);
            Pos.x += width;

            var configurable = ComboBoxConfigurables[comboBoxIndex];
            OpComboBox comboBox = new(configurable, Pos, width, ComboBoxLists[comboBoxIndex])
            {
                allowEmpty = ComboBoxAllowEmpty[comboBoxIndex],
                description = Translate(configurable.info?.description) ?? ""
            };
            tab.AddItems(opLabel, comboBox);

            if (comboBoxIndex < ComboBoxConfigurables.Count - 1)
            {
                AddNewLine();
                Pos.x = MarginX.x;
            }
        }

        ComboBoxesTextLabels.Clear();
        ComboBoxConfigurables.Clear();
        ComboBoxLists.Clear();
        ComboBoxAllowEmpty.Clear();
    }



	// INT SLIDER
    protected void AddIntSlider(Configurable<int> configurable, string? text = null, string sliderTextLeft = "", string sliderTextRight = "")
    {
        text ??= Translate((string)configurable.info.Tags[0]);

        IntSliderConfigurables.Add(configurable);
        IntSliderMainTextLabels.Add(text);
        IntSliderTextLabelsLeft.Add(new(new(), new(), sliderTextLeft, alignment: FLabelAlignment.Right)); // set pos and size when drawing
        IntSliderTextLabelsRight.Add(new(new(), new(), sliderTextRight, alignment: FLabelAlignment.Left));
    }

    protected void DrawIntSliders(ref OpTab tab)
    {
        if (IntSliderConfigurables.Count != IntSliderMainTextLabels.Count) return;
        if (IntSliderConfigurables.Count != IntSliderTextLabelsLeft.Count) return;
        if (IntSliderConfigurables.Count != IntSliderTextLabelsRight.Count) return;

        float width = MarginX.y - MarginX.x;
        float sliderCenter = MarginX.x + 0.5f * width;
        float sliderLabelSizeX = 0.2f * width;
        float sliderSizeX = width - 2f * sliderLabelSizeX - SPACING;

        for (int sliderIndex = 0; sliderIndex < IntSliderConfigurables.Count; ++sliderIndex)
        {
            AddNewLine(2f);

            var opLabel = IntSliderTextLabelsLeft[sliderIndex];
            opLabel.pos = new(MarginX.x, Pos.y + 5f);
            opLabel.size = new(sliderLabelSizeX, FONT_HEIGHT);
            tab.AddItems(opLabel);

            var configurable = IntSliderConfigurables[sliderIndex];
            OpSlider slider = new(configurable, new(sliderCenter - 0.5f * sliderSizeX, Pos.y), (int)sliderSizeX)
            {
                size = new(sliderSizeX, FONT_HEIGHT),
                description = Translate(configurable.info?.description) ?? ""
            };
            tab.AddItems(slider);

            opLabel = IntSliderTextLabelsRight[sliderIndex];
            opLabel.pos = new(sliderCenter + 0.5f * sliderSizeX + 0.5f * SPACING, Pos.y + 5f);
            opLabel.size = new(sliderLabelSizeX, FONT_HEIGHT);
            tab.AddItems(opLabel);

            AddTextLabel(IntSliderMainTextLabels[sliderIndex]);
            DrawTextLabels(ref tab);

            if (sliderIndex < IntSliderConfigurables.Count - 1)
                AddNewLine();
        }

        IntSliderConfigurables.Clear();
        IntSliderMainTextLabels.Clear();
        IntSliderTextLabelsLeft.Clear();
        IntSliderTextLabelsRight.Clear();
    }



	// TEXT LABEL
    protected void AddTextLabel(string text, FLabelAlignment alignment = FLabelAlignment.Center, bool bigText = false, bool shiny = false, bool translate = true, Color? color = null)
    {
        float textHeight = (bigText ? 2f : 1f) * FONT_HEIGHT;

        if (TextLabels.Count == 0)
            Pos.y -= textHeight;
        
        OpLabel textLabel = new(new Vector2(), new Vector2(20f, textHeight), translate ? Translate(text) : text, alignment, bigText) // minimal size.x = 20f
        {
            autoWrap = true
        };

		if (shiny)
		{
			textLabel.label.shader = Custom.rainWorld.Shaders["MenuText"];
		}

		if (color != null)
		{
			textLabel.color = (Color)color;
		}
        
        TextLabels.Add(textLabel);
    }

    protected void DrawTextLabels(ref OpTab tab)
    {
        if (TextLabels.Count == 0) return;

        float width = (MarginX.y - MarginX.x) / TextLabels.Count;

        foreach (var textLabel in TextLabels)
        {
            textLabel.pos = Pos;
            textLabel.size += new Vector2(width - 20f, 0.0f);
            tab.AddItems(textLabel);
            Pos.x += width;
        }

        Pos.x = MarginX.x;
        TextLabels.Clear();
    }



	// FLOAT SLIDER
    protected void AddFloatSlider(Configurable<float> configurable, string? text = null, string sliderTextLeft = "", string sliderTextRight = "")
    {
        text ??= Translate((string)configurable.info.Tags[0]);

        FloatSliderConfigurables.Add(configurable);
        FloatSliderMainTextLabels.Add(text);
        FloatSliderTextLabelsLeft.Add(new OpLabel(new Vector2(), new Vector2(), sliderTextLeft, alignment: FLabelAlignment.Right)); // set pos and size when drawing
        FloatSliderTextLabelsRight.Add(new OpLabel(new Vector2(), new Vector2(), sliderTextRight, alignment: FLabelAlignment.Left));
    }

    protected void DrawFloatSliders(ref OpTab tab)
    {
        if (FloatSliderConfigurables.Count != FloatSliderMainTextLabels.Count) return;
        if (FloatSliderConfigurables.Count != FloatSliderTextLabelsLeft.Count) return;
        if (FloatSliderConfigurables.Count != FloatSliderTextLabelsRight.Count) return;

        float width = MarginX.y - MarginX.x;
        float sliderCenter = MarginX.x + 0.5f * width;
        float sliderLabelSizeX = 0.2f * width;
        float sliderSizeX = width - 2f * sliderLabelSizeX - SPACING;

        for (int sliderIndex = 0; sliderIndex < FloatSliderConfigurables.Count; ++sliderIndex)
        {
            AddNewLine(2f);

            OpLabel opLabel = FloatSliderTextLabelsLeft[sliderIndex];
            opLabel.pos = new Vector2(MarginX.x, Pos.y + 5f);
            opLabel.size = new Vector2(sliderLabelSizeX, FONT_HEIGHT);
            tab.AddItems(opLabel);

            Configurable<float> configurable = FloatSliderConfigurables[sliderIndex];
            OpFloatSlider slider = new(configurable, new Vector2(sliderCenter - 0.5f * sliderSizeX, Pos.y), (int)sliderSizeX, 1)
            {
                size = new Vector2(sliderSizeX, FONT_HEIGHT),
                description = Translate(configurable.info?.description) ?? ""
            };
            tab.AddItems(slider);

            opLabel = FloatSliderTextLabelsRight[sliderIndex];
            opLabel.pos = new Vector2(sliderCenter + 0.5f * sliderSizeX + 0.5f * SPACING, Pos.y + 5f);
            opLabel.size = new Vector2(sliderLabelSizeX, FONT_HEIGHT);
            tab.AddItems(opLabel);

            AddTextLabel(FloatSliderMainTextLabels[sliderIndex]);
            DrawTextLabels(ref tab);

            if (sliderIndex < FloatSliderConfigurables.Count - 1)
            {
                AddNewLine();
            }
        }

        FloatSliderConfigurables.Clear();
        FloatSliderMainTextLabels.Clear();
        FloatSliderTextLabelsLeft.Clear();
        FloatSliderTextLabelsRight.Clear();
    }



	// INT DRAGGER
    protected void AddDragger(Configurable<int> configurable, string? text = null)
    {
        text ??= Translate((string)configurable.info.Tags[0]);

        DraggerConfigurables.Add(configurable);
        DraggerTextLabels.Add(new OpLabel(new Vector2(), new Vector2(), text, FLabelAlignment.Left));
    }

    protected void DrawDraggers(ref OpTab tab)
    {
        if (DraggerConfigurables.Count != DraggerTextLabels.Count) return;

        float width = MarginX.y - MarginX.x;
        float elementWidth = (width - (DRAGGER_COUNT - 1) * 0.5f * SPACING) / DRAGGER_COUNT;
        Pos.y -= DRAGGER_SIZE;
        float _posX = Pos.x;

        for (int i = 0; i < DraggerConfigurables.Count; ++i)
        {
            Configurable<int> configurable = DraggerConfigurables[i];

            OpDragger dragger = new(configurable, new Vector2(_posX, Pos.y))
            {
                description = Translate(configurable.info?.description) ?? ""
            };
            tab.AddItems(dragger);
            _posX += DraggerWithSpacing;

            OpLabel draggerLabel = DraggerTextLabels[i];
            draggerLabel.pos = new Vector2(_posX, Pos.y + 2f);
            draggerLabel.size = new Vector2(elementWidth - DraggerWithSpacing, FONT_HEIGHT);
            tab.AddItems(draggerLabel);

            if (i < DraggerConfigurables.Count - 1)
            {
                if ((i + 1) % DRAGGER_COUNT == 0)
                {
                    AddNewLine();
                    Pos.y -= DRAGGER_SIZE;
                    _posX = Pos.x;
                }
                else
                {
                    _posX += elementWidth - DraggerWithSpacing + 0.5f * SPACING;
                }
            }
        }

        DraggerConfigurables.Clear();
        DraggerTextLabels.Clear();
    }



	// DIVIDER
	protected void AddAndDrawLargeDivider(ref OpTab tab, Vector2? offset = null, bool newline = false)
	{
		var pos = new Vector2(300.0f, Pos.y) + (offset ?? Vector2.zero);

		var left = new OpImage(pos, "LinearGradient200");
		left.sprite.SetAnchor(0.5f, 0.0f);
		left.sprite.rotation = 270.0f;

		var right = new OpImage(pos, "LinearGradient200");
		right.sprite.SetAnchor(0.5f, 0.0f);
		right.sprite.rotation = 90.0f;

		tab.AddItems(left, right);

		if (newline)
		{
			AddNewLine(1);
		}
	}

	protected void AddAndDrawSmallDivider(ref OpTab tab, Vector2? offset = null, bool newline = false)
	{
		var pos = new Vector2(300.0f, Pos.y) + (offset ?? Vector2.zero);

		var divider = new OpImage(pos, "listDivider");
		divider.sprite.SetAnchor(0.5f, 0.0f);

		tab.AddItems(divider);

		if (newline)
		{
			AddNewLine(1);
		}
	}



	// KEYBINDER
	protected void AddAndDrawKeybinder(Configurable<KeyCode> configurable, ref OpTab tab, Vector2? offset = null, bool newline = true)
	{
		var name = Translate((string)configurable.info.Tags[0]);

		tab.AddItems(
			new OpLabel(new Vector2(115.0f, Pos.y) + (offset ?? Vector2.zero), new Vector2(100f, 34f), name)
			{
				alignment = FLabelAlignment.Right,
				verticalAlignment = OpLabel.LabelVAlignment.Center,
				description = Translate(configurable.info?.description)
			},
			new OpKeyBinder(configurable, new Vector2(235.0f, Pos.y) + (offset ?? Vector2.zero), new(146f, 30f), false)
		);

		if (newline)
		{
			AddNewLine(2);
		}
	}



	// CONFIGURABLE GETTERS
	// Used to retrieve an existing configurable from the menu after init

	public bool GetConfigurable<T, TConfigurable>(Configurable<T> cfg, out TConfigurable checkBox)
        where TConfigurable : UIconfig
    {
        foreach (var tab in Tabs)
        {
            if (tab == null) continue;

            if (tab.items.FirstOrDefault(item => item is TConfigurable configurable && configurable.cfgEntry == cfg) is TConfigurable search)
            {
                checkBox = search;
                return true;
            }
        }

        checkBox = null!;
        return false;
    }

    public bool GetLabel<T>(Configurable<T> cfg, out OpLabel label) => GetLabel(cfg.info.Tags[0].ToString(), out label);

    public bool GetLabel(string text, out OpLabel label)
    {
        foreach (var tab in Tabs)
        {
            if (tab == null) continue;

            if (tab.items.FirstOrDefault(item => item is OpLabel label && label.text == Translate(text)) is OpLabel search)
            {
                label = search;
                return true;
            }
        }

        label = null!;
        return false;
    }
}

/*
    MIT License

    Copyright(c) 2022 SchuhBaum

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
