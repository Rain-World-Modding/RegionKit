using System.Reflection;
namespace RegionKit;

[BIE.BepInPlugin("rwmodding.coreorg.rk", "RegionKit", "2.1")]
public class Mod : BIE.BaseUnityPlugin
{
	internal const string RK_POM_CATEGORY = "RegionKit";
	private static Mod __inst = null!;
	//private readonly List<ActionWithData> _enableDels = new();
	private readonly List<ModuleInfo> _modules = new();
	private bool _modulesSetUp = false;
	private RainWorld _rw = null!;
	internal static LOG.ManualLogSource __logger => __inst.Logger;
	internal static RainWorld __RW => __inst._rw;
	public void OnEnable()
	{
		__inst = this;
		On.RainWorld.OnModsInit += Init;
		//Init();
		TheRitual.Commence();
	}

	private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
	{
		orig(self);
		if (!_modulesSetUp) ScanAssemblyForModules(typeof(Mod).Assembly);
		foreach (var mod in _modules)
		{
			if (!_modulesSetUp) RunEnableOn(mod);
		}
		_modulesSetUp = true;
	}

	private void RunEnableOn(ModuleInfo mod)
	{
		try
		{
			mod.enable();
		}
		catch (Exception ex)
		{
			Logger.LogError($"Could not enable {mod.name}: {ex}");
		}
	}
	public void OnDisable()
	{
		On.RainWorld.OnModsInit -= Init;
		foreach (var mod in _modules)
		{
			RunDisableOn(mod);
		}
		__inst = null!;
	}

	private void RunDisableOn(ModuleInfo mod)
	{
		try
		{
			mod.disable();
		}
		catch (Exception ex)
		{
			Logger.LogError($"Error disabling {mod.name}: {ex}");
		}
	}

	public void FixedUpdate()
	{
		_rw ??= FindObjectOfType<RainWorld>();
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
			catch (Exception ex)
			{
				Logger.LogError($"Module {mod.name} error in tick: {ex}");
			}
		}
	}
	#region module shenanigans
	public void ScanAssemblyForModules(RFL.Assembly asm)
	{
		foreach (Type t in asm.DefinedTypes)
		{
			if (t.IsGenericTypeDefinition) continue;
			TryRegisterModule(t);
		}
	}

	public void TryRegisterModule(Type? t)
	{
		if (t is null) return;
		foreach (RegionKitModuleAttribute moduleAttr in t.GetCustomAttributes(typeof(RegionKitModuleAttribute), false))
		{
			RFL.MethodInfo
				enable = t.GetMethod(moduleAttr._enableMethod, BF_ALL_CONTEXTS_STATIC),
				disable = t.GetMethod(moduleAttr._disableMethod, BF_ALL_CONTEXTS_STATIC);
			RFL.MethodInfo? tick = moduleAttr._tickMethod is string tic ? t.GetMethod(tic, BF_ALL_CONTEXTS_STATIC) : null;
			string moduleName = moduleAttr._moduleName ?? t.FullName;
			if (enable is null || disable is null)
			{
				Logger.LogError($"Cannot register RegionKit module {t.FullName}: method contract incomplete ({moduleAttr._enableMethod} -> {enable}, {moduleAttr._disableMethod} -> {disable})");
				break;
			}
			Logger.LogMessage($"Registering module {moduleName}");

			Action
				enableDel = (Action)Delegate.CreateDelegate(typeof(Action), enable),
				disableDel = (Action)Delegate.CreateDelegate(typeof(Action), disable);
			Action? tickDel = tick is RFL.MethodInfo ntick ? (Action)Delegate.CreateDelegate(typeof(Action), ntick) : null;

			_modules.Add(new(moduleAttr._moduleName ?? t.FullName, enableDel, disableDel, tickDel, moduleAttr._tickPeriod)
			{
				counter = 0,
				errored = false
			});
			if (_modulesSetUp)
			{
				RunEnableOn(_modules.Last());
			}
			break;
			
		}
		foreach (Type nested in t.GetNestedTypes())
		{
			TryRegisterModule(nested);
		}
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
