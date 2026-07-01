namespace RegionKit.Extras.FutileExtras
{
	/// <summary>
	/// A quad Futile render layer with 8 UV channels
	/// </summary>
	public class FQuadUVRenderLayer : FQuadRenderLayer
	{
		public static readonly FFacetType FacetType;

		static FQuadUVRenderLayer()
		{
			FacetType = FFacetType.CreateFacetType("RKQuadUV", 10, 10, 60, new FFacetType.CreateRenderLayerDelegate(CreateRenderLayer));
			On.FFacetRenderLayer.UpdateMeshProperties += FFacetRenderLayer_UpdateMeshProperties;
		}

		private static FFacetRenderLayer CreateRenderLayer(FStage stage, FFacetType facetType, FAtlas atlas, FShader shader)
		{
			return new FQuadUVRenderLayer(stage, facetType, atlas, shader);
		}

		private static void FFacetRenderLayer_UpdateMeshProperties(On.FFacetRenderLayer.orig_UpdateMeshProperties orig, FFacetRenderLayer self)
		{
			orig(self);
			if (self is FQuadUVRenderLayer quadUVRenderLayer)
			{
				quadUVRenderLayer.OverrideUpdateMeshProperties();
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

		protected FQuadUVRenderLayer(FStage stage, FFacetType facetType, FAtlas atlas, FShader shader) : base(stage, facetType, atlas, shader)
		{
		}

		public override void ShrinkMaxFacetLimit(int deltaDecrease)
		{
			if (deltaDecrease <= 0) return;

			base.ShrinkMaxFacetLimit(deltaDecrease);

			Array.Resize(ref _uvs1, _maxFacetCount * 4);
			Array.Resize(ref _uvs2, _maxFacetCount * 4);
			Array.Resize(ref _uvs3, _maxFacetCount * 4);
			Array.Resize(ref _uvs4, _maxFacetCount * 4);
			Array.Resize(ref _uvs5, _maxFacetCount * 4);
			Array.Resize(ref _uvs6, _maxFacetCount * 4);
			Array.Resize(ref _uvs7, _maxFacetCount * 4);
		}

		public override void ExpandMaxFacetLimit(int deltaIncrease)
		{
			if (deltaIncrease <= 0) return;

			base.ExpandMaxFacetLimit(deltaIncrease);

			Array.Resize(ref _uvs1, _maxFacetCount * 4);
			Array.Resize(ref _uvs2, _maxFacetCount * 4);
			Array.Resize(ref _uvs3, _maxFacetCount * 4);
			Array.Resize(ref _uvs4, _maxFacetCount * 4);
			Array.Resize(ref _uvs5, _maxFacetCount * 4);
			Array.Resize(ref _uvs6, _maxFacetCount * 4);
			Array.Resize(ref _uvs7, _maxFacetCount * 4);
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
