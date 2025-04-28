using Music;
using EffExt;
using MoreSlugcats;

namespace RegionKit.Modules.Effects
{
	// By @.k0r1 / Korii1
	internal static class RainSirenBuilder
	{
		internal static void __RegisterBuilder()
		{
			try
			{
				new EffectDefinitionBuilder("RainSiren")
					.AddStringField("songname", "Moon_Siren_MS", "Song Name")
					.AddStringField("fadein", "1000", "Fade-In Time")
					.SetUADFactory((Room room, EffectExtraData data, bool firstTimeRealized) => new RainSirenUAD(data))
					.SetCategory("RegionKit")
					.Register();
				RainSiren.Setup();
			}
			catch (Exception ex)
			{
				LogWarning(string.Format("Error on eff examples init {0}", ex));
			}
		}
	}
	internal class RainSirenUAD : UpdatableAndDeletable
	{
		public EffectExtraData EffectData { get; }
		public string songname;
		public float fadein;
		string previousfadein = "1000.0";
		public RainSiren siren;

		public RainSirenUAD(EffectExtraData effectData)
		{
			this.EffectData = effectData;
			this.songname = effectData.GetString("songname");
			this.siren = new RainSiren();
		}
		public override void Update(bool eu)
		{
			this.songname = this.EffectData.GetString("songname");
			string currentfadein = this.EffectData.GetString("fadein");
			if (!float.TryParse(currentfadein, out this.fadein))
			{
				this.EffectData.Set("fadein", previousfadein);
				return;
			}
			previousfadein = currentfadein;
		}
	}
	public class RainSiren
	{
		public class RainSirenSong : Song
		{
			Player CurrentPlayer;
			public RainSirenSong(MusicPlayer musicPlayer, Player player, string songname, float fadein) : base(musicPlayer, songname, MusicPlayer.MusicContext.StoryMode)
			{
				Loop = true;
				priority = 101f;
				baseVolume = 0.33f;
				stopAtGate = true;
				stopAtDeath = true;
				fadeInTime = fadein;
				CurrentPlayer = player;
			}
			public override void Update()
			{
				base.Update();
				if (CurrentPlayer == null || CurrentPlayer.room == null) return;
				float volume = CurrentPlayer.room.roomSettings.GetEffectAmount(_Enums.RainSiren);
				if (baseVolume != volume)
				{
					baseVolume = Custom.LerpAndTick(baseVolume, volume, 0.005f, 0.0025f);
					return;
				}
				if (musicPlayer == null || musicPlayer.manager == null || musicPlayer.manager.currentMainLoop == null
					|| musicPlayer.manager.currentMainLoop.ID != ProcessManager.ProcessID.Game) FadeOut(200f);
			}
		}
		static void Update(On.Player.orig_Update orig, Player self, bool eu)
		{
			orig(self, eu);
			if (self.room == null) return;
			ProcessManager? manager = self.room.world?.game?.manager;
			MusicPlayer? musicPlayer = manager?.musicPlayer;
			if (manager == null || musicPlayer == null) return;
			if (musicPlayer.song is MSSirenSong) return;
			RainSirenUAD? sirenUAD = self.room.updateList.OfType<RainSirenUAD>().FirstOrDefault<RainSirenUAD>();
			string? songname = sirenUAD?.songname;

			if (musicPlayer.song is RainSirenSong)
			{
				if (self.room.roomSettings.GetEffect(_Enums.RainSiren) == null || songname == null || (musicPlayer.song.name != songname) || self.room.world?.rainCycle.RainApproaching > 0.5f)
					musicPlayer.song.baseVolume = 0f;
				return;
			}
			if (sirenUAD == null || songname == null) return;
			if (musicPlayer.nextSong is RainSirenSong) return;
			if (self.room.roomSettings.GetEffect(_Enums.RainSiren) == null) return;
			if (self.room.world?.rainCycle.RainApproaching >= 0.5f) return;
			if (!(manager.currentMainLoop is RainWorldGame rainWorldGame) || !rainWorldGame.IsStorySession) return;
			if (!manager.rainWorld.setup.playMusic) return;
			
			Song mssiren = new RainSirenSong(musicPlayer, self, songname, sirenUAD.fadein);
			if (musicPlayer.song == null)
			{
				musicPlayer.song = mssiren;
				mssiren.playWhenReady = true;
				return;
			}
			musicPlayer.nextSong = mssiren;
			mssiren.playWhenReady = false;
		}
		static void Patch_RainRequestStopSong(On.Music.MusicPlayer.orig_RainRequestStopSong orig, MusicPlayer self)
		{
			if (self.song is RainSirenSong && self.song.baseVolume > 0.02f) { return; }
			orig(self);
		}
		internal static void Setup()
		{
			On.Music.MusicPlayer.RainRequestStopSong += Patch_RainRequestStopSong;
			On.Player.Update += Update;
		}
	}
}
