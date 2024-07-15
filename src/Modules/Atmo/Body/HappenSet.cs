using RegionKit.Modules.Atmo.Gen;

namespace RegionKit.Modules.Atmo.Body;
/// <summary>
/// Represents a set of Happens for a single region. Binds together room names, groups and happens.
/// Binding is done using <see cref="TwoPools{TLeft, TRight}"/> sets, living in properties:
/// <see cref="RoomsToGroups"/>, <see cref="GroupsToHappens"/>, <see cref="IncludeToHappens"/>, <see cref="ExcludeToHappens"/>.
/// <para>
/// To get happens that should be active in a room with a given name, see <seealso cref="GetHappensForRoom(string)"/>.
/// To get rooms a happen should be active in, see <seealso cref="GetRoomsForHappen(Happen)"/>.
/// </para>
/// </summary>
public sealed class HappenSet {
	#region fields
	/// <summary>
	/// The world this instance is created for.
	/// </summary>
	public readonly World world;

	public List<RoomGroup> AllRoomGroups { get; private set; } = new();

	public Dictionary<Happen, RoomGroup> RoomGroups { get; private set; } = new();

	public Dictionary<string, List<Happen>> RoomsToHappens { get; private set; } = new();

	public List<Happen> AllHappens { get; private set; } = new();
	#endregion
	/// <summary>
	/// Creates a new instance. Reads from a file if provided; otherwise stays blank.
	/// </summary>
	/// <param name="world">World to be bound to.</param>
	/// <param name="file">File to read contents from. New instance stays blank if this is null.</param>
	public HappenSet(World world, System.IO.FileInfo? file = null) {
		BangBang(world, nameof(world));
		this.world = world;
		//if (world is null || file is null) return;
		if (file is not null && file.Exists) {
			HappenParser.Parse(file, this, world.game);
		}
		else {
			LogWarning($"No atmo file found for {file?.FullName ?? "NULL"}, leaving blank");
		}

		//subregions as groups
		Dictionary<string, RoomGroup> subContents = new();

		foreach (string sub in world.region.subRegions) 
		{
			subContents[sub] = new(sub);
			subContents[sub].includeRooms.AddRange(world.abstractRooms
					.Where(x => x.subregionName == sub)
					.Select(x => x.name));
			//int index = world.region.subRegions.IndexOf(sub);
			//plog.VerboseLog($"\"{sub}\" :: {index}");
		}
		foreach (RoomGroup group in AllRoomGroups)
		{ group.EvaluateGroups(subContents); }

		InsertGroups(subContents.Values.ToList());

		RefreshRoomsToHappens();
	}

	public void RefreshRoomsToHappens()
	{
		RoomsToHappens = new();
		foreach ((Happen happen, RoomGroup group) in RoomGroups)
		{
			foreach (string room in group.Rooms)
			{
				if (RoomsToHappens.TryGetValue(room, out var happens))
				{
					happens.Add(happen);
				}
				else
				{
					RoomsToHappens[room] = new() { happen };
				}
			}
		}
	}

	/// <summary>
	/// Yields all rooms a given happen should be active in.
	/// </summary>
	/// <param name="ha">Happen to be checked. Must not be null.</param>
	/// <returns>A set of room names for rooms the given happen should work in.</returns>
	public IEnumerable<string> GetRoomsForHappen(Happen ha) {
		BangBang(ha, nameof(ha));
		return RoomGroups.TryGetValue(ha, out var group)? group.Rooms : new List<string>();
	}
	/// <summary>
	/// Yields all happens a given room should have active.
	/// </summary>
	/// <param name="roomname">Room name to check. Must not be null.</param>
	/// <returns>A set of happens active for given room.</returns>
	public IEnumerable<Happen> GetHappensForRoom(string roomname) {
		BangBang(roomname, nameof(roomname));
		return RoomsToHappens.TryGetValue(roomname, out var happens) ? happens : new List<Happen>();
	}

	/// <summary>
	/// Fetches dictionary containing all groups with their contents
	/// </summary>
	/// <returns></returns>
	public IDictionary<string, IEnumerable<string>> GetGroups() => AllRoomGroups.ToDictionary(x => x.name, x => x.Rooms);
	#region insertion
	/// <summary>
	/// Adds a group with its contents.
	/// </summary>
	/// <param name="groups"></param>
	public void InsertGroups(List<RoomGroup> groups)
	{
		AllRoomGroups.AddRange(groups);
	}
	/// <summary>
	/// Inserts a single happen
	/// </summary>
	/// <param name="happen"></param>
	/// <param name="group"></param>
	public void InsertHappen(Happen happen, RoomGroup group) 
	{
		AllHappens.Add(happen);
		RoomGroups[happen] = group;
	}
	#endregion
	/// <summary>
	/// Yields performance records for all happens. Consume or discard the enumerable on the same frame.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<Happen.Perf> GetPerfRecords() {
		foreach (Happen? ha in AllHappens) {
			yield return ha.PerfRecord();
		}
	}
	/// <summary>
	/// Attempts to create a new happenSet for a given world. This checks all loaded CRS packs and merges together intersecting <c>.atmo</c> files.
	/// </summary>
	/// <param name="world">The world to create a happenSet for. Must not be null.</param>
	/// <returns>A resulting HappenSet; null if there was no regpack with an .atmo file for given region, or if there was an error on creation.</returns>
	public static HappenSet TryCreate(World world)
	{
		BangBang(world, nameof(world));
		HappenSet? res = null;

		System.IO.FileInfo fi = new(AssetManager.ResolveFilePath($"world/{world.name}/{world.name}.atmo"));
		res = new(world, fi);
		return res;
	}
}
