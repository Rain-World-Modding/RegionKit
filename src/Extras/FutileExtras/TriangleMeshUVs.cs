using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Extras.FutileExtras
{
	public class TriangleMeshUVs : TriangleMesh
	{
		protected Vector2[] _uvs1;
		protected Vector2[] _uvs2;
		protected Vector2[] _uvs3;
		protected Vector2[] _uvs4;
		protected Vector2[] _uvs5;
		protected Vector2[] _uvs6;
		protected Vector2[] _uvs7;
		protected FTriangleUVRenderLayer SpecialRenderLayer => (_renderLayer as FTriangleUVRenderLayer)!;

		public TriangleMeshUVs(string imageName, Triangle[] tris, bool customColor, bool atlasedImage = false) : base(imageName, tris, customColor, atlasedImage)
		{
			Init(FTriangleUVRenderLayer.FacetType, element, 1);
			_uvs1 = new Vector2[vertices.Length];
			_uvs2 = new Vector2[vertices.Length];
			_uvs3 = new Vector2[vertices.Length];
			_uvs4 = new Vector2[vertices.Length];
			_uvs5 = new Vector2[vertices.Length];
			_uvs6 = new Vector2[vertices.Length];
			_uvs7 = new Vector2[vertices.Length];
		}

		/// <summary>
		/// Sets the UVs of a channel
		/// </summary>
		/// <param name="uv">Value to set</param>
		/// <param name="channel">Number between 0 and 7</param>
		public void SetUVs(Vector2 uv, int channel)
		{
			Vector2[] array = channel switch
			{
				0 => UVvertices,
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
		/// <param name="channel">Number between 0 and 7</param>
		public void SetUV(Vector2 uv, int index, int channel)
		{
			Vector2[] array = channel switch
			{
				0 => UVvertices,
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

				int firstIndex = _firstFacetIndex * 3;
				for (int i = 0; i < this.triangles.Length; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						int index = j switch
						{
							0 => triangles[i].a,
							1 => triangles[i].b,
							2 => triangles[i].c,
							_ => throw new InvalidOperationException()
						};

						int k = firstIndex + i * 3 + j;

						uvs1[k] = _uvs1[index];
						uvs2[k] = _uvs2[index];
						uvs3[k] = _uvs3[index];
						uvs4[k] = _uvs4[index];
						uvs5[k] = _uvs5[index];
						uvs6[k] = _uvs6[index];
						uvs7[k] = _uvs7[index];
					}
				}

				_renderLayer.HandleVertsChange();
			}
		}
	}
}
