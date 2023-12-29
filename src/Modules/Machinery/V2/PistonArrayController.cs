namespace RegionKit.Modules.Machinery.V2;

public class PistonArrayController : UpdatableAndDeletable
{
	public readonly PlacedObject owner;
	public readonly List<Piston> pistons = new();
	public IOscillationProvider? oscProv;
	public IVisualsProvider[]? visProvSet;
	private int _lastPistonCount = -1;
	public PistonArrayControllerData Data => (PistonArrayControllerData)owner.data;
	public PistonArrayController(PlacedObject owner, Room room)
	{
		this.owner = owner;
		this.room = room;
	}
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (visProvSet is null)
		{
			IEnumerable<IVisualsProvider>? allvisprovs = room.roomSettings.FindAllPlacedObjectsData<IVisualsProvider>();
			visProvSet = ParseVisualTags()
				.Select(tag => allvisprovs.FirstOrDefault(prov => prov.Tag == tag))
				.Where(selected_prov => selected_prov is not null)
				.ToArray();
		}
		oscProv ??=
			room.roomSettings.FindAllPlacedObjectsData<IOscillationProvider>()
			.Where(prov => prov.Tag == Data.oscTag)
			.FirstOrDefault();
		if (_lastPistonCount != Data.count) GeneratePistons();
		_lastPistonCount = Data.count;

		Vector2
			pos = owner.pos,
			posStep = Data.p2 / (float)Data.count;
		if (Data.alignAxis)
		{
			float
				deg = VecToDeg(posStep),
				mag = posStep.magnitude;
			posStep = DegToVec(deg - deg % 45f) * mag;
		}
		float pistonAngle = VecToDeg(Data.p2) + 90f + Data.addRot;
		if (Data.alignPistons) pistonAngle = pistonAngle - pistonAngle % 45f;
		OscillationParams oscillation = (oscProv ?? IOscillationProvider.Default.one).OscillationForNew();

		IEnumerable<IVisualsProvider> visProvCycle = visProvSet.Loop();
		IEnumerable<(Piston piston, IVisualsProvider vis)> pairs = pistons.Zip(visProvCycle, (piston, vis) => (piston, vis));
		foreach ((Piston piston, IVisualsProvider vis) in pairs)
		{
			piston.oscillation = oscillation;
			piston.visuals = vis.VisualsForNew();
			piston.pos = pos;
			piston.rotDeg = pistonAngle;

			oscillation.phase += Data.phaseStep;
			pos += posStep;
		}
	}
	public void GeneratePistons()
	{
		foreach (Piston p in pistons) p.Destroy();
		pistons.Clear();
		for (int i = 0; i < Data.count; i++)
		{
			Piston item = new(() => 1f); //todo: dynamic speed
			pistons.Add(item);
			room.AddObject(item);
		}
	}
	public IEnumerable<int> ParseVisualTags()
	{
		int successes = 0;
		string[] split = System.Text.RegularExpressions.Regex.Split(Data.visualsTags, "\\s+");
		for (int i = 0; i < split.Length; i++)
		{
			if (int.TryParse(split[i], out int result))
			{
				successes++;
				yield return result;
			}
		}
		if (successes is 0) yield return 0;
	}
}
