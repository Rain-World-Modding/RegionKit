namespace RegionKit.Modules.Slideshow;

internal struct SlideShowInstant
{
	public string elementName;
	public string shader;
	public Vector2 position;
	public Color color;

	public SlideShowInstant(string elementName, string shader, Vector2 position, Color color)
	{
		this.elementName = elementName;
		this.shader = shader;
		this.position = position;
		this.color = color;
	}
}
