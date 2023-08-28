namespace RegionKit.Modules.Slideshow;

[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Room Slideshow")]
public static class _Module {
    internal readonly static Dictionary<string, Playback> __playbacksById = new();
    public static void Enable() {

    }
    public static void Disable() {

    }
    public static void Setup() {
        try {
            RegisterManagedObject<SlideShowUAD, SlideShowData, ManagedRepresentation>("SlideShow", RK_POM_CATEGORY);


            Playback test = Playback.MakeTestPlayback();
            __playbacksById.Add(test.id, test);
            // PlayState state = new(test, false);
            // while (!state.Completed) {
            //     state.Update();
            //     __logger.LogDebug(state.ThisInstant());
            // }
        }
        catch (Exception ex) {
            __logger.LogError(ex);
        }
    }
}
