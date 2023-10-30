using System;
using System.Linq;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;

public class CGSongSFX : Music.Song
{
	public CGSongSFX(Music.MusicPlayer musicPlayer, string title) : base(musicPlayer, title, Music.MusicPlayer.MusicContext.StoryMode)
	{
		this.priority = 1.1f;
		this.stopAtGate = true;
		this.stopAtDeath = true;
		this.fadeInTime = 120f;
		base.Loop = true;
		LogMessage("Created CGSongSFX for " + title);
	}

	public override void Update()
	{
		base.Update();
		if (this.setVolume != null)
		{
			this.baseVolume = RWCustom.Custom.LerpAndTick(this.baseVolume, this.setVolume.Value, 0.005f, 0.0025f);
			this.destroyCounter = 0;
		}
		else
		{
			if (!(this.musicPlayer.manager.currentMainLoop is RainWorldGame game) || game.pauseMenu == null)
				this.destroyCounter++;
			if (this.destroyCounter == 150)
			{
				LogMessage("Destroyed CGSongSFX");
				base.FadeOut(400f);
			}
		}
		this.setVolume = null;
	}

	public float? setVolume;

	public int destroyCounter;
}

public class CGSongSFXTrigger : UpdatableAndDeletable
    {

        internal class CGSongSFXTriggerData : ManagedData
        {
#pragma warning disable 0649
		[StringField("1name", "songname", "Name")]
		public string name = "songname";
		[FloatField("2intensity", 0f, 1f, 0.1f, 0.01f, displayName: "Intensity")]
		public float intensity;
#pragma warning restore 0649
		public CGSongSFXTriggerData(PlacedObject owner) : base(owner, null) { }
		public CGSongSFXTriggerData(PlacedObject owner, ManagedField[] fields) : base(owner, fields) { }
	}

	public readonly PlacedObject pObj;
        private CGSongSFXTriggerData data => (CGSongSFXTriggerData)pObj.data;
        public CGSongSFXTrigger(Room room, PlacedObject pObj)
        {
            this.room = room;
            this.pObj = pObj;
        }

	public override void Update(bool eu)
	{
		base.Update(eu);
		if (ShouldTrigger()) Trigger(Intensity());
	}

	protected virtual bool ShouldTrigger()
        {
		for (int i = 0; i < this.room.game.Players.Count; i++)
		{
			if (this.room.game.Players[i].realizedCreature != null && this.room.game.Players[i].realizedCreature.room == this.room)
			{
				return true;
			}
		}
		return false;
	}

	protected virtual float Intensity()
        {
		return this.data.intensity;
	}

	protected void Trigger(float intensity)
	{
		var player = this.room.game.manager.musicPlayer;
		if (player == null)// || this.room.gravity > 0f)
		{
			return;
		}

		var cGSongSFX = ((CGSongSFX)player.song);
		if (player.song == null || !(player.song is CGSongSFX) || !(player.song.name == this.data.name))
		{
			//this.room.game.manager.musicPlayer.RequestSSSong();
			if (player.song != null && player.song is CGSongSFX && player.song.name == this.data.name)
			{
				return;
			}
			if (player.nextSong != null && player.nextSong is CGSongSFX && player.nextSong.name == this.data.name)
			{
				return;
			}
			if (!player.manager.rainWorld.setup.playMusic)
			{
				return;
			}
			Music.Song song = new CGSongSFX(player, data.name);
			if (player.song == null)
			{
				player.song = song;
				player.song.playWhenReady = true;
			}
			else
			{
				player.nextSong = song;
				player.nextSong.playWhenReady = false;
			}
		}
		else if (cGSongSFX.setVolume != null)
		{
			cGSongSFX.setVolume = new float?(Mathf.Max(cGSongSFX.setVolume.Value, intensity));
		}
		else
		{
			cGSongSFX.setVolume = new float?(intensity);
		}
	}
}

    public class CGSongSFXGradient : CGSongSFXTrigger
    {
        internal class CGSongSFXGradientData : CGSongSFXTrigger.CGSongSFXTriggerData
        {
#pragma warning disable 0649
		[FloatField("3intensityb", 0f, 1f, 0.1f, 0.01f, displayName: "Intensity B")]
		public float intensityB;
		[FloatField("4expo", 0.01f, 10f, 1f, 0.01f, displayName: "Exponent")]
		public float exponent;
		private static readonly ManagedField[] customFields = new ManagedField[]
		   {
				new Vector2Field("5ev", new Vector2(-100, -40), Vector2Field.VectorReprType.line),
		   };
		[BackedByField("5ev")]
		public Vector2 handle;
#pragma warning restore 0649
		public CGSongSFXGradientData(PlacedObject owner) : base(owner, customFields) { }
		public CGSongSFXGradientData(PlacedObject owner, ManagedField[] fields) : base(owner, customFields.ToList().Concat(fields.ToList()).ToArray()) { }
	}

	private CGSongSFXGradientData data => (CGSongSFXGradientData)pObj.data;

	public CGSongSFXGradient(Room room, PlacedObject pObj) : base(room, pObj) { }

        protected override bool ShouldTrigger()
        {
		return base.ShouldTrigger() && Intensity() > 0f;
	}

        protected override float Intensity()
        {
		float max = 0f;
		for (int i = 0; i < this.room.game.Players.Count; i++)
		{
			if (this.room.game.Players[i].realizedCreature != null && this.room.game.Players[i].realizedCreature.room == this.room)
			{
				var creature = this.room.game.Players[i].realizedCreature;
				Vector2 value = creature.bodyChunks[0].pos;
				if (creature.inShortcut)
				{
					Vector2? vector = this.room.game.shortcuts.OnScreenPositionOfInShortCutCreature(this.room, creature);
					if (vector != null)
					{
						value = vector.Value;
					}
				}
				max = Mathf.Max(max, Mathf.Lerp(data.intensity, data.intensityB, Mathf.Pow(InverseLerp(pObj.pos, pObj.pos + data.handle, value), data.exponent)));
			}
		}
		return max;
	}

	// https://answers.unity.com/questions/1271974/inverselerp-for-vector3.html
	public static float InverseLerp(Vector2 a, Vector2 b, Vector2 value)
	{
		Vector2 AB = b - a;
		Vector2 AV = value - a;
		return Mathf.Clamp01(Vector2.Dot(AV, AB) / AB.sqrMagnitude);
	}
}
