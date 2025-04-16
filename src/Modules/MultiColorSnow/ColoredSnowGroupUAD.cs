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
		Color result = data.color;
		result.a = data.s / 255.0f;
		return result;
	}

	public Color getERGBA()
	{
		Color result = data.rainColor;
		result.a = data.s / 255.0f;
		return result;
	}

	public Color getBlendedRGBA(float blend)
	{
		return Color.Lerp(getRGBA(), getERGBA(), blend);
	}
}

#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
public class ColoredSnowGroupData
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
{
	public Color color;
	public Color rainColor;
	public int palette;
	public int s;
	public int es;
	public int from;
	public int to;

	public ColoredSnowGroupData()
	{
		this.palette = 0;
		this.s = 0;
		this.es = 0;
		this.from = 0;
		this.to = 0;
	}

	public void update(PlacedObject placedObject)
	{
		ManagedData data = ((ManagedData)placedObject.data);

		this.palette = data.GetValue<int>("palette");
		this.color = data.GetValue<Color>("color");
		this.s = data.GetValue<int>("s");
		this.rainColor = data.GetValue<Color>("rainColor");
		this.es = data.GetValue<int>("es");
		this.from = data.GetValue<int>("front");
		this.to = data.GetValue<int>("back");
	}

	public void copy(ColoredSnowGroupData data)
	{
		this.palette = data.palette;
		this.color = data.color;
		this.s = data.s;
		this.rainColor = data.rainColor;
		this.es = data.es;
		this.from = data.from;
		this.to = data.to;
	}

	public static bool operator ==(ColoredSnowGroupData a, ColoredSnowGroupData b)
	{
		return a.palette == b.palette &&
		a.color == b.color &&
		a.s == b.s &&
		a.rainColor == b.rainColor &&
		a.es == b.es &&
		a.from == b.from &&
		a.to == b.to;
	}
	public static bool operator !=(ColoredSnowGroupData a, ColoredSnowGroupData b)
	{
		return a.palette != b.palette ||
		a.color != b.color ||
		a.s != b.s ||
		a.color != b.color ||
		a.es != b.es ||
		a.from != b.from ||
		a.to != b.to;
	}

}
