using IO = System.IO;

namespace RegionKit.Modules.Slideshow;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Room Slideshow")]
public static class _Module
{
	internal readonly static System.Collections.Concurrent.ConcurrentDictionary<string, (Playback, IO.FileSystemWatcher?)> __playbacksById = new();
	public static void Enable()
	{
		__playbacksById.Clear();
		Playback testPlayback = Playback.MakeTestPlayback();
        __playbacksById[testPlayback.id] = (testPlayback, null);
		//__playbacksById.TryAdd(testPlayback.id, (testPlayback, null));
		IEnumerable<string> playback_files =
			AssetManager.ListDirectory("assets/regionkit/slideshows")
			//.Where(filename => filename.EndsWith(".txt"))
            ;
		foreach (string filename in playback_files)
		{
			IO.FileInfo file = new(filename);
            string ext = file.Extension;
            if (ext is not ".txt") continue;
			string name = file.Name[0..^ext.Length];
			try
			{
				__ReadAndRegisterFromFile(file, null, name);
			}
			catch (Exception ex)
			{
				__logger.LogError($"Error adding playback {name}, full filepath {filename}:\n{ex}");
			}
		}
	}

	public static void Disable()
	{

	}
	public static void Setup()
	{
		try
		{
			RegisterManagedObject<SlideShowUAD, SlideShowData, ManagedRepresentation>("SlideShow", RK_POM_CATEGORY);
		}
		catch (Exception ex)
		{
			__logger.LogError(ex);
		}
	}

	private static IO.FileSystemWatcher __CreateWatcher(
		// IO.DirectoryInfo directory,
		// string watcherId
		IO.FileInfo file,
		string watcherId
		)
	{
        __logger.LogDebug($"Creating watcher for file {file.FullName}");
		IO.FileSystemWatcher watcher = new(file.DirectoryName)
		{
			NotifyFilter = IO.NotifyFilters.LastWrite,
			Filter = file.Name,
			// IncludeSubdirectories = false,
			EnableRaisingEvents = true,
		};
		IO.FileSystemEventHandler handlerReadPlayback = (sender, args) =>
		{
            __logger.LogDebug($"Watcher {watcherId} start read event");
			try
			{
				IO.FileInfo file = new(args.FullPath);
				__ReadAndRegisterFromFile(file, watcher, file.Name[0..^file.Extension.Length]);
                __logger.LogDebug($"Watcher {watcherId} read event success");
			}
			catch (Exception ex)
			{
				__logger.LogError($"Error reading slideshow playback from watcher {watcherId} (args '{args.ChangeType}' '{args.Name}' '{args.FullPath}' ):\n{ex}");
			}
		};
		IO.FileSystemEventHandler handlerRemovePlayback = (sender, args) =>
		{
            __logger.LogDebug($"Watcher {watcherId} start clear event");
		    //IO.FileInfo file = new(args.FullPath);
		    __playbacksById.TryRemove(file.Name, out (Playback, IO.FileSystemWatcher?) popped);
            __logger.LogDebug($"Watcher {watcherId} clear event success {popped}");
		};


		watcher.Changed += handlerReadPlayback;
		// watcher.Created += handlerReadPlayback;
		watcher.Deleted += handlerRemovePlayback;

		return watcher;
	}
	private static void __ReadAndRegisterFromFile(
		IO.FileInfo file,
		IO.FileSystemWatcher? existingWatcher,
		string name)
	{
        __logger.LogDebug($"Adding playback fromfile called {name}");
		string[] lines = IO.File.ReadAllLines(file.FullName);
		Playback playback = _Read.FromText(name, lines);
		__playbacksById[name] = (playback, existingWatcher ?? __CreateWatcher(file, "w_" + name));
	}
}
