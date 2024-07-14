using static RegionKit.Modules.Atmo.Atmod;

using RegionKit.Modules.Atmo.Body;
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
	/// Game process instance this set is bound to.
	/// </summary>
	public readonly RainWorldGame game;
	/// <summary>
	/// The world this instance is created for.
	/// </summary>
	public readonly World world;
	/// <summary>
	/// Left pool contains room names, right pool contains group names.
	/// </summary>
	public TwoPools<string, string> RoomsToGroups { get; private set; } = new();
	/// <summary>
	/// Left pool contains group names, right pool contains happens.
	/// </summary>
	public TwoPools<string, Happen> GroupsToHappens { get; private set; } = new();
	/// <summary>
	/// Left pool conrains individual includes, right pool contains happens.
	/// </summary>
	public TwoPools<string, Happen> IncludeToHappens { get; private set; } = new();
	/// <summary>
	/// Left pool conrains individual excludes, right pool contains happens.
	/// </summary>
	public TwoPools<string, Happen> ExcludeToHappens { get; private set; } = new();
	/// <summary>
	/// Contains all owned happens
	/// </summary>
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
		game = world.game;
		//if (world is null || file is null) return;
		if (file is not null && file.Exists) {
			HappenParser.Parse(file, this, game);
		}
		else {
			LogWarning($"No atmo file found for {file?.FullName ?? "NULL"}, leaving blank");
		}

		//subregions as groups
		Dictionary<string, List<string>> subContents = new();

		foreach (string sub in world.region.subRegions) {
			//int index = world.region.subRegions.IndexOf(sub);
			//plog.VerboseLog($"\"{sub}\" :: {index}");
			subContents
				.EnsureAndGet(sub, () => new())
				.AddRange(
					world.abstractRooms
					.Where(x => x.subregionName == sub)
					.Select(x => x.name));
		}
		foreach ((string? sub, List<string>? rooms) in subContents) {
			InsertGroup(sub, rooms);
		}
		foreach (var g in RoomsToGroups.EnumerateRight()) {
			VerboseLog($"group {g} contains: {RoomsToGroups.IndexFromRight(g).Stitch()}");
		}
		foreach (var ha in AllHappens) {
			VerboseLog($"happen {ha} works in rooms {GetRoomsForHappen(ha).Stitch()}");
		}
	}
	/// <summary>
	/// Yields all rooms a given happen should be active in.
	/// </summary>
	/// <param name="ha">Happen to be checked. Must not be null.</param>
	/// <returns>A set of room names for rooms the given happen should work in.</returns>
	public IEnumerable<string> GetRoomsForHappen(Happen ha) {
		BangBang(ha, nameof(ha));
		List<string> returned = new();
		IEnumerable<string>? excludes = ExcludeToHappens.IndexFromRight(ha);
		IEnumerable<string>? includes = IncludeToHappens.IndexFromRight(ha);
		foreach (string? group in GroupsToHappens.IndexFromRight(ha)) {
			foreach (string? room in RoomsToGroups.IndexFromRight(group)) {
				if (excludes.Contains(room)) break;
				//if (SpecificExcludeToHappens.IndexFromRight(ha).Contains(room)) continue;
				returned.Add(room);
				yield return room;
			}
		}
		foreach (string? room in includes) {
			if (!returned.Contains(room)) yield return room;
		}
	}
	/// <summary>
	/// Yields all happens a given room should have active.
	/// </summary>
	/// <param name="roomname">Room name to check. Must not be null.</param>
	/// <returns>A set of happens active for given room.</returns>
	public IEnumerable<Happen> GetHappensForRoom(string roomname) {
		BangBang(roomname, nameof(roomname));
		List<Happen> returned = new();
		//goto _specific;
		if (!RoomsToGroups.LeftContains(roomname)) goto _specific;
		foreach (string? group in RoomsToGroups.IndexFromLeft(roomname)) {
			if (!GroupsToHappens.LeftContains(group)) continue;
			foreach (Happen? ha in GroupsToHappens.IndexFromLeft(group)) {
				//exclude the minused
				if (ExcludeToHappens.IndexFromRight(ha)
					.Contains(roomname)) continue;
				returned.Add(ha);
				yield return ha;
			}
		}
	_specific:
		if (!IncludeToHappens.LeftContains(roomname)) yield break;
		foreach (Happen? ha in IncludeToHappens.IndexFromLeft(roomname)) {
			if (!returned.Contains(ha)) yield return ha;
		}
	}

	/// <summary>
	/// Fetches dictionary containing all groups with their contents
	/// </summary>
	/// <returns></returns>
	public IDictionary<string, IEnumerable<string>> GetGroups() {
		Dictionary<string, IEnumerable<string>> res = new();
		foreach (string g in RoomsToGroups.EnumerateRight()) {
			res.Add(g, RoomsToGroups.IndexFromRight(g));
		}
		return res;
	}
	/// <summary>
	/// Returns a dictionary with happen-group binds.
	/// </summary>
	/// <returns></returns>
	public IDictionary<Happen, IEnumerable<string>> GetBinds() {
		Dictionary<Happen, IEnumerable<string>> res = new();
		foreach (Happen ha in GroupsToHappens.EnumerateRight()) {
			res.Add(ha, GroupsToHappens.IndexFromRight(ha));
		}
		return res;
	}
	#region insertion
	/// <summary>
	/// Binds a given happen to a set of groups. Assumes that happen has already been added via <see cref="InsertHappens(IEnumerable{Happen})"/>.
	/// </summary>
	/// <param name="happen">The happen that should receive group links. Must not be null.</param>
	/// <param name="bind">A set of groups to be bound to given happen. Must not be null.</param>
	public void AddBind(Happen happen, IEnumerable<string> bind) {
		BangBang(happen, nameof(happen));
		BangBang(bind, nameof(bind));
		if (bind?.Count() is null or 0) return;
		Dictionary<string, IEnumerable<string>> ins = new();
		foreach (string? g in bind) {
			//why the fuck did i make it a blank list?..
			ins.Set(g, new List<string>(0));
		}
		InsertGroups(ins);
		GroupsToHappens.AddLinksBulk(bind.Select(gr => new KeyValuePair<string, Happen>(gr, happen)));
	}
	/// <summary>
	/// Registers multiple happen-group binds.
	/// </summary>
	/// <param name="binds"></param>
	public void AddBinds(IDictionary<Happen, IEnumerable<string>> binds) {
		BangBang(binds, nameof(binds));
		foreach ((Happen ha, IEnumerable<string> bind) in binds) {
			AddBind(ha, bind);
		}
	}
	/// <summary>
	/// Adds room excludes for a given happen. In these rooms, the happen will be inactive regardless of grouping. Assumes happen has already been added via <see cref="InsertHappens(IEnumerable{Happen})"/>
	/// </summary>
	/// <param name="happen">A happen receiving excludes. Must not be null.</param>
	/// <param name="excl">A set of room names to exclude. Must not be null.</param>
	public void AddExcludes(Happen happen, IEnumerable<string> excl) {
		BangBang(happen, nameof(happen));
		BangBang(excl, nameof(excl));
		if (excl?.Count() is null or 0) return;
		ExcludeToHappens.InsertRangeLeft(excl);
		foreach (string? ex in excl) ExcludeToHappens.AddLink(ex, happen);
	}
	/// <summary>
	/// Adds room includes for a given happen. In these rooms, the happen will be active regardless of grouping. Assumes happen has already been added via <see cref="InsertHappens(IEnumerable{Happen})"/>
	/// </summary>
	/// <param name="happen">A happen receiving includes. Must not be null.</param>
	/// <param name="incl">A set of room names to include. Must not be null.</param>
	public void AddIncludes(Happen happen, IEnumerable<string> incl) {
		BangBang(happen, nameof(happen));
		BangBang(incl, nameof(incl));
		if (incl?.Count() is null or 0) return;
		IncludeToHappens.InsertRangeLeft(incl);
		foreach (string? @in in incl) IncludeToHappens.AddLink(@in, happen);
	}
	/// <summary>
	/// Inserts a single group.
	/// </summary>
	/// <param name="group"></param>
	/// <param name="rooms"></param>
	public void InsertGroup(string group, IEnumerable<string> rooms) {
		RoomsToGroups.InsertRight(group);
		GroupsToHappens.InsertLeft(group);
		RoomsToGroups.InsertRangeLeft(rooms);
		RoomsToGroups.AddLinksBulk(rooms.Select(x => new KeyValuePair<string, string>(x, group)));
		//foreach (var room in rooms) { RoomsToGroups.AddLink() }
	}
	/// <summary>
	/// Adds a group with its contents.
	/// </summary>
	/// <param name="groups"></param>
	public void InsertGroups(IDictionary<string, IEnumerable<string>> groups) {
		RoomsToGroups.InsertRangeRight(groups.Keys);
		GroupsToHappens.InsertRangeLeft(groups.Keys);
		foreach ((string name, IEnumerable<string> group) in groups) {
			RoomsToGroups.InsertRangeLeft(group);
			foreach (string? room in group) { RoomsToGroups.AddLink(room, name); }
		}
	}
	/// <summary>
	/// Inserts a set of happens without binding them to any rooms.
	/// </summary>
	/// <param name="haps"></param>
	public void InsertHappens(IEnumerable<Happen> haps) {
		AllHappens.AddRange(haps);
		GroupsToHappens.InsertRangeRight(haps);
		ExcludeToHappens.InsertRangeRight(haps);
		IncludeToHappens.InsertRangeRight(haps);
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
	#region statics
	/// <summary>
	/// Attempts to create a new happenSet for a given world. This checks all loaded CRS packs and merges together intersecting <c>.atmo</c> files.
	/// </summary>
	/// <param name="world">The world to create a happenSet for. Must not be null.</param>
	/// <returns>A resulting HappenSet; null if there was no regpack with an .atmo file for given region, or if there was an error on creation.</returns>
	public static HappenSet TryCreate(World world) {
		BangBang(world, nameof(world));
		HappenSet? res = null;
#if REMIX
		System.IO.FileInfo fi = new(AssetManager.ResolveFilePath($"world/{world.name}/{world.name}.atmo"));
		res = new(world, fi);
		return res;
#else
		try
		{
			Dictionary<string, CRS.CustomWorldStructs.RegionPack>? packs = CRS.API.InstalledPacks;
			Dictionary<string, string>? active = CRS.API.ActivatedPacks;
			foreach (KeyValuePair<string, CRS.CustomWorldStructs.RegionPack> kvp in packs)
			{
				string? name = kvp.Key;
				CRS.CustomWorldStructs.RegionPack data = kvp.Value;
				//skip inactive
				if (!active.ContainsKey(name)) continue;
				string? tarpath = CRS.API.BuildPath(
					regionPackFolder: data.folderName,
					folderName: "World",
					regionID: world.name,
					file: $"{world.name}.atmo",
					folder: IO.Path.Combine("Regions", world.name),
					includeRoot: true);
				var tarfile = new IO.FileInfo(tarpath);
				plog.LogDebug($"Checking regpack {name} (path {tarpath})");
				if (tarfile.Exists)
				{
					plog.LogDebug("Found a .atmo file, reading a happenset...");
					HappenSet gathered = new(world, tarfile);
					plog.LogDebug($"Read happenset {gathered}");
					if (res is null) res = gathered;
					else res += gathered;
				}
				else
				{
					plog.LogDebug("No XX.atmo file found.");
				}
			}
		}
		catch (Exception ex)
		{
			plog.LogError($"Could not load event setup for {world.name}:\n{ex}");
		}
		return res ?? new(world);
#endif
	}
	/// <summary>
	/// Joins two instances together. Used when merging from several files.
	/// </summary>
	/// <param name="l"></param>
	/// <param name="r"></param>
	/// <returns></returns>
	public static HappenSet operator +(HappenSet l, HappenSet r) {
		HappenSet res = new(l.world ?? r.world) {
			IncludeToHappens = TwoPools<string, Happen>.Stitch(
				l.IncludeToHappens,
				r.IncludeToHappens),
			RoomsToGroups = TwoPools<string, string>.Stitch(
				l.RoomsToGroups,
				r.RoomsToGroups),
			GroupsToHappens = TwoPools<string, Happen>.Stitch(
				l.GroupsToHappens,
				r.GroupsToHappens),
			ExcludeToHappens = TwoPools<string, Happen>.Stitch(
				l.ExcludeToHappens,
				r.ExcludeToHappens),
		};
		res.AllHappens.AddRange(l.AllHappens);
		res.AllHappens.AddRange(r.AllHappens);
		foreach (Happen? ha in res.AllHappens) {
			LogDebug($"{ha.name}: switching ownership");
			ha.Set = res;
			VerboseLog(res.GetRoomsForHappen(ha).Stitch());
		}
		VerboseLog(res.GetHappensForRoom("SU_S04").Select(x => x.name).Stitch());
		return res;
		//HappenSet res = new(l.world ?? r.world)
		//{

		//};
		//res.InsertHappens(l.AllHappens.Concat(r.AllHappens));
		//res.InsertGroups(l.GetGroups());
		//res.InsertGroups(r.GetGroups());
		//res.AddBinds(l.GetBinds());
		//res.AddBinds(r.GetBinds());
		//return res;
	}
	#endregion statics
}
