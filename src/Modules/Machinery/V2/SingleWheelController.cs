namespace RegionKit.Modules.Machinery.V2;

public class SingleWheelController : UpdatableAndDeletable
{
	public readonly PlacedObject owner;
	public readonly Wheel wheel;
	public IVisualsProvider? visualProv;
	public IOscillationProvider? oscProv;
	public SingleWheelControllerData Data => (SingleWheelControllerData)owner.data;
	public SingleWheelController(PlacedObject owner, Room room)
	{
		wheel = new(() => 1f); //todo: dynamic speed
		room.AddObject(wheel);
		this.owner = owner;
	}
	public override void Update(bool eu)
	{
		base.Update(eu);
		visualProv ??=
			room.roomSettings.FindAllPlacedObjectsData<IVisualsProvider>()
			.Where(prov => prov.Tag == Data.visualsTag)
			.FirstOrDefault();
		oscProv ??= 
			room.roomSettings.FindAllPlacedObjectsData<IOscillationProvider>()
			.Where(prov => prov.Tag == Data.oscTag)
			.FirstOrDefault();
		wheel.visuals = (visualProv ?? IVisualsProvider.Default.one).VisualsForNew();
		wheel.oscillation = (oscProv ?? IOscillationProvider.Default.one).OscillationForNew();
		wheel.pos = owner.pos;
	}

}
