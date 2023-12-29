namespace RegionKit.Modules.Machinery.V2;

public struct PartVisuals
{
	public string atlasElement;
	public string shader;
	public ContainerCodes container;
	public Color color;
	public float alpha;
	public float scaleX;
	public float scaleY;
	public float anchorX;
	public float anchorY;
	public float additionalRotDeg;

	public PartVisuals()
	{
		throw new InvalidOperationException("Did you forget to use the proper constructor?");
	}

	public PartVisuals(string atlasElement, string shader, ContainerCodes container, Color color, float alpha, float scaleX, float scaleY, float anchorX, float anchorY, float additionalRotDeg)
	{
		this.atlasElement = atlasElement;
		this.shader = shader;
		this.container = container;
		this.color = color;
		this.alpha = alpha;
		this.scaleX = scaleX;
		this.scaleY = scaleY;
		this.anchorX = anchorX;
		this.anchorY = anchorY;
		this.additionalRotDeg = additionalRotDeg;
	}
}

