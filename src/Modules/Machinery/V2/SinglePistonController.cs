namespace RegionKit.Modules.Machinery.V2;

internal class SinglePistonController : UpdatableAndDeletable
{
	public readonly PlacedObject owner;
	public readonly Piston piston;
	public IVisualsProvider? visualProv;
	public IOscillationProvider? oscProv;
	public SinglePistonControllerData Data => (SinglePistonControllerData)owner.data;

	public SinglePistonController(PlacedObject owner, Room room)
	{
		LogTrace($"Creating piston controller in room {room.abstractRoom.name}");
		piston = new(static () => 1f); //todo: dynamic speed support
		room.AddObject(piston);
		this.owner = owner;
		this.room = room;
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
		piston.visuals = (visualProv ?? IVisualsProvider.Default.one).VisualsForNew();
		piston.oscillation = (oscProv ?? IOscillationProvider.Default.one).OscillationForNew();
		piston.pos = owner.pos;
		piston.rotDeg = Data.align ? Data.rotation - Data.rotation % 45f : Data.rotation;

		
	}
}
