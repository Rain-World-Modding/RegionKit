using System.Reflection;
//using ActionWithData = System.ValueTuple<System.Action, string>;

namespace RegionKit;

[BIE.BepInPlugin("rwmodding.coreorg.rk", "RegionKit", "2.0")]
public class Mod : BIE.BaseUnityPlugin
{
	private static Mod __inst = null!;
	//private readonly List<ActionWithData> _enableDels = new();
	private readonly List<ModuleInfo> _modules = new();
	private bool _modulesSetUp = false;
	internal static LOG.ManualLogSource plog => __inst.Logger;
	public void OnEnable()
	{
		__inst = this;
		if (!_modulesSetUp) ScanAssemblyForModules(typeof(Mod).Assembly);
		_modulesSetUp = true;
		foreach (var mod in _modules)
		{
			try
			{
				mod.enable();
			}
			catch (Exception ex)
			{
				Logger.LogError($"Could not enable {name}: {ex}");
			}
		}
		TheRitual.Commence();
	}
	public void OnDisable()
	{
		foreach (var mod in _modules)
		{
			try
			{
				mod.disable();
			}
			catch (Exception ex)
			{
				Logger.LogError($"Error disabling {name}: {ex}");
			}
		}
		__inst = null!;
	}
	public void FixedUpdate()
	{

		foreach (var mod in _modules)
		{
			try
			{
				mod.counter--;
				if (mod.counter < 1)
				{
					mod.counter = mod.period;
					mod?.tick?.Invoke();
				}
			}
			catch (Exception ex){
				Logger.LogError($"Module {mod.name} error in tick: {ex}");
			}
		}
	}
	#region module shenanigans
	public void ScanAssemblyForModules(RFL.Assembly asm)
	{
		foreach (var t in asm.DefinedTypes)
		{
			if (t.IsGenericTypeDefinition) continue;
			TryRegisterModule(t);
		}
	}

	public bool TryRegisterModule(TypeInfo? t)
	{
		if (t is null) return false;
		foreach (RegionKitModuleAttribute moduleAttr in t.GetCustomAttributes(typeof(RegionKitModuleAttribute), false))
		{
			RFL.MethodInfo
				enable = t.GetMethod(moduleAttr.enableMethod, BF_ALL_CONTEXTS_STATIC),
				disable = t.GetMethod(moduleAttr.disableMethod, BF_ALL_CONTEXTS_STATIC);
			RFL.MethodInfo? tick = moduleAttr.tickMethod is string tic ? t.GetMethod(tic, BF_ALL_CONTEXTS_STATIC) : null;
			string moduleName = moduleAttr.moduleName ?? t.FullName;
			if (enable is null || disable is null)
			{
				Logger.LogError($"Cannot register RegionKit module {t.FullName}: method contract incomplete ({moduleAttr.enableMethod} -> {enable}, {moduleAttr.disableMethod} -> {disable})");
				break;
			}
			Logger.LogMessage($"Registering module {moduleName}");

			Action
				enableDel = (Action)Delegate.CreateDelegate(typeof(Action), enable),
				disableDel = (Action)Delegate.CreateDelegate(typeof(Action), disable);
			Action? tickDel = tick is RFL.MethodInfo ntick ? (Action)Delegate.CreateDelegate(typeof(Action), ntick) : null;

			_modules.Add(new(moduleAttr.moduleName ?? t.FullName, enableDel, disableDel, tickDel, moduleAttr.tickPeriod)
			{
				counter = 0,
				errored = false
			});
			return true;
		}
		return false;
	}


	internal record ModuleInfo(
		string name,
		Action enable,
		Action disable,
		Action? tick,
		int period)
	{
		internal bool errored;
		internal int counter;
	};
	#endregion
}
