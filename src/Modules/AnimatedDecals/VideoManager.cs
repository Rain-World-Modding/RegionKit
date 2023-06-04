using System.IO;
using UnityEngine.Video;

namespace RegionKit.Modules.AnimatedDecals
{
	/// <summary>
	/// Provides helpers to manipulate video files.
	/// </summary>
	public static class VideoManager
	{
		static readonly Dictionary<FAtlas, VideoPlayer> _players = new();

		private static readonly string[] _supportedExtensions = new string[]
		{
			".asf", ".avi", ".dv", ".m4v", ".mov", ".mp4", ".mpg", ".mpeg", ".ogv", ".vp8", ".webm", ".wmv"
		};

		internal static void Enable()
		{
			On.ProcessManager.Update += ProcessManager_Update;
			On.FAtlasManager.ActuallyUnloadAtlasOrImage += FAtlasManager_ActuallyUnloadAtlasOrImage;
		}

		internal static void Disable()
		{
			foreach(VideoPlayer player in _players.Values)
			{
				UnityEngine.Object.Destroy(player.targetTexture);
				UnityEngine.Object.Destroy(player.gameObject);
			}
			_players.Clear();

			On.ProcessManager.Update -= ProcessManager_Update;
			On.FAtlasManager.ActuallyUnloadAtlasOrImage -= FAtlasManager_ActuallyUnloadAtlasOrImage;
		}

		// Pause videos when the game is paused
		private static void ProcessManager_Update(On.ProcessManager.orig_Update orig, ProcessManager self, float deltaTime)
		{
			orig(self, deltaTime);

			bool pauseVideos = false;
			if (self.currentMainLoop is RainWorldGame game)
			{
				pauseVideos = game.GamePaused;
			}

			foreach (VideoPlayer player in _players.Values)
			{
				if (player.isPaused != pauseVideos)
				{
					if (pauseVideos)
						player.Pause();
					else
						player.Play();
				}
			}
		}

		// Unload videos when the associated atlas is unloaded
		private static void FAtlasManager_ActuallyUnloadAtlasOrImage(On.FAtlasManager.orig_ActuallyUnloadAtlasOrImage orig, FAtlasManager self, string name)
		{
			if (self.DoesContainAtlas(name)
				&& self.GetAtlasWithName(name) is FAtlas atlas
				&& _players.TryGetValue(atlas, out VideoPlayer player))
			{
				UnityEngine.Object.Destroy(player.gameObject);
				_players.Remove(atlas);
			}

			orig(self, name);
		}

		/// <summary>
		/// Check if the given file path refers to a video.
		/// </summary>
		/// <param name="fileName">The file path to check.</param>
		/// <returns><c>true</c> if the file is a video, <c>false</c> otherwise.</returns>
		public static bool IsVideoFile(string fileName)
		{
			fileName = fileName.ToLowerInvariant();
			for (int i = 0; i < _supportedExtensions.Length; i++)
			{
				if (fileName.EndsWith(_supportedExtensions[i]))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Load a video from disk as an <see cref="FAtlas"/>.
		/// </summary>
		/// <param name="fileName">The name of the atlas.</param>
		/// <param name="path">The absolute path of the video.</param>
		/// <returns>An <see cref="FAtlas"/> that continuously plays the video.</returns>
		public static FAtlas LoadAndCacheVideo(string fileName, string path)
		{
			if (Futile.atlasManager.GetAtlasWithName(fileName) is FAtlas oldAtlas)
			{
				return oldAtlas;
			}

			// Set up video player
			string url = path.StartsWith("file://") ? path : ("file:///" + path);
			var go = new GameObject(fileName + " Player");
			var player = go.AddComponent<VideoPlayer>();
			player.audioOutputMode = VideoAudioOutputMode.None;
			player.url = url;
			player.playOnAwake = false;
			player.isLooping = true;

			var rt = new RenderTexture(1, 1, 0);
			player.targetTexture = rt;

			// Create the atlas
			HeavyTexturesCache.LoadAndCacheAtlasFromTexture(fileName, rt, false);
			var atlas = Futile.atlasManager.GetAtlasWithName(fileName);
			_players.Add(atlas, player);

			// Resize texture to render target when video loads
			player.prepareCompleted += _ =>
			{
				if (rt.IsCreated())
				{
					rt.Release();
				}
				rt.width = (int)player.width;
				rt.height = (int)player.height;
				rt.Create();

				atlas._textureSize = new Vector2(player.width, player.height);
			};

			player.Play();

			return atlas;
		}
	}
}
