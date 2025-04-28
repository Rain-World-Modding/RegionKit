using RegionKit.Modules.Effects;
using RegionKit.Modules.GateCustomization;
using RegionKit.Modules.RoomSlideShow;

namespace RegionKit;
/// <summary>
/// Main plugin class
/// </summary>
[BepInEx.BepInDependency("rwmodding.coreorg.pom", BepInEx.BepInDependency.DependencyFlags.HardDependency)]
[BepInEx.BepInPlugin(MOD_GUID, MOD_FRIENDLYNAME, MOD_VERSION)]
public class Mod : BepInEx.BaseUnityPlugin
{
	internal const string MOD_VERSION = "3.17.0";
	internal const string MOD_FRIENDLYNAME = "RegionKit";
	internal const string MOD_GUID = "rwmodding.coreorg.rk";
	internal const string RK_POM_CATEGORY = "RegionKit";
	internal static Mod? __inst;
	//private readonly List<ActionWithData> _enableDels = new();
	internal readonly List<ModuleInfo> _modules = [];
	private bool _modulesSetUp = false;
	private RainWorld? _rw;
	// internal static BepInEx.Logging.ManualLogSource __logger => __inst.Logger;
	internal static RainWorld? __RW => __inst?._rw;
	///<inheritdoc/>
	private BepInEx.Configuration.ConfigEntry<bool>? _writeTraceConfig;
	private BepInEx.Configuration.ConfigEntry<bool>? _writeCallSiteInfoConfig;
	/// <inheritdoc/>
	public void OnEnable()
	{
		__inst = this;

		_writeTraceConfig = Config.Bind("main", "writeTrace", false, "Write additional spammy debug lines");
		__writeTrace = _writeTraceConfig.Value;
		_writeTraceConfig.SettingChanged += (sender, args) =>
		{
			__writeTrace = _writeTraceConfig.Value;
		};
		_writeCallSiteInfoConfig = Config.Bind("main", "writeCallsite", false, "Write information about method call site in all log entries");
		__writeCallsiteInfo = _writeCallSiteInfoConfig.Value;
		_writeCallSiteInfoConfig.SettingChanged += (sender, args) =>
		{
			__writeCallsiteInfo = _writeTraceConfig.Value;
		};
		Logfix.__SwitchToBepinexLogger(Logger);
		On.RainWorld.OnModsInit += Init;
		//Init();
	}

	private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
	{
		System.Diagnostics.Stopwatch
			totalTime = new(),
			perModule = new();
		List<(ModuleInfo, TimeSpan)> enableTimes = new();
		try { orig(self); }
		catch (Exception ex) { LogFatal($"Caught error in init-orig: {ex}"); }
		try
		{
			if (!_modulesSetUp)
			{
				ScanAssemblyForModules(typeof(Mod).Assembly);
				totalTime.Start();

				foreach (var mod in _modules)
				{
					perModule.Restart();
					RunEnableOn(mod);
					enableTimes.Add((mod, perModule.Elapsed));
				}
				totalTime.Stop();
			}
			_modulesSetUp = true;
			_Assets.LoadResources();
			ModOptions.RegisterOI("regionkit");
		}
		catch (Exception ex)
		{
			LogError($"Error on init: {ex}");
		}
		finally
		{
			LogDebug($"Total load time {totalTime.Elapsed}");
			LogDebug($"Load time for modules: ");
			foreach ((ModuleInfo module, TimeSpan elapsed) in enableTimes) LogDebug($"\t{module.name} : {elapsed}");
		}
	}

	private void RunEnableOn(ModuleInfo mod)
	{
		try
		{
			if (!mod.ran_setup)
			{
				LogDebug($"setup {mod.name}");
				mod.setup?.Invoke();
			}
			LogDebug($"enable {mod.name}");
			mod.enable();
		}
		catch (Exception ex)
		{
			Logger.LogError($"Could not enable {mod.name}: {ex}");
		}
		finally
		{
			mod.ran_setup = true;
		}
	}
	///<inheritdoc/>
	public void OnDisable()
	{
		On.RainWorld.OnModsInit -= Init;
		foreach (ModuleInfo mod in _modules)
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
	///<inheritdoc/>
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
	///<inheritdoc/>
	public void ScanAssemblyForModules(System.Reflection.Assembly asm)
	{
		System.Diagnostics.Stopwatch scanTime = new();
		scanTime.Start();
		Type[] types;
		types = asm.GetTypesSafe(out var err);
		if (err is not null)
		{
			LogError(err);
			LogError(err.InnerException);
		}
		foreach (Type t in types)
		{
			if (t.IsGenericTypeDefinition) continue;
			TryRegisterModule(t);
		}
		scanTime.Stop();
		LogDebug($"Scanned {asm.FullName} for modules in {scanTime.Elapsed}");
	}

	///<inheritdoc/>
	public void TryRegisterModule(Type? t)
	{
		if (t is null) return;
		foreach (RegionKitModuleAttribute moduleAttr in t.GetCustomAttributes(typeof(RegionKitModuleAttribute), false))
		{
			System.Reflection.MethodInfo
				enable = t.GetMethod(moduleAttr._enableMethod, BF_ALL_CONTEXTS_STATIC),
				disable = t.GetMethod(moduleAttr._disableMethod, BF_ALL_CONTEXTS_STATIC);

			System.Reflection.MethodInfo?
				tick = moduleAttr._tickMethod is string ticm ? t.GetMethod(ticm, BF_ALL_CONTEXTS_STATIC) : null,
				setup = moduleAttr._setupMethod is string setupm ? t.GetMethod(setupm, BF_ALL_CONTEXTS_STATIC) : null;

			System.Reflection.FieldInfo? loggerField = moduleAttr._loggerField is string logf ? t.GetField(logf, BF_ALL_CONTEXTS_STATIC) : null;
			string moduleName = moduleAttr._moduleName ?? t.FullName;
			if (enable is null || disable is null)
			{
				Logger.LogError($"Cannot register RegionKit module {t.FullName}: method contract incomplete ({moduleAttr._enableMethod} -> {enable}, {moduleAttr._disableMethod} -> {disable})");
				break;
			}
			Logger.LogMessage($"Registering module {moduleName}");
			if (loggerField is not null)
				try
				{
					loggerField.SetValue(null, BepInEx.Logging.Logger.CreateLogSource($"RegionKit/{moduleName}"), BF_ALL_CONTEXTS_STATIC, null, System.Globalization.CultureInfo.InvariantCulture
					);
				}
				catch (Exception ex) { Logger.LogWarning($"Invalid logger field name supplied! {ex}"); }
			Action
				enableDel = (Action)Delegate.CreateDelegate(typeof(Action), enable),
				disableDel = (Action)Delegate.CreateDelegate(typeof(Action), disable);
			;
			Action?
				tickDel = tick is System.Reflection.MethodInfo ntick ? (Action)Delegate.CreateDelegate(typeof(Action), ntick) : null,
				setupDel = setup is System.Reflection.MethodInfo nset ? (Action)Delegate.CreateDelegate(typeof(Action), nset) : null;
			_modules.Add(new(
				t,
				moduleAttr._moduleName ?? t.FullName,
				enableDel,
				disableDel,
				setupDel,
				tickDel,
				moduleAttr._tickPeriod)
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
	#endregion
}
