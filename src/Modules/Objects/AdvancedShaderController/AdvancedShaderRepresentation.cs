using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;
using Steamworks;
using Watcher;

namespace RegionKit.Modules.Objects.AdvancedShaderController
{
	public class AdvancedShaderRepresentation : PlacedObjectRepresentation
	{
		public AdvancedShader.Data data => (pObj.data as AdvancedShader.Data)!;

		private readonly AdvancedShader shaderInstance;

		private readonly FSprite quadConnector;
		private readonly FSprite panelConnector;
		private readonly FSprite[] quadFSprites;
		private readonly TitledHandle[] quadHandles;
		private readonly AdvancedShaderPanel panel;

		private readonly Vector2[] lastHandlesPos;

		public AdvancedShaderRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, "Advanced Shader")
		{
			if (pObj is null) throw new ArgumentNullException(nameof(pObj));

			// Init stuff
			fSprites.Add(quadConnector = new FSprite("pixel") { anchorY = 0f });
			fSprites.Add(panelConnector = new FSprite("pixel") { anchorY = 0f });
			owner.placedObjectsContainer.AddChild(quadConnector);
			owner.placedObjectsContainer.AddChild(panelConnector);

			quadFSprites = new FSprite[data.vertices.Length];
			quadHandles = new TitledHandle[data.vertices.Length];
			lastHandlesPos = new Vector2[data.vertices.Length];
			for (int i = 0; i < data.vertices.Length; i++)
			{
				fSprites.Add(quadFSprites[i] = new FSprite("pixel") { anchorY = 0f });
				owner.placedObjectsContainer.AddChild(quadFSprites[i]);

				quadHandles[i] = new TitledHandle(owner, $"AdvancedShader_Handle{i}", this, data.vertices[i], $"Vertex {i}");
				subNodes.Add(quadHandles[i]);
				lastHandlesPos[i] = data.vertices[i];
			}

			subNodes.Add(panel = new AdvancedShaderPanel(owner, "AdvancedShader_Panel", this, data.panelPos));

			// Get the thingy
			foreach (UpdatableAndDeletable obj in owner.room.updateList)
			{
				if ((obj as AdvancedShader)?.pObj == pObj)
				{
					shaderInstance = (AdvancedShader)obj;
				}
			}

			if (shaderInstance is null)
			{
				shaderInstance = new AdvancedShader(pObj);
				owner.room.AddObject(shaderInstance);
			}
		}

