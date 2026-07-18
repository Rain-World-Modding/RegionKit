using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Extras.FutileExtras
{
	public class FTriangleUVRenderLayer : FTriangleRenderLayer
	{
		public static readonly FFacetType FacetType;

		static FTriangleUVRenderLayer()
		{
			FacetType = FFacetType.CreateFacetType("RKTriUV", 10, 10, 60, new FFacetType.CreateRenderLayerDelegate(CreateRenderLayer));
			On.FFacetRenderLayer.UpdateMeshProperties += FFacetRenderLayer_UpdateMeshProperties;
		}

		private static FFacetRenderLayer CreateRenderLayer(FStage stage, FFacetType facetType, FAtlas atlas, FShader shader)
		{
			return new FTriangleUVRenderLayer(stage, facetType, atlas, shader);
		}

		private static void FFacetRenderLayer_UpdateMeshProperties(On.FFacetRenderLayer.orig_UpdateMeshProperties orig, FFacetRenderLayer self)
		{
			orig(self);
			if (self is FTriangleUVRenderLayer triUVRenderLayer)
			{
				triUVRenderLayer.OverrideUpdateMeshProperties();
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		protected Vector2[] _uvs1 = [];
		protected Vector2[] _uvs2 = [];
		protected Vector2[] _uvs3 = [];
		protected Vector2[] _uvs4 = [];
		protected Vector2[] _uvs5 = [];
		protected Vector2[] _uvs6 = [];
		protected Vector2[] _uvs7 = [];

		public Vector2[] uvs1 => _uvs1;
		public Vector2[] uvs2 => _uvs2;
		public Vector2[] uvs3 => _uvs3;
		public Vector2[] uvs4 => _uvs4;
		public Vector2[] uvs5 => _uvs5;
		public Vector2[] uvs6 => _uvs6;
		public Vector2[] uvs7 => _uvs7;

		private FTriangleUVRenderLayer(FStage stage, FFacetType facetType, FAtlas atlas, FShader shader) : base(stage, facetType, atlas, shader)
		{
		}

		public override void ShrinkMaxFacetLimit(int deltaDecrease)
		{
			if (deltaDecrease <= 0) return;

			base.ShrinkMaxFacetLimit(deltaDecrease);

			Array.Resize(ref _uvs1, _maxFacetCount * 3);
			Array.Resize(ref _uvs2, _maxFacetCount * 3);
			Array.Resize(ref _uvs3, _maxFacetCount * 3);
			Array.Resize(ref _uvs4, _maxFacetCount * 3);
			Array.Resize(ref _uvs5, _maxFacetCount * 3);
			Array.Resize(ref _uvs6, _maxFacetCount * 3);
			Array.Resize(ref _uvs7, _maxFacetCount * 3);
		}

		public override void ExpandMaxFacetLimit(int deltaIncrease)
		{
			if (deltaIncrease <= 0) return;

			base.ExpandMaxFacetLimit(deltaIncrease);

			Array.Resize(ref _uvs1, _maxFacetCount * 3);
			Array.Resize(ref _uvs2, _maxFacetCount * 3);
			Array.Resize(ref _uvs3, _maxFacetCount * 3);
			Array.Resize(ref _uvs4, _maxFacetCount * 3);
			Array.Resize(ref _uvs5, _maxFacetCount * 3);
			Array.Resize(ref _uvs6, _maxFacetCount * 3);
			Array.Resize(ref _uvs7, _maxFacetCount * 3);
		}

		protected virtual void OverrideUpdateMeshProperties()
		{
			_mesh.SetUVs(1, _uvs1);
			_mesh.SetUVs(2, _uvs2);
			_mesh.SetUVs(3, _uvs3);
			_mesh.SetUVs(4, _uvs4);
			_mesh.SetUVs(5, _uvs5);
			_mesh.SetUVs(6, _uvs6);
			_mesh.SetUVs(7, _uvs7);
		}
	}
}
