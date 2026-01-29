using DevInterface;

namespace RegionKit.Modules.Objects.AdvancedShaderController
{
	public class AdvancedShaderUVPanel : Panel, IDevUISignals
	{
		public AdvancedShaderRepresentation rep => (parentNode.parentNode as AdvancedShaderRepresentation)!;
		public AdvancedShader.Data data => rep.data;

		private readonly UnboundVectorControl[] uvControls;
		private readonly Vector2[] lastUVs;

		private readonly Cycler restrictUVsButton;
		private readonly Cycler lockUVsButton;
		private readonly Button resetButton;

		public AdvancedShaderUVPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 265f), "Vertex UVs")
		{
			foreach (FSprite sprite in fSprites)
			{
				// fuck you
				sprite.RemoveFromContainer();
				owner.placedObjectsContainer.AddChild(sprite);
			}

			size = new Vector2(250f, 5f + 60f * data.vertices.Length + 60f);

			subNodes.Add(restrictUVsButton = new Cycler(owner, "AdvancedShader_UVPanel_Restrict", this, new Vector2(5f, size.y - 20f), 240f, "Clamp UVs: ", ["NO", "YES"]));
			subNodes.Add(lockUVsButton = new Cycler(owner, "AdvancedShader_UVPanel_Lock", this, new Vector2(5f, size.y - 40f), 240f, "Sync UVs: ", ["NO", "YES"]));
			subNodes.Add(resetButton = new Button(owner, "AdvancedShader_UVPanel_Reset", this, new Vector2(5f, size.y - 60f), 240f, "Reset UVs"));
			restrictUVsButton.currentAlternative = data.restrictUVs ? 1 : 0;
			restrictUVsButton.Text = restrictUVsButton.baseName + restrictUVsButton.alternatives[restrictUVsButton.currentAlternative];
			lockUVsButton.currentAlternative = data.lockUVs ? 1 : 0;
			lockUVsButton.Text = lockUVsButton.baseName + lockUVsButton.alternatives[lockUVsButton.currentAlternative];

			uvControls = new UnboundVectorControl[data.uvs.Length];
			lastUVs = new Vector2[data.uvs.Length];
			for (int i = 0; i < data.uvs.Length; i++)
			{
				uvControls[i] = new UnboundVectorControl(owner, $"AdvancedShader_UVPanel_Vertex{i}", this, new Vector2(5f, 5f + 60f * (data.vertices.Length - i - 1)), 240f, data.uvs[i], data.restrictColors, $"Vertex {i}");
				subNodes.Add(uvControls[i]);
				lastUVs[i] = data.uvs[i];
			}

			Refresh();
		}

		public override void Refresh()
		{
			base.Refresh();
			
			bool restrictUVs = restrictUVsButton.currentAlternative == 1;
			if (restrictUVs != data.restrictUVs)
			{
				data.restrictUVs = restrictUVs;
				for (int i = 0; i < uvControls.Length; i++)
				{
					uvControls[i].Restrict = data.restrictUVs;
				}
			}

			bool lockUVs = lockUVsButton.currentAlternative == 1;
			data.lockUVs = lockUVs;
			if (lockUVs)
			{
				for (int i = 0; i < 4; i++)
				{
					if (uvControls[i].Value != lastUVs[i])
					{
						float left = (i == 0 || i == 1) ? uvControls[i].Value.x : uvControls[0].Value.x;
						float bottom = (i == 0 || i == 2) ? uvControls[i].Value.y : uvControls[0].Value.y;
						float right = (i == 2 || i == 3) ? uvControls[i].Value.x : uvControls[3].Value.x;
						float top = (i == 1 || i == 3) ? uvControls[i].Value.y : uvControls[3].Value.y;
						uvControls[0].Value = new Vector2(left, bottom);
						uvControls[1].Value = new Vector2(left, top);
						uvControls[2].Value = new Vector2(right, bottom);
						uvControls[3].Value = new Vector2(right, top);
						break;
					}
				}
			}

			for (int i = 0; i < uvControls.Length; i++)
			{
				data.uvs[i] = uvControls[i].Value;
				lastUVs[i] = uvControls[i].Value;
			}
		}

		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender == resetButton)
			{
				data.ResetUVs();
				RefreshUVs();
			}
		}

		public void RefreshUVs()
		{
			for (int i = 0; i < uvControls.Length; i++)
			{
				uvControls[i].Value = data.uvs[i];
			}
		}
	}
}