		public override void Refresh()
		{
			// Shape keeping
			for (int i = 0; i < 4; i++)
			{
				// 1 -- 3
				// |    |
				// |    |
				// 0 -- 2

				if (quadHandles[i].pos != lastHandlesPos[i])
				{
					Vector2 newPos = quadHandles[i].pos;
					if (!Input.GetKey(KeyCode.LeftShift))
					{
						// Shift NOT pressed, normal behavior
						switch (data.shapeLock)
						{
							case ShapeLock.Square:
							{
								Vector2 rotateAround = (i == 0) ? lastHandlesPos[3] : lastHandlesPos[0];
								Vector2 dir = rotateAround - newPos;
								if (i == 0 || i == 3)
								{
									Vector2 perp = Custom.PerpendicularVector(dir) * (i == 0 ? -1 : 1);
									quadHandles[1].pos = newPos + dir / 2 - perp * dir.magnitude / 2;
									quadHandles[2].pos = newPos + dir / 2 + perp * dir.magnitude / 2;
								}
								else
								{
									Vector2 perp = Custom.PerpendicularVector(dir) * (i == 2 ? -1 : 1);
									quadHandles[3 - i].pos = quadHandles[0].pos + perp * dir.magnitude;
									quadHandles[3].pos = newPos + perp * dir.magnitude;
								}
								break;
							}
							case ShapeLock.Rect:
							{
								float left = (i == 0 || i == 1) ? newPos.x : quadHandles[0].pos.x;
								float bottom = (i == 0 || i == 2) ? newPos.y : quadHandles[0].pos.y;
								float right = (i == 2 || i == 3) ? newPos.x : quadHandles[3].pos.x;
								float top = (i == 1 || i == 3) ? newPos.y : quadHandles[3].pos.y;
								quadHandles[0].pos = new Vector2(left, bottom);
								quadHandles[1].pos = new Vector2(left, top);
								quadHandles[2].pos = new Vector2(right, bottom);
								quadHandles[3].pos = new Vector2(right, top);
								break;
							}
							case ShapeLock.Shape:
							{
								int rotateAroundIndex = (i == 0) ? 3 : 0;
								Vector2 rotateAround = lastHandlesPos[rotateAroundIndex];

								float angleDiff = Custom.VecToDeg(rotateAround - newPos) - Custom.VecToDeg(rotateAround - lastHandlesPos[i]);
								float scaleDiff = (rotateAround - newPos).magnitude / (rotateAround - lastHandlesPos[i]).magnitude;
								for (int j = 0; j < 4; j++)
								{
									if (j != i && j != rotateAroundIndex)
									{
										Vector2 diff = quadHandles[j].pos - rotateAround;
										quadHandles[j].pos = rotateAround + Custom.DegToVec(Custom.VecToDeg(diff) + angleDiff) * diff.magnitude * scaleDiff;
									}
								}
								break;
							}
						}
					}
					else
					{
						// Shift pressed, move all other vertices
						Vector2 change = newPos - lastHandlesPos[i];
						for (int j = 0; j < quadHandles.Length; j++)
						{
							if (j != i)
							{
								quadHandles[j].pos += change;
							}
						}
					}

					// Break out, since now any and all positions will be different, so no point in checking the rest
					break;
				}
			}

			// Refresh to update the handles (and every other subnode really)
			base.Refresh();

			// Update data
			for (int i = 0; i < data.vertices.Length; i++)
			{
				data.vertices[i] = quadHandles[i].pos;
				lastHandlesPos[i] = quadHandles[i].pos;
			}
			data.panelPos = panel.pos;

			// Update connector sprites
			quadConnector.SetPosition(absPos + new Vector2(0.01f, 0.01f));
			quadConnector.rotation = Custom.AimFromOneVectorToAnother(absPos, quadHandles[0].absPos);
			quadConnector.scaleY = (absPos - quadHandles[0].absPos).magnitude;

			quadFSprites[0].SetPosition(quadHandles[0].absPos + new Vector2(0.01f, 0.01f));
			quadFSprites[0].rotation = Custom.AimFromOneVectorToAnother(quadHandles[0].absPos, quadHandles[1].absPos);
			quadFSprites[0].scaleY = Vector2.Distance(quadHandles[0].absPos, quadHandles[1].absPos);
			quadFSprites[1].SetPosition(quadHandles[1].absPos + new Vector2(0.01f, 0.01f));
			quadFSprites[1].rotation = Custom.AimFromOneVectorToAnother(quadHandles[1].absPos, quadHandles[3].absPos);
			quadFSprites[1].scaleY = Vector2.Distance(quadHandles[1].absPos, quadHandles[3].absPos);
			quadFSprites[2].SetPosition(quadHandles[2].absPos + new Vector2(0.01f, 0.01f));
			quadFSprites[2].rotation = Custom.AimFromOneVectorToAnother(quadHandles[2].absPos, quadHandles[3].absPos);
			quadFSprites[2].scaleY = Vector2.Distance(quadHandles[2].absPos, quadHandles[3].absPos);
			quadFSprites[3].SetPosition(quadHandles[2].absPos + new Vector2(0.01f, 0.01f));
			quadFSprites[3].rotation = Custom.AimFromOneVectorToAnother(quadHandles[2].absPos, quadHandles[0].absPos);
			quadFSprites[3].scaleY = Vector2.Distance(quadHandles[2].absPos, quadHandles[0].absPos);

			panelConnector.SetPosition(absPos + new Vector2(0.01f, 0.01f));
			panelConnector.rotation = Custom.AimFromOneVectorToAnother(absPos, panel.absPos);
			panelConnector.scaleY = Vector2.Distance(absPos, panel.absPos);
		}

		private class AdvancedShaderPanel : Panel, IDevUISignals
		{
			public AdvancedShaderRepresentation rep => (parentNode as AdvancedShaderRepresentation)!;
			public AdvancedShader.Data data => rep.data;

