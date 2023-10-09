using static RegionKit.Modules.GateCustomization.GateDataRepresentations;

namespace RegionKit.Modules.GateCustomization;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "GateCustomization")]
internal class _Module
{
	public const string GATE_CUSTOMIZATION_POM_CATEGORY = RK_POM_CATEGORY + "-GateCustomization";

	// Had to make a new function for this becasue when I used RegisterEmptyObject with a ManagedData class the
	// order of the fields was not correct (think it sorted them alphabetically?).

	// And the RegisterFullyManagedObjectType which orderd things correctly did not have a option for 
	// changing the representation type which I wanted to do.
	public static void RegisterGateDataManagedObjectType(ManagedField[] managedFields, Type reprType, string name, string category)
	{
		RegisterManagedObject(new GateDataManagedObjectType(managedFields, name, category, reprType));
	}

	public class GateDataManagedObjectType : ManagedObjectType
	{
		private ManagedField[] managedFields;

		public GateDataManagedObjectType(ManagedField[] managedFields, string name, string category, Type reprType) : base(name, category, null, null, reprType)
		{
			this.managedFields = managedFields;
		}

		public override PlacedObject.Data MakeEmptyData(PlacedObject pObj)
		{
			return new ManagedData(pObj, managedFields);
		}
	}

	public static void Setup()
	{
		RegisterGateDataManagedObjectType(new ManagedField[]
		{
			new BooleanField("singleUse", false, displayName: "Single Use"),

			new BooleanField("door0Lit", false, displayName: "Left Door Lit"),
			new BooleanField("door1Lit", false, displayName: "Middle Door Lit"),
			new BooleanField("door2Lit", false, displayName: "Right Door Lit"),

			new BooleanField("noDoor0", false, displayName: "No Left Door"),
			new BooleanField("noDoor2", false, displayName: "No Right Door"),

			new BooleanField("colorOverride", false, displayName: "Karma Glyph Color Override"),
			new FloatField("hue", 0f, 1f, 0f, increment: 0.01f, displayName: "Hue"),
			new FloatField("saturation", 0f, 1f, 1f, increment: 0.01f, displayName: "Saturation"),
			new FloatField("brightness", 0f, 1f, 1f, increment: 0.01f, displayName: "Brightness"),
		}, typeof(CommonGateDataRepresentation), "CommonGateData", GATE_CUSTOMIZATION_POM_CATEGORY);

		RegisterGateDataManagedObjectType(new ManagedField[]
		{
			new BooleanField("water", true, displayName: "Water"),
			new BooleanField("bubbleFX", true, displayName: "Bubble Effect"),
			new EnumField<HeaterData>("heater0", HeaterData.Nrml, displayName: "Left Heater"),
			new EnumField<HeaterData>("heater1", HeaterData.Nrml, displayName: "Right Heater")
		}, typeof(WaterGateDataRepresentation), "WaterGateData", GATE_CUSTOMIZATION_POM_CATEGORY);

		RegisterGateDataManagedObjectType(new ManagedField[]
		{
			new BooleanField("battery", true, displayName: "Battery Visible"),

			new BooleanField("steamer0broken", false, displayName: "Left Steamer Broken"),
			new BooleanField("steamer1broken", false, displayName: "Right Steamer Broken"),

			new BooleanField("lamp0", true, displayName: "Lamp 0 Enabled"),
			new BooleanField("lamp1", true, displayName: "Lamp 1 Enabled"),
			new BooleanField("lamp2", true, displayName: "Lamp 2 Enabled"),
			new BooleanField("lamp3", true, displayName: "Lamp 3 Enabled"),

			new BooleanField("lampColorOverride", false, displayName: "Lamp Color Override"),
			new FloatField("lampHue", 0f, 1f, 0f, increment: 0.01f, displayName: "Hue"),
			new FloatField("lampSaturation", 0f, 1f, 1f, increment: 0.01f, displayName: "Saturation"),

	        new BooleanField("batteryColorOverride", false, displayName: "Battery Color Override"),
			new FloatField("batteryHue", 0f, 1f, 0f, increment: 0.01f, displayName: "Hue"),
			new FloatField("batterySaturation", 0f, 1f, 1f, increment: 0.01f, displayName: "Saturation"),
			new FloatField("batteryLightness", 0f, 1f, 0.5f, increment: 0.01f, displayName: "Lightness")
		}, typeof(ElectricGateDataRepresentation), "ElectricGateData", GATE_CUSTOMIZATION_POM_CATEGORY);

	}

	public static void Enable()
	{
		GateCustomization.Enable();
	}

	public static void Disable()
	{
		GateCustomization.Disable();
	}
}
