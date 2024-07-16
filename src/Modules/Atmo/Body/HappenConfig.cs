using RegionKit.Modules.Atmo.Gen;

namespace RegionKit.Modules.Atmo.Body;

/// <summary>
/// Represents data accumulated by a <see cref="HappenParser"/> for a singular happen.
/// </summary>
public struct HappenConfig
{
	/// <summary>
	/// Happen's name.
	/// </summary>
	public string name;
	/// <summary>
	/// Group the happen is associated with.
	/// </summary>
	public RoomGroup myGroup;
	/// <summary>
	/// List of action names with parameters.
	/// </summary>
	public Dictionary<string, string[]> actions;
	/// <summary>
	/// An eval tree for activation conditions.
	/// </summary>
	public PredicateInlay? conditions;
	/// <summary>
	/// Creates a blank config with given name.
	/// </summary>
	/// <param name="name"></param>
	public HappenConfig(string name)
	{
		this.name = name;
		actions = new();
		myGroup = new(name);
		conditions = null;
	}
}
