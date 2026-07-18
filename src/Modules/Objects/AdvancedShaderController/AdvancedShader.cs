using System.Globalization;
using System.IO;
using RegionKit.Extras.FutileExtras;

namespace RegionKit.Modules.Objects.AdvancedShaderController
{
	public class AdvancedShader : CosmeticSprite
	{
		private const string VERSION_ID = "VER2";

		public PlacedObject pObj;
		public Data data => (pObj.data as Data)!;
		private bool _needsRefresh = false;

		public AdvancedShader(PlacedObject pObj)
		{
			this.pObj = pObj;
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[1];
			List<TriangleMesh.Triangle> tris = [];
			for (int i = 0; i < data.vertices.Length - 2; i++)
			{
				tris.Add(new TriangleMesh.Triangle(i, i + 1, i + 2));
			}
			sLeaser.sprites[0] = new TriangleMeshUVs(data.LoadAndGetSpriteName(), [.. tris], true, false)
			{
				shader = rCam.game.rainWorld.Shaders.TryGetValue(data.shader, out FShader shader) ? shader : FShader.Basic
			};
			AddToContainer(sLeaser, rCam, null!);
		}

		public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			base.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer(data.container.ToString()));
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			TriangleMeshUVs mesh = (sLeaser.sprites[0] as TriangleMeshUVs)!;

			for (int i = 0; i < mesh.vertices.Length; i++)
			{
				mesh.verticeColors[i] = data.colors[i];
				for (int j = 0; j < 8; j++)
				{
					mesh.SetUV(data.uvs[j][i], i, j);
				}
				mesh.MoveVertice(i, VertexPos(i));
			}

			if (_needsRefresh)
			{
				_needsRefresh = false;
				mesh.RemoveFromContainer();
				InitiateSprites(sLeaser, rCam);
			}

