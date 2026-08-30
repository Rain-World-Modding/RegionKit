namespace RegionKit.Modules.MoonStuff
{
	public class BarbedWireData : PlacedObject.ConsumableObjectData
	{
		public Vector2 endPos;		

		public float Poisionious;

		public BarbedWireData(PlacedObject owner) : base(owner)
		{
			endPos = new Vector2(60, 0);

			if (owner.type == _Enums.BarbedWire)
			{
				minRegen = 1;
				maxRegen = 1;
			}
			else if (owner.type == _Enums.PoisonBerryVine)
			{
				minRegen = 4;
				maxRegen = 9;
			}
		}

		public override string ToString()
		{
			return
				panelPos.x.ToString() + "~" +
				panelPos.y.ToString() + "~" +
				minRegen.ToString() + "~" +
				maxRegen.ToString() + "~" +
				endPos.x.ToString() + "~" +
				endPos.y.ToString() + "~" +
				unrecognizedAttributes;
		}

		public override void FromString(string s)
		{
			string[] data = s.Split('~');
			panelPos.x = float.Parse(data[0]);
			panelPos.y = float.Parse(data[1]);
			minRegen = int.Parse(data[2]);
			maxRegen = int.Parse(data[3]);
			endPos.x = float.Parse(data[4]);
			endPos.y = float.Parse(data[5]);
			unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(data, 6);
		}
	}
}
