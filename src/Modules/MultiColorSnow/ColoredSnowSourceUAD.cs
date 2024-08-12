namespace RegionKit.Modules.MultiColorSnow;

public class ColoredSnowSourceUAD : UpdatableAndDeletable
{
	PlacedObject placedObject;

	public ColoredSnowSourceData data = new ColoredSnowSourceData();
	public ColoredSnowSourceData lastData = new ColoredSnowSourceData();

	public int visibility = 2;
	private int lastCam;

	public ColoredSnowSourceUAD(Room room, PlacedObject placedObject)
	{
		this.placedObject = placedObject;

		ColoredSnowWeakRoomData roomData = ColoredSnowWeakRoomData.GetData(room);

		roomData.snowSources.Add(this);

		if (roomData.snowObject == null)
		{
			roomData.snowObject = new ColoredSnowDrawable(room);
			room.drawableObjects.Add(roomData.snowObject);
		}

		roomData.snow = true;
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		this.data.update(this.placedObject);

		int cam = this.room.game.cameras[0].currentCameraPosition;

		if ((cam != this.lastCam || data != lastData || (this.room.BeingViewed && this.visibility == 2)) && ColoredSnowWeakRoomData.GetData(this.room).snow && this.room.BeingViewed)
		{
			this.visibility = this.CheckVisibility(cam);
			ColoredSnowRoomCamera.GetData(this.room.game.cameras[0]).snowChange = true;
		}

		this.lastCam = cam;
		this.lastData.copy(this.data);
	}
	public int CheckVisibility(int camIndex)
	{
		Vector2 cam = this.room.cameraPositions[camIndex];

		if (data.pos.x > cam.x - data.radius && data.pos.x < cam.x + data.radius + 1400f && data.pos.y > cam.y - data.radius && data.pos.y < cam.y + data.radius + 800f)
		{
			return 1;
		}

		return 0;
	}

	public Vector4[] PackSnowData()
	{
		Vector2 vector = room.cameraPositions[room.game.cameras[0].currentCameraPosition];

		float width = 1400f;
		float height = 800f;

		Vector4[] array = new Vector4[3];
		Vector2 vector2 = Custom.EncodeFloatRG((data.pos.x - vector.x) / width * 0.3f + 0.3f);
		Vector2 vector3 = Custom.EncodeFloatRG((data.pos.y - vector.y) / height * 0.3f + 0.3f);
		Vector2 vector4 = Custom.EncodeFloatRG(data.radius / 1600f);
		array[0] = new Vector4(vector2.x, vector2.y, vector3.x, vector3.y);
		array[1] = new Vector4(vector4.x, vector4.y, data.intensity, data.noisiness);
		array[2] = new Vector4(0f, 0f, 0f, ((float)(data.shape) + (data.unsnow ? 4 : 0)) / 8f);
		return array;
	}
}

public class ColoredSnowSourceData
{

	public Vector2 pos;
	public float radius;
	public float intensity;
	public float noisiness;
	public int palette;
	public ColoredSnowShape shape;
	public Boolean unsnow;

	public ColoredSnowSourceData()
	{
		this.pos = new Vector2();
		this.radius = 100;
		this.intensity = 1;
		this.noisiness = 0;
		this.shape = ColoredSnowShape.Radial;
		this.unsnow = false;
	}

	public void update(PlacedObject placedObject)
	{
		ManagedData data = ((ManagedData)placedObject.data);

		this.pos = placedObject.pos;
		this.radius = data.GetValue<Vector2>("range").magnitude;
		this.intensity = (float)data.GetValue<int>("intensity") / 100.0F;
		this.noisiness = (float)data.GetValue<int>("irregularity") / 100.0F;
		this.palette = data.GetValue<int>("palette");
		this.shape = data.GetValue<ColoredSnowShape>("shape");
		this.unsnow = data.GetValue<bool>("unsnow");
	}

	public void copy(ColoredSnowSourceData data)
	{
		this.pos = new Vector2(data.pos.x, data.pos.y);
		this.radius = data.radius;
		this.intensity = data.intensity;
		this.noisiness = data.noisiness;
		this.shape = data.shape;
		this.palette = data.palette;
		this.unsnow = data.unsnow;
	}

	public static bool operator ==(ColoredSnowSourceData a, ColoredSnowSourceData b)
	{
		return a.pos == b.pos &&
		a.radius == b.radius &&
		a.intensity == b.intensity &&
		a.noisiness == b.noisiness &&
		a.palette == b.palette &&
		a.shape == b.shape &&
		a.unsnow == b.unsnow;
	}
	public static bool operator !=(ColoredSnowSourceData a, ColoredSnowSourceData b)
	{
		return a.pos != b.pos ||
		a.radius != b.radius ||
		a.intensity != b.intensity ||
		a.noisiness != b.noisiness ||
		a.palette != b.palette ||
		a.shape != b.shape ||
		a.unsnow != b.unsnow;
	}
}
public class ColoredSnowShape : ExtEnum<ColoredSnowShape>
{

	public static readonly ColoredSnowShape None = new ColoredSnowShape("None", true);
	public static readonly ColoredSnowShape Radial = new ColoredSnowShape("Radial", true);
	public static readonly ColoredSnowShape Strip = new ColoredSnowShape("Strip", true);
	public static readonly ColoredSnowShape Column = new ColoredSnowShape("Column", true);

	public ColoredSnowShape(string value, bool register = false) : base(value, register)
	{
	}
}