			Vector2 VertexPos(int index) => pObj.pos + data.vertices[index] - camPos;
		}

		/// <summary>
		/// Do not use this unless you change the element, shader, or container
		/// </summary>
		public void CompletelyRefreshSprite()
		{
			_needsRefresh = true;
		}

		public class Data : PlacedObject.Data
		{
			public Vector2[] vertices;
			public Vector2[][] uvs;
			public Color[] colors;

			public Vector2 panelPos = new Vector2(0, 150f);

			public bool restrictUVs = true;
			public bool restrictColors = true;
			public bool lockColors = true;
			public bool lockUVs = true;

			public string shader = "Basic";
			public string spriteName = "Futile_White";
			public ContainerCodes container = ContainerCodes.Foreground;

			public string filePath = "illustrations/icon0.png";
			public bool useFile = false;

			public AdvancedShaderRepresentation.ShapeLock shapeLock = AdvancedShaderRepresentation.ShapeLock.None; // this does not get saved

			public Data(PlacedObject owner) : base(owner)
			{
				vertices = [new Vector2(100f, 0f), new Vector2(100f, 100f), new Vector2(200f, 0f), new Vector2(200f, 100f)];
				uvs = new Vector2[8][];
				for (int i = 0; i < uvs.Length; i++)
				{
					uvs[i] = [new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(1f, 1f)];
				}
				colors = [Color.white, Color.white, Color.white, Color.white];
			}

			public string LoadAndGetSpriteName()
			{
				// Do we need to load as file?
				if (useFile)
				{
					// Get file to load
					string fullFilePath = AssetManager.ResolveFilePath(filePath);
					if (!File.Exists(fullFilePath))
					{
						// Doesn't exist, so load something that we do know exists
						filePath = "illustrations/icon0.png";
						fullFilePath = AssetManager.ResolveFilePath(filePath);
					}

					// Make sure it is loaded
					if (!Futile.atlasManager.DoesContainAtlas(filePath))
					{
						Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
						AssetManager.SafeWWWLoadTexture(ref tex, "file:///" + fullFilePath, true, true);
						Futile.atlasManager.LoadAtlasFromTexture(filePath, tex, false);
					}

					// Return
					return filePath;
				}

				// Return sprite, or Futile_White if we can't for some reason
				if (!Futile.atlasManager.DoesContainElementWithName(spriteName))
				{
					return "Futile_White";
				}
				return spriteName;
			}

			public void ResetUVs(int channel)
			{
				if (channel == 0 && Futile.atlasManager.TryGetElementWithName(LoadAndGetSpriteName(), out FAtlasElement? el) && el is not null)
				{
					uvs[channel] = [el.uvBottomLeft, el.uvTopLeft, el.uvBottomRight, el.uvTopRight];
				}
				else
				{
					uvs[channel] = [new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(1f, 1f)];
				}
			}

			public void ResetColors()
			{
				colors = [Color.white, Color.white, Color.white, Color.white];
			}

			public override string ToString()
			{
				string text = string.Format(CultureInfo.InvariantCulture,
					"{0}|{1}~{2}~{3}~{4}~{5}~{6}~{7}~{8}~{9}~{10}~{11}~{12}",
					VERSION_ID,
					panelPos.x, panelPos.y,
					shader,
					spriteName,
					container,
					useFile,
					filePath,
					restrictUVs,
					restrictColors,
					lockColors,
					lockUVs,
					vertices.Length
					);
				for (int i = 0; i < vertices.Length; i++)
				{
					text += string.Format(CultureInfo.InvariantCulture,
						"|{0}~{1}~{2}~{3}~{4}~{5}",
						vertices[i].x, vertices[i].y,
						colors[i].r, colors[i].g, colors[i].b, colors[i].a
						);
					for (int j = 0; j < uvs.Length; j++)
					{
						text += string.Format(CultureInfo.InvariantCulture,
							"~{0}~{1}",
							uvs[j][i].x,
							uvs[j][i].y
							);
					}
				}
				return text;
			}

			public override void FromString(string s)
			{
				string[] splitFull = s.Split('|');

				if (splitFull[0] != VERSION_ID)
				{
					LegacyFromString(splitFull[0].Split('~'));
					return;
				}
				if (splitFull.Length < 2) return;

				string[] mainData = splitFull[1].Split('~');
				int i = 0;
				if (mainData.Length > i) _ = float.TryParse(mainData[i++], out panelPos.x);
				if (mainData.Length > i) _ = float.TryParse(mainData[i++], out panelPos.y);
				if (mainData.Length > i) shader = mainData[i++];
				if (mainData.Length > i) spriteName = mainData[i++];
				if (mainData.Length > i) _ = Enum.TryParse(mainData[i++], out container);
				if (mainData.Length > i) _ = bool.TryParse(mainData[i++], out useFile);
				if (mainData.Length > i) filePath = mainData[i++];
				if (mainData.Length > i) _ = bool.TryParse(mainData[i++], out restrictUVs);
				if (mainData.Length > i) _ = bool.TryParse(mainData[i++], out restrictColors);
				if (mainData.Length > i) _ = bool.TryParse(mainData[i++], out lockColors);
				if (mainData.Length > i) _ = bool.TryParse(mainData[i++], out lockUVs);


				if (mainData.Length > i && int.TryParse(mainData[i++], out int numVertices) && numVertices >= 4)
				{
					vertices = new Vector2[numVertices];
					uvs = new Vector2[8][];
					for (int j = 0; j < uvs.Length; j++)
					{
						uvs[j] = new Vector2[numVertices];
					}
					colors = new Color[numVertices];
					for (int j = 2; j < splitFull.Length; j++)
					{
						string[] subData = splitFull[j].Split('~');
						i = 0;
						int v = j - 2;

						_ = float.TryParse(subData[i++], out vertices[v].x);
						_ = float.TryParse(subData[i++], out vertices[v].y);
						_ = float.TryParse(subData[i++], out colors[v].r);
						_ = float.TryParse(subData[i++], out colors[v].g);
						_ = float.TryParse(subData[i++], out colors[v].b);
						_ = float.TryParse(subData[i++], out colors[v].a);

						for (int k = 0; k < 8; k++)
						{
							_ = float.TryParse(subData[i++], out uvs[k][v].x);
							_ = float.TryParse(subData[i++], out uvs[k][v].y);
						}
					}
				}
			}

			private void LegacyFromString(string[] split)
			{
				int i = 0;
				if (split.Length > i) _ = float.TryParse(split[i++], out panelPos.x);
				if (split.Length > i) _ = float.TryParse(split[i++], out panelPos.y);
				if (split.Length > i) shader = split[i++];
				if (split.Length > i) spriteName = split[i++];
				if (split.Length > i) _ = Enum.TryParse(split[i++], out container);
				if (split.Length > i) _ = bool.TryParse(split[i++], out useFile);
				if (split.Length > i) filePath = split[i++];
				if (split.Length > i) _ = bool.TryParse(split[i++], out restrictUVs);
				if (split.Length > i) _ = bool.TryParse(split[i++], out restrictColors);
				if (split.Length > i) _ = bool.TryParse(split[i++], out lockColors);
				if (split.Length > i) _ = bool.TryParse(split[i++], out lockUVs);
				if (split.Length > i && int.TryParse(split[i++], out int numVertices) && numVertices >= 4)
				{
					vertices = new Vector2[numVertices];
					uvs = new Vector2[8][];
					for (int j = 0; j < uvs.Length; j++)
					{
						uvs[j] = new Vector2[numVertices];
					}
					colors = new Color[numVertices];
					for (int j = 0; j < numVertices && split.Length >= 12 + 8 * (j + 1); j++)
					{
						_ = float.TryParse(split[12 + 8 * j + 0], out vertices[j].x);
						_ = float.TryParse(split[12 + 8 * j + 1], out vertices[j].y);
						_ = float.TryParse(split[12 + 8 * j + 2], out uvs[0][j].x);
						_ = float.TryParse(split[12 + 8 * j + 3], out uvs[0][j].y);
						_ = float.TryParse(split[12 + 8 * j + 4], out colors[j].r);
						_ = float.TryParse(split[12 + 8 * j + 5], out colors[j].g);
						_ = float.TryParse(split[12 + 8 * j + 6], out colors[j].b);
						_ = float.TryParse(split[12 + 8 * j + 7], out colors[j].a);
					}
				}
			}
		}
	}
}
