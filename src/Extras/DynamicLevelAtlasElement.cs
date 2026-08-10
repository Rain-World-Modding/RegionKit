using Watcher;

namespace RegionKit.Extras
{
	public class DynamicLevelAtlasElement : DynamicLevelElement
	{
		private static readonly Vector2 InverseMagicScale = new Vector2(1f / 16f, 1f / 16f);

		private Vector2[] uvs;
		private FAtlasElement currentElement;
		private bool atlasDirty = false;

		private void SetUVs()
		{
			uvs = [currentElement.uvBottomLeft, currentElement.uvTopLeft, currentElement.uvBottomRight, currentElement.uvTopRight];
		}

		public FAtlasElement AtlasElement
		{
			get => currentElement;
			set
			{
				if (currentElement != value)
				{
					currentElement = value;
					atlasDirty = true;
				}
			}
		}

		public DynamicLevelAtlasElement(Vector2 pos, Vector2 scale, string atlasElement, int depthOffset = 0, string? shaderOverride = null) 
			: this(pos, scale, Futile.atlasManager.GetElementWithName(atlasElement), depthOffset, shaderOverride)
		{
		}


		public DynamicLevelAtlasElement(Vector2 pos, Vector2 scale, FAtlasElement atlasElement, int depthOffset = 0, string? shaderOverride = null) 
			: base(pos, scale, atlasElement.atlas.texture, depthOffset, MaskSource.CreateQuadMesh(), shaderOverride)
		{
			currentElement = atlasElement;
			uvs = [atlasElement.uvBottomLeft, atlasElement.uvTopLeft, atlasElement.uvBottomRight, atlasElement.uvTopRight];
		}

		public override void Update(bool eu)
		{
			Vector2 actualOldScale = element?.scale ?? (scale * InverseMagicScale * currentElement.sourcePixelSize);
			base.Update(eu);
			if (room.BeingViewed && element != null)
			{
				if (atlasDirty)
				{
					atlasDirty = false;
					element.SetTexture(currentElement.atlas.texture);
					SetUVs();
				}
				element.meshFilter.mesh.uv = uvs;
				element.oldScale = actualOldScale;
				element.scale = scale * InverseMagicScale * currentElement.sourcePixelSize;
			}
		}
	}
}
