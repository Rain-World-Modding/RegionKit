/// <summary>
/// Denote your type with this to register it as a RegionKit module. Use <see cref="Mod.ScanAssemblyForModules(System.Reflection.Assembly)"/> on containing assembly to find and register the type.
/// </summary>
public class RegionKitModuleAttribute : Attribute
{
	internal readonly string _enableMethod;
	internal readonly string _disableMethod;
	internal readonly string? _tickMethod;
	internal readonly int _tickPeriod;
	internal readonly string? _loggerField;
	internal readonly string? _moduleName;
	/// <summary>
	/// 
	/// </summary>
	/// <param name="enableMethod">Name of a static method that will be called to enable your module. Obligatory.</param>
	/// <param name="disableMethod">Name of a static method that will be called to disable your module. Obligatory.</param>
	/// <param name="tickMethod">Name of a static method that will be called every few fixedUpdates. Optional.</param>
	/// <param name="tickPeriod">If tickMethod is set, how often (in frames) will tick be called.</param>
	/// <param name="loggerField">Name of a static field that will be populated with a ManualLogSource created for your module. Optional</param>
	/// <param name="moduleName">Name of your module. Defaults to type name.</param>
	public RegionKitModuleAttribute(
		string enableMethod,
		string disableMethod,
		string? tickMethod = null,
		int tickPeriod = 1,
		string? loggerField = null,
		string? moduleName = null)
	{
		this._enableMethod = enableMethod;
		this._disableMethod = disableMethod;
		this._tickMethod = tickMethod;
		this._tickPeriod = tickPeriod;
		this._loggerField = loggerField;
		this._moduleName = moduleName;
	}
}
