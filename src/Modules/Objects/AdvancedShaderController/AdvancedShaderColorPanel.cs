using DevInterface;

namespace RegionKit.Modules.Objects.AdvancedShaderController
{
	public class AdvancedShaderColorPanel : Panel, IDevUISignals
	{
		public AdvancedShaderRepresentation rep => (parentNode.parentNode as AdvancedShaderRepresentation)!;
		public AdvancedShader.Data data => rep.data;

		private readonly UnboundRGBAControl[] colorControls;
		private readonly Color[] lastColors;

		private readonly Cycler lockColorsButton, restrictColorsButton;
		private readonly Button resetButton;
		private readonly ColorPreview preview;

		public AdvancedShaderColorPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 445f), "Vertex Colors")
		{
			foreach (FSprite sprite in fSprites)
			{
				// fuck you
				sprite.RemoveFromContainer();
				owner.placedObjectsContainer.AddChild(sprite);
			}

			size = new Vector2(250f, 5f + 100f * data.vertices.Length + 60f);

			subNodes.Add(preview = new ColorPreview(owner, "AdvancedShader_ColorPanel_Preview", this, new Vector2(5f, size.y - 60f), new Vector2(70f, 56f)));
			subNodes.Add(restrictColorsButton = new Cycler(owner, "AdvancedShader_ColorPanel_Restrict", this, new Vector2(80f, size.y - 20f), 160f, "Clamp colors: ", ["NO", "YES"]));
			subNodes.Add(lockColorsButton = new Cycler(owner, "AdvancedShader_ColorPanel_Lock", this, new Vector2(80f, size.y - 40f), 160f, "Sync colors: ", ["NO", "YES"]));
			subNodes.Add(resetButton = new Button(owner, "AdvancedShader_ColorPanel_Reset", this, new Vector2(80f, size.y - 60f), 160f, "Reset colors"));

			restrictColorsButton.currentAlternative = data.restrictColors ? 1 : 0;
			restrictColorsButton.Text = restrictColorsButton.baseName + restrictColorsButton.alternatives[restrictColorsButton.currentAlternative];
			lockColorsButton.currentAlternative = data.lockColors ? 1 : 0;
			lockColorsButton.Text = lockColorsButton.baseName + lockColorsButton.alternatives[lockColorsButton.currentAlternative];

			colorControls = new UnboundRGBAControl[data.vertices.Length];
			lastColors = new Color[data.vertices.Length];
			for (int i = 0; i < data.vertices.Length; i++)
			{
				colorControls[i] = new UnboundRGBAControl(owner, $"AdvancedShader_ColorPanel_Vertex{i}", this, new Vector2(5f, 5f + 100f * (data.vertices.Length - i - 1)), 240f, data.colors[i], data.restrictColors, $"Vertex {i}");
				subNodes.Add(colorControls[i]);
				lastColors[i] = data.colors[i];
			}

			Refresh();
		}

		public override void Refresh()
		{
			base.Refresh();

			if (data.lockColors)
			{
				for (int i = 0; i < colorControls.Length; i++)
				{
					if (colorControls[i].Value != lastColors[i])
					{
						for (int j = 0; j < colorControls.Length; j++)
						{
							colorControls[j].Value = colorControls[i].Value;
						}
						break;
					}
				}
			}

			for (int i = 0; i < colorControls.Length; i++)
			{
				data.colors[i] = lastColors[i] = colorControls[i].Value;
			}

			bool lockColors = lockColorsButton.currentAlternative == 1;
			if (lockColors != data.lockColors)
			{
				data.lockColors = lockColors;
			}

			bool restrictColors = restrictColorsButton.currentAlternative == 1;
			if (restrictColors != data.restrictColors)
			{
				data.restrictColors = restrictColors;
				for (int i = 0; i < colorControls.Length;i++)
				{
					colorControls[i].Restrict = data.restrictColors;
				}
			}
		}

		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender == resetButton)
			{
				data.ResetColors();
				for (int i = 0; i < colorControls.Length; i++)
				{
					colorControls[i].Value = data.colors[i];
				}
			}
		}

		private class ColorPreview : RectangularDevUINode
		{
			private readonly TriangleMesh colorSprite;
			private readonly FSprite[] lines;

			private Color GetColor(int i) => (parentNode as AdvancedShaderColorPanel)!.data.colors[i];

			public ColorPreview(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size) : base(owner, IDstring, parentNode, pos, size)
			{
				TriangleMesh.Triangle[] tris = [
					new TriangleMesh.Triangle(0, 1, 2),
					new TriangleMesh.Triangle(1, 2, 3)
					];
				fSprites.Add(colorSprite = new TriangleMesh("Futile_White", tris, true, false));
				colorSprite.UVvertices = [new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(1f, 1f)];
				owner.placedObjectsContainer.AddChild(colorSprite);

				lines = new FSprite[4];
				for (int i = 0; i < lines.Length; i++)
				{
					fSprites.Add(lines[i] = new FSprite("pixel")
					{
						anchorX = 0,
						anchorY = 0
					});
					owner.placedObjectsContainer.AddChild(lines[i]);
				}
			}

			public override void Refresh()
			{
				base.Refresh();

				colorSprite.verticeColors[0] = GetColor(0);
				colorSprite.verticeColors[1] = GetColor(1);
				colorSprite.verticeColors[2] = GetColor(2);
				colorSprite.verticeColors[3] = GetColor(3);

				colorSprite.MoveVertice(0, absPos + new Vector2(0.01f, 0.01f));
				colorSprite.MoveVertice(1, absPos + new Vector2(0.01f, 0.01f + size.y));
				colorSprite.MoveVertice(2, absPos + new Vector2(0.01f + size.x, 0.01f));
				colorSprite.MoveVertice(3, absPos + new Vector2(0.01f + size.x, 0.01f + size.y));

				lines[0].scaleY = lines[2].scaleY = size.y;
				lines[1].scaleX = lines[3].scaleX = size.x;
				lines[0].SetPosition(absPos + new Vector2(0.01f, 0.01f));
				lines[1].SetPosition(absPos + new Vector2(0.01f, 0.01f));
				lines[2].SetPosition(absPos + new Vector2(-1.01f + size.x, 0.01f));
				lines[3].SetPosition(absPos + new Vector2(0.01f, -1.01f + size.y));
			}
		}
	}
}
