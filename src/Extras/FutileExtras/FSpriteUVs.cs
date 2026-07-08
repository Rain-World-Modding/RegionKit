using System;

namespace RegionKit.Extras.FutileExtras
{
	/// <summary>
	/// Variant of FSprite with some settable UV channels
	/// </summary>
	public class FSpriteUVs : FSprite
	{
		protected Vector2[] _uvs1;
		protected Vector2[] _uvs2;
		protected Vector2[] _uvs3;
		protected Vector2[] _uvs4;
		protected Vector2[] _uvs5;
		protected Vector2[] _uvs6;
		protected Vector2[] _uvs7;
		protected FQuadUVRenderLayer SpecialRenderLayer => (_renderLayer as FQuadUVRenderLayer)!;

		public FSpriteUVs(string elementName) : this(Futile.atlasManager.GetElementWithName(elementName))
		{
		}

		public FSpriteUVs(FAtlasElement atlasElement) : base(atlasElement, true)
		{
			Init(FQuadUVRenderLayer.FacetType, element, 1);
			_uvs1 = [new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f)];
			_uvs2 = [new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f)];
			_uvs3 = [new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f)];
			_uvs4 = [new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f)];
			_uvs5 = [new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f)];
			_uvs6 = [new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f)];
			_uvs7 = [new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f)];
		}

		/// <summary>
		/// Sets the UVs of a channel
		/// </summary>
		/// <param name="uv">Value to set</param>
		/// <param name="channel">Number between 1 and 7</param>
		public void SetUV(Vector2 uv, int channel)
		{
			Vector2[] array = channel switch
			{
				1 => _uvs1,
				2 => _uvs2,
				3 => _uvs3,
				4 => _uvs4,
				5 => _uvs5,
				6 => _uvs6,
				7 => _uvs7,
				_ => throw new IndexOutOfRangeException($"{nameof(channel)} was not in the specified range!")
			};

			for (int i = 0; i < array.Length; i++)
			{
				array[i] = uv;
			}

			_isMeshDirty = true;
		}

		/// <summary>
		/// Sets a UV of a channel
		/// </summary>
		/// <param name="uv">Value to set</param>
		/// <param name="index">Index of array to set</param>
		/// <param name="channel">Number between 1 and 3</param>
		public void SetUV(Vector2 uv, int index, int channel)
		{
			Vector2[] array = channel switch
			{
				1 => _uvs1,
				2 => _uvs2,
				3 => _uvs3,
				4 => _uvs4,
				5 => _uvs5,
				6 => _uvs6,
				7 => _uvs7,
				_ => throw new IndexOutOfRangeException($"{nameof(channel)} was not in the specified range!")
			};

			array[index] = uv;

			_isMeshDirty = true;
		}

		public override void PopulateRenderLayer()
		{
			base.PopulateRenderLayer();
			if (_isOnStage && _firstFacetIndex != -1)
			{
				Vector2[] uvs1 = SpecialRenderLayer.uvs1;
				Vector2[] uvs2 = SpecialRenderLayer.uvs2;
				Vector2[] uvs3 = SpecialRenderLayer.uvs3;
				Vector2[] uvs4 = SpecialRenderLayer.uvs4;
				Vector2[] uvs5 = SpecialRenderLayer.uvs5;
				Vector2[] uvs6 = SpecialRenderLayer.uvs6;
				Vector2[] uvs7 = SpecialRenderLayer.uvs7;

				int firstIndex = _firstFacetIndex * 4;
				for (int i = 0; i < 4; i++)
				{
					uvs1[firstIndex + i] = _uvs1[i];
					uvs2[firstIndex + i] = _uvs2[i];
					uvs3[firstIndex + i] = _uvs3[i];
					uvs4[firstIndex + i] = _uvs4[i];
					uvs5[firstIndex + i] = _uvs5[i];
					uvs6[firstIndex + i] = _uvs6[i];
					uvs7[firstIndex + i] = _uvs7[i];
				}
				_renderLayer.HandleVertsChange();
			}
		}
	}
}
