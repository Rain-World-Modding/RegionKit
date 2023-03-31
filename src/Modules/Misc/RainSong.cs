using System.IO;
using Music;

///<summary>
///By HelloThere
///</summary>
internal static class RainSong
{
    internal static void Enable()
    {
        On.RainWorldGame.ctor += GameCtorPatch;
        On.RainCycle.Update += RainUpdatePatch;
        On.Music.MusicPlayer.RainRequestStopSong += RainStopPatch;
    }

    internal static void Disable()
    {
        On.RainWorldGame.ctor -= GameCtorPatch;
        On.RainCycle.Update -= RainUpdatePatch;
        On.Music.MusicPlayer.RainRequestStopSong -= RainStopPatch;
    }

    static bool filesChecked = false;

    static string  debugName;

    static readonly Dictionary<string, string> rainSongDict = new();

    static void GameCtorPatch(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig.Invoke(self, manager);

        if (!filesChecked)
        {
            Debug.Log("RainSong:  Checking song files...");
            string[] songArray = AssetManager.ListDirectory("music" + Path.DirectorySeparatorChar + "songs");
            Debug.Log("RainSong:  Song Array: " + songArray.Length);
            if (songArray.Length != 0)
            {
                foreach (string song in songArray)
                {
                    string songName = Path.GetFileNameWithoutExtension(song);
                    string key = songName.Remove(2);
                    if (songName.ToLower().Contains("rainsong") && !rainSongDict.ContainsKey(key))
                    {
                        rainSongDict.Add(key, songName);
                        Debug.Log("RainSong:  Registered Rain Song named " + song);
                    }
                }
            }

            filesChecked = true;
        }

        debugName = rainSongDict.Values.ToArray()[0];
        if (manager.musicPlayer != null && manager.musicPlayer.song != null && manager.musicPlayer.song.name.ToLower().Contains("rainsong")) manager.musicPlayer.song.FadeOut(100f);
    }

    static void RainUpdatePatch(On.RainCycle.orig_Update orig, RainCycle self)
    {
        orig.Invoke(self);
        RainWorldGame game = self.world.game;
        MusicPlayer player = game.manager.musicPlayer;
        string songName = null;
        bool check = self.RainApproaching < 0.5f && player != null && game.cameras[0].room.roomSettings.DangerType != RoomRain.DangerType.None && player.song == null && !game.cameras[0].room.abstractRoom.name.Contains("GATE") && !game.Players[0].realizedCreature.dead && rainSongDict.TryGetValue(self.world.region.name, out songName);
        //Debug.Log("RainSong:  " + check);
        if (player != null && player.song == null)
        {
            Debug.Log("RainSong:  Playing end of cycle song");
			Song song = new(game.manager.musicPlayer, debugName, MusicPlayer.MusicContext.StoryMode)
			{
                playWhenReady = true,
				Loop = true,
				priority = 100f,
				baseVolume = 0.1f,
				stopAtDeath = true,
				stopAtGate = true
			};
			player.song = song;
        }
    }

    static void RainStopPatch(On.Music.MusicPlayer.orig_RainRequestStopSong orig, MusicPlayer self)
    {
        if (self.song != null && self.song.name.ToLower().Contains("rainsong")) return;
        Debug.Log("RainSong:  are you sure this should happen");
        orig.Invoke(self);
    }
}