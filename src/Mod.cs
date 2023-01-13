using System.Reflection;
using ActionWithData = System.ValueTuple<System.Action, string>;

namespace RegionKit;

[BIE.BepInPlugin("rwmodding.coreorg.rk", "RegionKit", "2.0")]
public class Mod : BIE.BaseUnityPlugin
{
	private static Mod __inst = null!;
	private readonly List<ActionWithData> _enableDels = new();
	private readonly List<ActionWithData> _disableDels = new();
	private bool _modulesSetUp = false;
	internal static LOG.ManualLogSource plog => __inst.Logger;
	public void OnEnable()
	{
		__inst = this;
		if (_modulesSetUp) goto APPLY;
		ScanAssemblyForModules(typeof(Mod).Assembly);
		_modulesSetUp = true;
	APPLY:;
		foreach ((var ac, var name) in _enableDels)
		{
			try
			{
				ac();
			}
			catch (Exception ex)
			{
				Logger.LogError($"Could not enable {name}: {ex}");
			}
		}
		TheRitual.Commence();
	}

	public void ScanAssemblyForModules(RFL.Assembly asm)
	{
		foreach (var t in asm.DefinedTypes)
		{
			if (t.IsGenericTypeDefinition) continue;
			TryRegisterModule(t);
		}
	}

	private bool TryRegisterModule(TypeInfo? t)
	{
		if (t is null) return false;
		foreach (RegionKitModuleAttribute moduleAttr in t.GetCustomAttributes(typeof(RegionKitModuleAttribute), false))
		{
			RFL.MethodInfo
				enable = t.GetMethod(moduleAttr.enableMethod, BF_ALL_CONTEXTS_STATIC),
				disable = t.GetMethod(moduleAttr.disableMethod, BF_ALL_CONTEXTS_STATIC);
			string moduleName = moduleAttr.moduleNameOverride ?? t.FullName;
			if (enable is null || disable is null)
			{
				Logger.LogError($"Cannot register RegionKit module {t.FullName}: method contract incomplete ({moduleAttr.enableMethod} -> {enable}, {moduleAttr.disableMethod} -> {disable})");
				break;
			}
			Logger.LogMessage($"Registering module {moduleName}");
			_enableDels.Add(((Action)Delegate.CreateDelegate(typeof(Action), enable), moduleName));
			_disableDels.Add(((Action)Delegate.CreateDelegate(typeof(Action), disable), moduleName));
			return true;
		}
		return false;
	}

	public void OnDisable()
	{
		foreach ((var ac, var name) in _disableDels)
		{
			try
			{
				ac();
			}
			catch (Exception ex)
			{
				Logger.LogError($"Error disabling {name}: {ex}");
			}
		}
		__inst = null!;
	}
}