			private readonly Button shaderSelectButton, spriteSelectButton, lockNone, lockShape, lockSquare, lockRect, colorsButton, uvsButton;
			private readonly ArrowButton containerLeft, containerRight;
			private readonly DevUILabel containerLabel;
			private CustomDecalRepresentation.SelectDecalPanel? shaderSelectPanel, spriteSelectPanel;

			private AdvancedShaderColorPanel? colorPanel;
			private AdvancedShaderUVPanel? uvPanel;

			public AdvancedShaderPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 105f), "Advanced Shader")
			{
				subNodes.Add(new DevUILabel(owner, "AdvancedShader_Label_Shader", this, new Vector2(5f, 85f), 50f, "Shader: "));
				subNodes.Add(shaderSelectButton = new Button(owner, "AdvancedShader_Button_Shader", this, new Vector2(60f, 85f), 180f, data.shader));

				subNodes.Add(new DevUILabel(owner, "AdvancedShader_Label_Sprite", this, new Vector2(5f, 65f), 50f, "Sprite: "));
				subNodes.Add(spriteSelectButton = new Button(owner, "AdvancedShader_Button_Sprite", this, new Vector2(60f, 65f), 180f, data.spriteName));

				subNodes.Add(new DevUILabel(owner, "AdvancedShader_Label_Shape", this, new Vector2(5f, 45f), 44f, "Shape: "));
				subNodes.Add(lockNone = new Button(owner, "AdvancedShader_Button_LockNone", this, new Vector2(54f, 45f), 44f, "None"));
				subNodes.Add(lockShape = new Button(owner, "AdvancedShader_Button_LockShape", this, new Vector2(103f, 45f), 44f, "Shape"));
				subNodes.Add(lockSquare = new Button(owner, "AdvancedShader_Button_LockSquare", this, new Vector2(152f, 45f), 44f, "Square"));
				subNodes.Add(lockRect = new Button(owner, "AdvancedShader_Button_LockRect", this, new Vector2(201f, 45f), 44f, "Rect"));

				subNodes.Add(new DevUILabel(owner, "AdvancedShader_Label_Container", this, new Vector2(5f, 25f), 70f, "Container: "));
				subNodes.Add(containerLeft = new ArrowButton(owner, "AdvancedShader_Arrow_ContainerLeft", this, new Vector2(80f, 25f), -90f));
				subNodes.Add(containerLabel = new DevUILabel(owner, "AdvancedShader_Label_ContainerActual", this, new Vector2(101f, 25f), 123f, $"{(int)data.container}: {data.container}"));
				subNodes.Add(containerRight = new ArrowButton(owner, "AdvancedShader_Arrow_ContainerRight", this, new Vector2(229f, 25f), 90f));

				subNodes.Add(colorsButton = new Button(owner, "AdvancedShader_Button_Color", this, new Vector2(5f, 5f), 117f, "Colors"));
				subNodes.Add(uvsButton = new Button(owner, "AdvancedShader_Button_UVs", this, new Vector2(128f, 5f), 117f, "UVs"));
			}

			public override void Refresh()
			{
				base.Refresh();

				lockNone.overrideTextColor = null;
				lockShape.overrideTextColor = null;
				lockSquare.overrideTextColor = null;
				lockRect.overrideTextColor = null;
				switch (data.shapeLock)
				{
					case ShapeLock.None:
						lockNone.overrideTextColor = new Color(0f, 0f, 1f);
						break;
					case ShapeLock.Shape:
						lockShape.overrideTextColor = new Color(0f, 0f, 1f);
						break;
					case ShapeLock.Square:
						lockSquare.overrideTextColor = new Color(0f, 0f, 1f);
						break;
					case ShapeLock.Rect:
						lockRect.overrideTextColor = new Color(0f, 0f, 1f);
						break;
				}
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (sender == shaderSelectButton)
				{
					if (shaderSelectPanel != null)
					{
						subNodes.Remove(shaderSelectPanel);
						shaderSelectPanel.ClearSprites();
						shaderSelectPanel = null;
					}
					else
					{
						shaderSelectPanel = new CustomDecalRepresentation.SelectDecalPanel(owner, this, new Vector2(250f, 15f) - absPos, [.. Custom.rainWorld.Shaders.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)])
						{
							Title = "Select Shader"
						};
						subNodes.Add(shaderSelectPanel);
					}
				}
				else if (sender == spriteSelectButton)
				{
					if (spriteSelectPanel != null)
					{
						subNodes.Remove(spriteSelectPanel);
						spriteSelectPanel.ClearSprites();
						spriteSelectPanel = null;
					}
					else
					{
						spriteSelectPanel = new CustomDecalRepresentation.SelectDecalPanel(owner, this, new Vector2(250f, 15f) - absPos, [.. Futile.atlasManager._allElementsByName.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)])
						{
							Title = "Select Sprite"
						};
						subNodes.Add(spriteSelectPanel);
					}
				}
				else if (sender.parentNode is CustomDecalRepresentation.SelectDecalPanel selectPanel)
				{
					if (sender.IDstring == "BackPage99289..?/~")
					{
						selectPanel.PrevPage();
					}
					else if (sender.IDstring == "NextPage99289..?/~")
					{
						selectPanel.NextPage();
					}
					else
					{
						if (selectPanel == shaderSelectPanel)
						{
							data.shader = sender.IDstring;
							shaderSelectButton.Text = data.shader;
							subNodes.Remove(shaderSelectPanel);
							shaderSelectPanel.ClearSprites();
							shaderSelectPanel = null;
						}
						else if (selectPanel == spriteSelectPanel)
						{
							data.spriteName = sender.IDstring;
							data.ResetUVs();
							uvPanel?.RefreshUVs();
							spriteSelectButton.Text = data.spriteName;
							subNodes.Remove(spriteSelectPanel);
							spriteSelectPanel.ClearSprites();
							spriteSelectPanel = null;
						}
						rep.shaderInstance.CompletelyRefreshSprite();
					}
				}

				else if (sender == lockNone)
				{
					data.shapeLock = ShapeLock.None;
					Refresh();
				}
				else if (sender == lockShape)
				{
					data.shapeLock = ShapeLock.Shape;
					Refresh();
				}
				else if (sender == lockSquare)
				{
					data.shapeLock = ShapeLock.Square;
					Refresh();
				}
				else if (sender == lockRect)
				{
					data.shapeLock = ShapeLock.Rect;
					Refresh();
				}

				else if (sender == containerLeft)
				{
					data.container = (ContainerCodes)Math.Max((int)data.container - 1, 0);
					containerLabel.Text = $"{(int)data.container}: {data.container}";
					rep.shaderInstance.CompletelyRefreshSprite();
				}
				else if (sender == containerRight)
				{
					data.container = (ContainerCodes)Math.Min((int)data.container + 1, Enum.GetValues(typeof(ContainerCodes)).Cast<int>().Max());
					containerLabel.Text = $"{(int)data.container}: {data.container}";
					rep.shaderInstance.CompletelyRefreshSprite();
				}

				else if (sender == colorsButton)
				{
					if (colorPanel != null)
					{
						subNodes.Remove(colorPanel);
						colorPanel.ClearSprites();
						colorPanel = null;
					}
					else
					{
						colorPanel = new AdvancedShaderColorPanel(owner, "AdvancedShader_Color", this, new Vector2(-300f, 0f));
						colorPanel.pos -= new Vector2(0f, colorPanel.size.y - size.y);
						subNodes.Add(colorPanel);
					}
				}
				else if (sender == uvsButton)
				{
					if (uvPanel != null)
					{
						subNodes.Remove(uvPanel);
						uvPanel.ClearSprites();
						uvPanel = null;
					}
					else
					{
						uvPanel = new AdvancedShaderUVPanel(owner, "AdvancedShader_UVs", this, new Vector2(300f, 0f));
						uvPanel.pos -= new Vector2(0f, uvPanel.size.y - size.y);
						subNodes.Add(uvPanel);
					}
				}
			}
		}

		public enum ShapeLock
		{
			None,
			Shape,
			Square,
			Rect
		}
	}
}
