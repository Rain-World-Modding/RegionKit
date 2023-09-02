namespace RegionKit.Modules.Slideshow;

internal sealed record SlideShowInstant(
	string elementName,
	string shader,
	ContainerCodes container,
	Vector2 position,
	Color color,
	Vector2 scale,
	float rotationDegrees
	);
