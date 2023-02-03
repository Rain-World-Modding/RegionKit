namespace RegionKit.Modules.Machinery.V1;

/// <summary>
/// Provides visual settings to machinery objects
/// </summary>
public class MachineryCustomizer : ManagedData
{
#pragma warning disable 1591
	[StringField("element", "pixel", "Atlas element")]
	public string elementName = "pixel";
	[StringField("shader", "Basic", displayName: "Shader")]
	public string shaderName = "Basic";
	//[StringField("container", "Items", displayName:"rCam container")]
	[EnumField<ContainerCodes>("containerCode", ContainerCodes.Items, displayName: "Container")]
	public ContainerCodes containerName;
	[FloatField("scX", 0f, 35f, 1f, increment: 0.1f, ManagedFieldWithPanel.ControlType.text, displayName: "X scale")]
	public float scaleX = 1f;
	[FloatField("scY", 0f, 35f, 1f, increment: 0.1f, ManagedFieldWithPanel.ControlType.text, displayName: "Y scale")]
	public float scaleY = 1f;
	[FloatField("addRot", -90f, 90f, 0f, increment: 0.5f, displayName: "Additional rotation")]
	public float addedRotation = 0f;
	[ColorField("sCol", 1f, 0f, 0f, 1f, DisplayName: "Color")]
	public Color spriteColor;
	[EnumField<MachineryID>("amID", MachineryID.Piston, displayName: "Affected machinery")]
	public MachineryID affectedMachinesID;
	[FloatField("alpha", 0f, 1f, 1f, increment: 0.01f, ManagedFieldWithPanel.ControlType.text, "Alpha")]
	public float alpha;
	[FloatField("anchX", -10f, 10f, 0.5f, control: ManagedFieldWithPanel.ControlType.text, displayName: "X anchor")]
	public float anchorX;
	[FloatField("anchY", -10f, 10f, 0.5f, control: ManagedFieldWithPanel.ControlType.text, displayName: "Y anchor")]
	public float anchorY;
#pragma warning restore 1591
	/// <summary>
	/// Whether a given point belongs to this customizer
	/// </summary>
	/// <param name="p"></param>
	/// <returns></returns>
	public bool AffectsInPoint(Vector2 p)
	{
		return (p - owner.pos).sqrMagnitude < GetValue<Vector2>("radius").magnitude;
	}

	internal void BringToKin(FSprite other)
	{
		other.color = spriteColor;
		other.alpha = alpha;
		other.scaleX = scaleX;
		other.scaleY = scaleY;
		other.anchorX = anchorX;
		other.anchorY = anchorY;
		try { other.element = Futile.atlasManager.GetElementWithName(elementName); }
		catch { other.element = Futile.atlasManager.GetElementWithName("pixel"); }
		try { other.shader = __RW.Shaders[shaderName]; }
		catch { other.shader = FShader.defaultShader; }
	}
	/// <summary>
	/// Creates a blank instance
	/// </summary>
	public MachineryCustomizer(PlacedObject? owner) :
		base(owner!, null)
	{ }
}
