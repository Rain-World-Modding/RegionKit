using HUD;

namespace RegionKit.Modules.ExtendedGates
{
	public interface LockData
	{
		public string GateElementName(GateKarmaGlyph glyph);
		public string MapElementName(Map.GateMarker gateMarker);
		public bool Requirement(RegionGate regionGate);
	}
}
