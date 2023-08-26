namespace RegionKit.Modules.Slideshow;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Room Slideshow")]
public static class _Module {
    public static void Enable() {

    }
    public static void Disable() {

    }
    public static void Setup() {
        try {
            Playback test = Playback.MakeTestPlayback();
            PlayState state = new(test, false);
            while (!state.Completed) {
                
                state.Update();
                __logger.LogDebug(state.ThisInstant());
                
            }
        }
        catch (Exception ex) {
            __logger.LogError(ex);
        }
    }
}
