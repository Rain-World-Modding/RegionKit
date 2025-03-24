using Music;
using EffExt;
using MoreSlugcats;
using System.Linq;

namespace RegionKit.Modules.Effects
{
	// By @.k0r1 / Korii1
	internal static class MoonSirenBuilder
	{
		internal static void __RegisterBuilder()
		{
			try
			{
				new EffectDefinitionBuilder("MoonSiren")
					.AddBoolField("submerged", true, "Submerged")
					.SetUADFactory((Room room, EffectExtraData data, bool firstTimeRealized) => new MoonSirenUAD(data))
					.SetCategory("RegionKit")
					.Register();
				MoonSiren.Setup();
			}
			catch (Exception ex)
			{
				LogWarning(string.Format("Error on eff examples init {0}", ex));
			}
		}
	}
	internal class MoonSirenUAD : UpdatableAndDeletable
	{
		public EffectExtraData EffectData { get; }
		public MoonSiren siren;
		public bool submerged;
		public MoonSirenUAD(EffectExtraData effectData)
		{
			this.EffectData = effectData;
			this.submerged = EffectData.GetBool("submerged");
			this.siren = new MoonSiren();
		}
		//public override void Update(bool eu)
		//{
		//}
	}
	public class MoonSiren
	{
		public class MoonSirenSong : Song
		{
			Player CurrentPlayer;
			public MoonSirenSong(MusicPlayer musicPlayer, Player player, bool submerged) : base(musicPlayer, submerged ? "Moon_Siren_MS" : "Moon_Siren", MusicPlayer.MusicContext.StoryMode)
			{
				Loop = true;
				priority = 101f;
				baseVolume = 0.33f;
				stopAtGate = true;
				stopAtDeath = true;
				fadeInTime = 1000f;
				CurrentPlayer = player;
			}
			public override void Update()
			{
				base.Update();
				if (CurrentPlayer == null || CurrentPlayer.room == null) return;
				float volume = CurrentPlayer.room.roomSettings.GetEffectAmount(_Enums.MoonSiren);
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
			MoonSirenUAD? sirenUAD = self.room.updateList.OfType<MoonSirenUAD>().FirstOrDefault<MoonSirenUAD>();
			bool submerged = sirenUAD?.submerged ?? false;

			if (musicPlayer.song is MoonSirenSong)
			{
				if (self.room.roomSettings.GetEffect(_Enums.MoonSiren) == null || self.room.world?.rainCycle.RainApproaching > 0.5f)
					musicPlayer.song.baseVolume = 0f;
				return;
			}
			if (sirenUAD == null) return;
			if (musicPlayer.nextSong is MoonSirenSong) return;
			if (self.room.roomSettings.GetEffect(_Enums.MoonSiren) == null) return;
			if (self.room.world?.rainCycle.RainApproaching >= 0.5f) return;
			if (!(manager.currentMainLoop is RainWorldGame rainWorldGame) || !rainWorldGame.IsStorySession) return;
			if (!manager.rainWorld.setup.playMusic) return;
			
			Song mssiren = new MoonSirenSong(musicPlayer, self, submerged);
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
			if (self.song is MoonSirenSong && self.song.baseVolume > 0.02f) { return; }
			orig(self);
		}
		internal static void Setup()
		{
			On.Music.MusicPlayer.RainRequestStopSong += Patch_RainRequestStopSong;
			On.Player.Update += Update;
		}
	}
}
