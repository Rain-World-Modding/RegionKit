namespace RegionKit.Modules.RoomSlideShow;

/// <summary>
/// Describes different available keyframe channels
/// </summary>
public enum Channel
{
	/// <summary>
	/// Color, Red
	/// </summary>
	R,
	/// <summary>
	/// Color, Green
	/// </summary>
	G,
	/// <summary>
	/// Color, Blue
	/// </summary>
	B,
	/// <summary>
	/// Color, Alpha
	/// </summary>
	A,
	/// <summary>
	/// Horizontal shift, pixels
	/// </summary>
	X,
	/// <summary>
	/// Vertical shift, pixels
	/// </summary>
	Y,
	
	/// <summary>
	/// Rotation, degrees
	/// </summary>
	T,
	/// <summary>
	/// Width, multiplier
	/// </summary>
	W,
	/// <summary>
	/// Height, multiplier
	/// </summary>
	H,
}
