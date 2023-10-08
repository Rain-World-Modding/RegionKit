using System.Runtime.CompilerServices;

namespace RegionKit.Modules.GateCustomization;

internal static class RegionGateCWT
{
	public class RegionGateData
	{
		public ManagedData? commonGateData;
		public ManagedData? waterGateData;
		public ManagedData? electricGateData;

		public RegionGateData()
		{
			commonGateData = null;
			waterGateData = null;
			electricGateData = null;
		}
	}

	private static readonly ConditionalWeakTable<RegionGate, RegionGateData> _regionGateCWT = new ConditionalWeakTable<RegionGate, RegionGateData>();

	public static RegionGateData GetData(this RegionGate regionGate)
	{
		if (!_regionGateCWT.TryGetValue(regionGate, out var regionGateData))
		{
			regionGateData = new RegionGateData();
			_regionGateCWT.Add(regionGate, regionGateData);
		}

		return regionGateData;
	}

	public enum SnapMode
	{
		NoSnap,
		MiddleSnap,
		CornerSnap
	}

	public static Vector2 GetPosition(this ManagedData data, Room room, SnapMode snapMode = SnapMode.MiddleSnap)
	{
		switch (snapMode)
		{
		case SnapMode.MiddleSnap:
			return room.MiddleOfTile(data.owner.pos);

		case SnapMode.CornerSnap:
			return room.MiddleOfTile(data.owner.pos - new Vector2(10f, 10f)) + new Vector2(10f, 10f);

		case SnapMode.NoSnap:
			return data.owner.pos;

		default:
			return data.owner.pos;
		}
	}

	public static IntVector2 GetTilePosition(this ManagedData data, Room room)
	{
		return room.GetTilePosition(data.owner.pos);
	}
}
