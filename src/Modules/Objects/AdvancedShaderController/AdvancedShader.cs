using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Modules.Objects.AdvancedShaderController
{
	public class AdvancedShader : CosmeticSprite
	{
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
			sLeaser.sprites[0] = new TriangleMesh(data.LoadAndGetSpriteName(), [.. tris], true, false)
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
			TriangleMesh mesh = (sLeaser.sprites[0] as TriangleMesh)!;

			for (int i = 0; i < mesh.vertices.Length; i++)
			{
				mesh.verticeColors[i] = data.colors[i];
				mesh.UVvertices[i] = data.uvs[i];
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
			public Vector2[] uvs;
			public Color[] colors;

			public Vector2 panelPos = new Vector2(0, 150f);

			public bool restrictUVs = true;
			public bool restrictColors = true;
			public bool lockColors = true;
			public bool lockUVs = true;

			public string shader = "Basic";
			public string spriteName = "Futile_White";
			public ContainerCodes container = ContainerCodes.Foreground;

			public List<string> folderPath = ["illustrations", "icon0.png"];
			public bool useFile = false;

			public AdvancedShaderRepresentation.ShapeLock shapeLock = AdvancedShaderRepresentation.ShapeLock.None; // this does not get saved

			public Data(PlacedObject owner) : base(owner)
			{
				vertices = [new Vector2(100f, 0f), new Vector2(100f, 100f), new Vector2(200f, 0f), new Vector2(200f, 100f)];
				uvs = [new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(1f, 1f)];
				colors = [Color.white, Color.white, Color.white, Color.white];
			}

			public string LoadAndGetSpriteName()
			{
				// Do we need to load as file?
				if (useFile)
				{
					// Get file to load
					string filePath = string.Join("/", folderPath);
					string fullFilePath = AssetManager.ResolveFilePath(filePath);
					if (!File.Exists(fullFilePath))
					{
						// Doesn't exist, so load something that we do know exists
						folderPath = ["illustrations", "icon0.png"];
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

			public void ResetUVs()
			{
				if (Futile.atlasManager.TryGetElementWithName(LoadAndGetSpriteName(), out FAtlasElement? el) && el is not null)
				{
					uvs = [el.uvBottomLeft, el.uvTopLeft, el.uvBottomRight, el.uvTopRight];
				}
				else
				{
					uvs = [new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(1f, 1f)];
				}
			}

			public void ResetColors()
			{
				colors = [Color.white, Color.white, Color.white, Color.white];
			}

			public override string ToString()
			{
				string text = string.Format(CultureInfo.InvariantCulture,
					"{0}~{1}~{2}~{3}~{4}~{5}~{6}~{7}~{8}~{9}~{10}~{11}",
					panelPos.x, panelPos.y,
					shader,
					spriteName,
					container,
					useFile,
					string.Join("/", folderPath),
					restrictUVs,
					restrictColors,
					lockColors,
					lockUVs,
					vertices.Length
					);
				for (int i = 0; i < vertices.Length; i++)
				{
					text += string.Format(CultureInfo.InvariantCulture,
						"~{0}~{1}~{2}~{3}~{4}~{5}~{6}~{7}",
						vertices[i].x, vertices[i].y,
						uvs[i].x, uvs[i].y,
						colors[i].r, colors[i].g, colors[i].b, colors[i].a
						);
				}
				return text;
			}

			public override void FromString(string s)
			{
				string[] split = s.Split('~');
				if (split.Length > 0) _ = float.TryParse(split[0], out panelPos.x);
				if (split.Length > 1) _ = float.TryParse(split[1], out panelPos.y);
				if (split.Length > 2) shader = split[2];
				if (split.Length > 3) spriteName = split[3];
				if (split.Length > 4) _ = Enum.TryParse(split[4], out container);
				if (split.Length > 5) _ = bool.TryParse(split[5], out useFile);
				if (split.Length > 6) folderPath = [.. split[6].Split('/')];
				if (split.Length > 7) _ = bool.TryParse(split[7], out restrictUVs);
				if (split.Length > 8) _ = bool.TryParse(split[8], out restrictColors);
				if (split.Length > 9) _ = bool.TryParse(split[9], out lockColors);
				if (split.Length > 10) _ = bool.TryParse(split[10], out lockUVs);
				if (split.Length > 11 && int.TryParse(split[11], out int numVertices) && numVertices >= 4)
				{
					vertices = new Vector2[numVertices];
					uvs = new Vector2[numVertices];
					colors = new Color[numVertices];
					for (int i = 0; i < numVertices && split.Length >= 12 + 8 * (i + 1); i++)
					{
						_ = float.TryParse(split[12 + 8 * i + 0], out vertices[i].x);
						_ = float.TryParse(split[12 + 8 * i + 1], out vertices[i].y);
						_ = float.TryParse(split[12 + 8 * i + 2], out uvs[i].x);
						_ = float.TryParse(split[12 + 8 * i + 3], out uvs[i].y);
						_ = float.TryParse(split[12 + 8 * i + 4], out colors[i].r);
						_ = float.TryParse(split[12 + 8 * i + 5], out colors[i].g);
						_ = float.TryParse(split[12 + 8 * i + 6], out colors[i].b);
						_ = float.TryParse(split[12 + 8 * i + 7], out colors[i].a);
					}
				}
			}
		}
	}
}
