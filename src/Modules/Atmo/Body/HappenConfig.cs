using RegionKit.Modules.Atmo.Gen;

namespace RegionKit.Modules.Atmo.Body;

/// <summary>
/// Represents data accumulated by a <see cref="HappenParser"/> for a singular happen.
/// </summary>
public struct HappenConfig {
	/// <summary>
	/// Happen's name.
	/// </summary>
	public string name;
	/// <summary>
	/// List of room groups the happen is associated with.
	/// </summary>
	public List<string> groups;
	/// <summary>
	/// List of included rooms.
	/// </summary>
	public List<string> include;
	/// <summary>
	/// List of excluded rooms.
	/// </summary>
	public List<string> exclude;
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
	public HappenConfig(string name) {
		this.name = name;
		groups = new();
		actions = new();
		include = new();
		exclude = new();
		conditions = null;
	}
}
