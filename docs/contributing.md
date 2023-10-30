## Structure
- Build refs go to `src/libs/`; they are gitignored.
- Main plugin class: `src/Mod.cs`. Avoid clutter there and only add initialization calls; add your stuff to separate classes and/or sub-namespaces.
- `src/Prelude.cs` defines project-wide aliases and usings. Take a look there to ensure stylistic consistency.
- Use `RegionKit.Mod.__logger` for logging. Since there's a `global using static RegionKit.Mod;` in Prelude, you can write that as `__logger`.
- When adding new content, create a new module folder in `src/Modules`, or, if it's something really small, add it in `src/Modules/Misc`. Avoid directly adding code into project root. To enable your things, use one of the following methods:
    - Either it write out directly from `Mod.OnEnable` (`RegionKit.Modules.MyModuleNamespace._Module.ApplyHooks()`),
    - Or make a module class and denote it with `RegionKitModuleAttribute`. It will be discovered and linked via reflection.

## Naming conventions

When a module has a single class that serves as entrypoint, that class should be prefixed with an underscore (`_Module`). Classes that are not part of a folder in `src/Modules` can still be marked like that if they serve a clearly isolated utility function (example: `src/_Assets.cs`).

It is recommended to prefix nonpublic static members with double underscore and nonpublic instance members with single underscore, although for self-preservation purposes this is not required.

### Examples

Using RegionKitAttribute to define a module:
```cs
namespace RegionKit;
[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "MyShinyModule")]
internal static class _Module
{
	public static void Enable()
	{  
        On.Room.Loaded += myHook;
    }
    public static void Disable()
    {
        On.Room.Loaded += myHook;
    }
}
```
All methods passed to RegionKitModuleAttribute be of form `void ()` - void return type and no arguments passed.

## Logging

Please don't use `UnityEngine.Debug`. We have a logger in `src/Logfix.cs`, you can use static members from there like this:

```cs
LogMessage("Something is happening!")
LogError("Something bad is happening!")
LogTrace("Something insignificant is happening!")
```