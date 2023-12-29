namespace RegionKit;

public record ModuleInfo(
	Type moduleType,
	string name,
	Action enable,
	Action disable,
	Action? setup,
	Action? tick,
	int period)
{
	internal bool errored;
	internal int counter;
	internal bool ran_setup;
};
