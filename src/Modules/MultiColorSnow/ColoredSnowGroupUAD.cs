namespace RegionKit.Modules.MultiColorSnow;

using static Pom.Pom;
using UnityEngine;

public class ColoredSnowGroupUAD : UpdatableAndDeletable
{

	private PlacedObject placedObject;

	public ColoredSnowGroupData data = new ColoredSnowGroupData();
	public ColoredSnowGroupData lastData = new ColoredSnowGroupData();

	private bool valid = true;

	public ColoredSnowGroupUAD(Room room, PlacedObject placedObject)
	{
		this.placedObject = placedObject;

		data.update(placedObject);
		lastData.update(placedObject);

		ColoredSnowWeakRoomData roomData = ColoredSnowWeakRoomData.GetData(room);

		if (!roomData.snowPalettes.ContainsKey(data.palette))
		{
			roomData.snowPalettes[data.palette] = this;
		}
		else
		{
			valid = false;
		}
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		this.data.update(placedObject);

		ColoredSnowWeakRoomData roomData = ColoredSnowWeakRoomData.GetData(room);

		if (data != lastData)
		{
			if (data.palette != lastData.palette)
			{
				bool validityJustChanged = false;
				if (!valid)
				{
					if (!roomData.snowPalettes.ContainsKey(data.palette))
					{
						valid = true;
						validityJustChanged = true;
					}
				}
				if (valid)
				{
					if (!validityJustChanged)
					{
						roomData.snowPalettes.Remove(lastData.palette);
					}
					roomData.snowPalettes[data.palette] = this;
				}
			}
		}

		if (data != lastData && roomData.snow && this.room.BeingViewed)
		{
			ColoredSnowRoomCamera.GetData(this.room.game.cameras[0]).snowChange = true;
		}

		this.lastData.copy(this.data);
	}

	public Color getRGBA()
	{
		return new Color((float)data.r / 255.0f, (float)data.g / 255.0f, (float)data.b / 255.0f, (float)data.s / 255.0f);
	}

	public Color getERGBA()
	{
		return new Color((float)data.er / 255.0f, (float)data.eg / 255.0f, (float)data.eb / 255.0f, (float)data.es / 255.0f);
	}

	public Color getBlendedRGBA(float blend)
	{
		return Color.Lerp(getRGBA(), getERGBA(), blend);
	}
}

public class ColoredSnowGroupData
{

	public int palette;
	public int r;
	public int g;
	public int b;
	public int s;
	public int er;
	public int eg;
	public int eb;
	public int es;
	public int from;
	public int to;

	public ColoredSnowGroupData()
	{
		this.palette = 0;
		this.r = 0;
		this.g = 0;
		this.b = 0;
		this.s = 0;
		this.er = 0;
		this.eg = 0;
		this.eb = 0;
		this.es = 0;
		this.from = 0;
		this.to = 0;
	}

	public void update(PlacedObject placedObject)
	{
		ManagedData data = ((ManagedData)placedObject.data);

		this.palette = data.GetValue<int>("palette");
		this.r = data.GetValue<int>("r");
		this.g = data.GetValue<int>("g");
		this.b = data.GetValue<int>("b");
		this.s = data.GetValue<int>("s");
		this.er = data.GetValue<int>("er");
		this.eg = data.GetValue<int>("eg");
		this.eb = data.GetValue<int>("eb");
		this.es = data.GetValue<int>("es");
		this.from = data.GetValue<int>("front");
		this.to = data.GetValue<int>("back");
	}

	public void copy(ColoredSnowGroupData data)
	{
		this.palette = data.palette;
		this.r = data.r;
		this.g = data.g;
		this.b = data.b;
		this.s = data.s;
		this.er = data.er;
		this.eg = data.eg;
		this.eb = data.eb;
		this.es = data.es;
		this.from = data.from;
		this.to = data.to;
	}

	public static bool operator ==(ColoredSnowGroupData a, ColoredSnowGroupData b)
	{
		return a.palette == b.palette &&
		a.r == b.r &&
		a.g == b.g &&
		a.b == b.b &&
		a.s == b.s &&
		a.er == b.er &&
		a.eg == b.eg &&
		a.eb == b.eb &&
		a.es == b.es &&
		a.from == b.from &&
		a.to == b.to;
	}
	public static bool operator !=(ColoredSnowGroupData a, ColoredSnowGroupData b)
	{
		return a.palette != b.palette ||
		a.r != b.r ||
		a.g != b.g ||
		a.b != b.b ||
		a.s != b.s ||
		a.er != b.er ||
		a.eg != b.eg ||
		a.eb != b.eb ||
		a.es != b.es ||
		a.from != b.from ||
		a.to != b.to;
	}

}
