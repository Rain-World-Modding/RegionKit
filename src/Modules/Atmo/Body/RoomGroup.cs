using System.Text.RegularExpressions;

namespace RegionKit.Modules.Atmo.Body
{
	public class RoomGroup
	{
		public RoomGroup(string name)
		{
			this.name = name;
		}

		public void EvaluateMatchers(World world)
		{
			foreach (Regex? matcher in _matchers)
				foreach (AbstractRoom room in world.abstractRooms)
					if (matcher.IsMatch(room.name) && !includeRooms.Contains(room.name)) includeRooms.Add(room.name);
			_matchers = new();
		}

		public void EvaluateGroups(Dictionary<string, RoomGroup> groups)
		{
			for (int i = _groupNames.Count - 1; i >= 0; i--)
			{
				string name = _groupNames[i];
				if (groups.TryGetValue(name, out RoomGroup other) && !other.groups.Contains(this))
				{
					this.groups.Add(other);
					_groupNames.RemoveAt(i);
				}
			}
		}

		public string name;

		internal List<string> _groupNames = new();
		internal List<Regex> _matchers = new();


		internal List<string> includeRooms = new();
		internal List<string> excludeRooms = new();
		internal List<RoomGroup> groups = new();

		public IEnumerable<string> Rooms
		{
			get
			{
				List<string> result = includeRooms;
				foreach (RoomGroup group in groups)
				{
					foreach (string room in group.Rooms)
					{
						if (!result.Contains(room)) result.Add(room);
					}
				}
				foreach (string room in excludeRooms)
				{
					result.Remove(room);
				}
				return result;
			}
		}
	}
}
