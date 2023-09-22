using System.Drawing.Text;
using static RegionKit.Modules.ConcealedGarden.CGCosmeticLeaves;
using static RegionKit.Modules.ConcealedGarden.CGElectricArcs;
using static RegionKit.Modules.ConcealedGarden.CGSlipperySlope;

namespace RegionKit.Modules.ConcealedGarden;

[RegionKitModule(nameof(Enable), nameof(Disable), setupMethod: nameof(Setup), moduleName: "The Mast")]
internal static class _Module
{
	const string CG_POM_CATEGORY = RK_POM_CATEGORY + "-CG";
	public static void Setup()
	{
		RegisterManagedObject(new ManagedObjectType("CGDrySpot", CG_POM_CATEGORY,
			typeof(CGDrySpot), typeof(CGDrySpot.CGDrySpotData), typeof(ManagedRepresentation)));

		RegisterFullyManagedObjectType(new ManagedField[]
		{
				new BooleanField("noleft", false, displayName:"No Left Door"),
				new BooleanField("noright", false, displayName:"No Right Door"),
				new BooleanField("nowater", false, displayName:"No Water"),
				new BooleanField("zdontstop", false, displayName:"Dont cut song"),
				new FloatField("elecpos", -1000f, 1000f, -64f, 1f, ManagedFieldWithPanel.ControlType.text, "battery pos"),
		}, typeof(CGGateCustomization), "CGGateCustomization", CG_POM_CATEGORY);

		//needs sprites
		//RegisterManagedObject(new ManagedObjectType("CGBunkerShelterFlap", RK_POM_CATEGORY, typeof(CGBunkerShelterParts.CGBunkerShelterFlap), typeof(CGBunkerShelterParts.CGBunkerShelterFlapData), typeof(ManagedRepresentation)));

		RegisterManagedObject(new ManagedObjectType("CGCosmeticLeaves", CG_POM_CATEGORY,
			typeof(CGCosmeticLeaves), typeof(CosmeticLeavesObjectData), typeof(ManagedRepresentation)));

		//needs graphics fix
		RegisterManagedObject(new ManagedObjectType("CGCosmeticWater", CG_POM_CATEGORY, typeof(CGCosmeticWater), typeof(CGCosmeticWater.CGCosmeticWaterData), typeof(ManagedRepresentation)));


		RegisterManagedObject(new ManagedObjectType("CGElectricArc", CG_POM_CATEGORY, typeof(CGElectricArc), typeof(CGElectricArcData), typeof(ManagedRepresentation)));
		RegisterManagedObject(new ManagedObjectType("CGElectricArcGenerator", CG_POM_CATEGORY, typeof(CGElectricArcGenerator), typeof(CGElectricArcGeneratorData), typeof(ManagedRepresentation)));

		RegisterManagedObject(new ManagedObjectType("CGGravityGradient", CG_POM_CATEGORY,
			typeof(CGGravityGradient), typeof(CGGravityGradient.CGGravityGradientData), typeof(ManagedRepresentation)));


		RegisterManagedObject(CGNoLurkArea.noLurkType = new ManagedObjectType("CGNoLurkArea", CG_POM_CATEGORY, null,
			typeof(CGNoLurkArea.CGNoLurkAreaData), typeof(ManagedRepresentation)));

		RegisterFullyManagedObjectType(new ManagedField[]
		{
				new FloatField("rmin", 0, 1, 0.1f, 0.001f),
				new FloatField("rmax", 0, 1, 0.3f, 0.001f),
				new FloatField("gmin", 0, 1, 0.05f, 0.001f),
				new FloatField("gmax", 0, 1, 0.2f, 0.001f),
				new FloatField("bmin", 0, 1, 0.5f, 0.001f),
				new FloatField("bmax", 0, 1, 0.25f, 0.001f),
				new FloatField("stiff", 0, 1, 0.5f, 0.01f),
			//new IntegerField("ftc", 0, 400, 120, ManagedFieldWithPanel.ControlType.slider),
		}, typeof(CGOrganicShelter.CGOrganicShelterCoordinator), "CGOrganicShelterCoordinator", CG_POM_CATEGORY);

		RegisterFullyManagedObjectType(new ManagedField[]
		{
				new Vector2Field("size", new UnityEngine.Vector2(40,40), Vector2Field.VectorReprType.circle),
				new Vector2Field("dest", new UnityEngine.Vector2(0,50), Vector2Field.VectorReprType.line),
				new FloatField("stiff", 0, 1, 0.5f, 0.01f),
		}, null, "CGOrganicLockPart", CG_POM_CATEGORY);

		RegisterFullyManagedObjectType(new ManagedField[]
		{
				new Vector2Field("size", new UnityEngine.Vector2(-100,100), Vector2Field.VectorReprType.circle),
				new FloatField("sizemin", 1, 200, 12f, 1f),
				new FloatField("sizemax", 1, 200, 20f, 1f),
				new FloatField("depth", -100, 100, 4f, 1f),
				new FloatField("density", 0, 5, 0.5f, 0.01f),
				new FloatField("stiff", 0, 1, 0.5f, 0.01f),
				new FloatField("spread", 0, 20f, 2f, 0.1f),
				new IntegerField("seed", 0, 9999, 0),
		}, null, "CGOrganicLining", CG_POM_CATEGORY);


		RegisterManagedObject(new ManagedObjectType("CGSlipperySlope", CG_POM_CATEGORY,
			typeof(CGSlipperySlope), typeof(CGSlipperySlopeData), typeof(ManagedRepresentation)));


		RegisterManagedObject(new ManagedObjectType("CGShelterRain", CG_POM_CATEGORY, typeof(CGShelterRain), null, null));


		RegisterManagedObject(new ManagedObjectType("CGSongSFXTrigger", CG_POM_CATEGORY,
			typeof(CGSongSFXTrigger), typeof(CGSongSFXTrigger.CGSongSFXTriggerData), typeof(ManagedRepresentation)));
		RegisterManagedObject(new ManagedObjectType("CGSongSFXGradient", CG_POM_CATEGORY,
			typeof(CGSongSFXGradient), typeof(CGSongSFXGradient.CGSongSFXGradientData), typeof(ManagedRepresentation)));


		RegisterManagedObject(new ManagedObjectType("CGLifeSimProjectionSegment", CG_POM_CATEGORY,
			typeof(CGLifeSimProjection), typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation), singleInstance: true));
		//PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("LifeSimProjectionPulser",
		//    typeof(LifeSimProjection), typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation), singleInstance: true));
		//PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("LifeSimProjectionKiller",
		//    typeof(LifeSimProjection), typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation), singleInstance: true));

		RegisterFullyManagedObjectType(new ManagedField[]
		{
			new EnumField<Objects.Drawable.FContainer>("container", Objects.Drawable.FContainer.Foreground, displayName: "FContainer"),
			new StringField("shader", "Basic", "Shader"),
			new FloatField("alpha", 0f, 1f, 0f, 0.01f, ManagedFieldWithPanel.ControlType.slider, "amount")
		}, typeof(CGCameraEffects.CGCameraEffectsObj), "CGFullScreenShader", CG_POM_CATEGORY);
	}

	public static void Enable()
	{

		CGDrySpot.Hooks.Apply();
		CGCameraEffects.Apply();
		CGNoLurkArea.Apply();
		CGShelterRain.Apply();
	}
	public static void Disable()
	{
		CGDrySpot.Hooks.Undo();
		CGCameraEffects.Undo();
		CGNoLurkArea.Undo();
		CGShelterRain.Undo();
	}
}
