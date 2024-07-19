namespace RegionKit.Modules.Atmo.Data;

/// <summary>
/// Displays what kind of data an <see cref="IArgPayload"/> instance was constructed from.
/// </summary>
public enum ArgType
{
	/// <summary>
	/// The data type is unspecified.
	/// </summary>
	OTHER,
	/// <summary>
	/// Value was originally assigned as float.
	/// </summary>
	DECIMAL,
	/// <summary>
	/// Value was originally assigned as int.
	/// </summary>
	INTEGER,
	/// <summary>
	/// Value was originally assigned as string.
	/// </summary>
	STRING,
	/// <summary>
	/// Value was originally assigned as an enum.
	/// </summary>
	ENUM,
	/// <summary>
	/// Value was originally assigned as boolean.
	/// </summary>
	BOOLEAN,
	/// <summary>
	/// Value was orignally assigned as a vector.
	/// </summary>
	VECTOR,
}
