using DevInterface;

namespace RegionKit.Modules.Iggy;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Iggy")]
public static class _Module
{
	public static System.Runtime.CompilerServices.ConditionalWeakTable<DevInterface.Page, DevInterface.Panel> __iggys = new() { };
	public static void Setup() { }
	public static void Enable()
	{
		On.DevInterface.Page.ctor += __PageCtor;
	}
	public static void Disable() { 

		On.DevInterface.Page.ctor -= __PageCtor;
	}

	public static void __PageCtor(
		On.DevInterface.Page.orig_ctor orig,
		DevInterface.Page self,
		DevUI owner,
		string IDstring,
		DevUINode parentNode,
		string name)
	{
		orig(self, owner, IDstring, parentNode, name);
		self.subNodes.Add(new Iggy(
			owner,
			IDstring + "_Iggy",
			self,
			new(100f, 100f)));
	}
}
